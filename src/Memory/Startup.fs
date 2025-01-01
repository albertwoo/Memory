#nowarn "0020"

open System
open System.IO
open System.Reflection
open System.Security.Claims
open System.Runtime.InteropServices
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Server.Kestrel.Core
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http.Features
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.AspNetCore.Authorization
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Options
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.DataProtection
open Microsoft.Extensions.DependencyInjection
open Microsoft.EntityFrameworkCore
open Serilog
open Memory.Db
open Memory.Domain
open Memory.Options
open Memory.Services
open Memory.Endpoints


let builder = WebApplication.CreateBuilder(Environment.GetCommandLineArgs())
let config = builder.Configuration
let services = builder.Services
let disableAuth = config.GetValue("App:DisableAuth", false)


services.AddSerilog(LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger())

// Options
services.AddOptions<AppOptions>().Bind(config.GetSection("App")).ValidateDataAnnotations()

services.Configure(fun (options: KestrelServerOptions) -> options.Limits.MaxRequestBodySize <- Int64.MaxValue)
services.Configure(fun (options: FormOptions) -> options.MultipartBodyLengthLimit <- Int64.MaxValue)
services.Configure(fun (options: ForwardedHeadersOptions) -> options.ForwardedHeaders <- ForwardedHeaders.All)

services.AddHttpContextAccessor()

services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(fun options ->
        let dataProtectionDir = config.GetValue<string>("App:DataProtectionDir")
        if not (String.IsNullOrEmpty dataProtectionDir) && Directory.Exists dataProtectionDir then
            options.DataProtectionProvider <- DataProtectionProvider.Create(DirectoryInfo dataProtectionDir)
    )
services.AddAuthorization()

if disableAuth then
    services.AddSingleton<IAuthorizationHandler, AllowAnonymous>() |> ignore

services.AddMediatR(fun config -> config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()) |> ignore)

services.AddDbContext<MemoryDbContext>(
    (fun options -> options.UseSqlite(config.GetConnectionString("Memory")) |> ignore),
    contextLifetime = ServiceLifetime.Transient
)

services.AddTransient<DatabaseService>()

services.AddHostedService<MemoryBackgroundService>()

if RuntimeInformation.IsOSPlatform OSPlatform.Windows || RuntimeInformation.IsOSPlatform OSPlatform.Linux then
    services.AddHostedService<FaceTagBackgroundService>() |> ignore

// Frontend
services.AddRazorComponents().AddInteractiveServerComponents()
services.AddServerSideBlazor(fun x -> x.RootComponents.RegisterCustomElementForFunBlazor(Assembly.GetExecutingAssembly()))
services.AddFunBlazorServer()


let app = builder.Build()


do
    use scope = app.Services.GetRequiredService<IServiceProvider>().CreateScope()
    scope.ServiceProvider.GetRequiredService<DatabaseService>().Migrate().Result

    let appOptions = scope.ServiceProvider.GetRequiredService<IOptions<AppOptions>>()
    FFMediaToolkit.FFmpegLoader.FFmpegPath <- appOptions.Value.GetRootedFFmpegBinFolder(app.Environment)


app.Use(fun (ctx: HttpContext) (nxt: RequestDelegate) ->
    task {
        // So we can access request body for multiple times
        ctx.Request.EnableBuffering()
        do! nxt.Invoke ctx
    }
    :> Threading.Tasks.Task
)

app.UseForwardedHeaders()

app.UseStaticFiles()

app.UseAuthentication().UseAuthorization()

// Must put after auth middleware
app.UseAntiforgery()

app.Use(fun (ctx: HttpContext) (nxt: RequestDelegate) ->
    task {
        // Set user name for logging
        if ctx.User <> null && ctx.User.Claims <> null then
            let userName =
                ctx.User.Claims
                |> Seq.tryFind (fun x -> x.Type = ClaimTypes.Name)
                |> Option.map (fun x -> x.Value)
                |> Option.defaultValue "Unknown"
            Context.LogContext.PushProperty("UserName", userName)

        do! nxt.Invoke ctx
    }
    :> Threading.Tasks.Task
)

app.UseSerilogRequestLogging(fun options ->
    options.MessageTemplate <- "HTTP {RequestMethod} {UserName} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms"
)

app.MapMemory()
app.MapMemoryViews()

app.Run()
