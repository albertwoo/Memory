#nowarn "0020"

open System
open System.Reflection
open System.Runtime.InteropServices
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Server.Kestrel.Core
open Microsoft.AspNetCore.Http.Features
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Options
open Microsoft.Extensions.Configuration
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

services.AddSerilog(LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger())

// Options
services.AddOptions<AppOptions>().Bind(config.GetSection("App")).ValidateDataAnnotations()

services.Configure(fun (options: KestrelServerOptions) -> options.Limits.MaxRequestBodySize <- Int64.MaxValue)
services.Configure(fun (options: FormOptions) -> options.MultipartBodyLengthLimit <- Int64.MaxValue)
services.Configure(fun (options: ForwardedHeadersOptions) ->
    options.KnownProxies.Clear()
    options.KnownNetworks.Clear()
    options.ForwardedHeaders <- ForwardedHeaders.All
)

services.AddHttpContextAccessor()

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

// Must put after auth middleware
app.UseAntiforgery()

app.UseSerilogRequestLogging()

app.MapMemory()
app.MapMemoryViews()

app.Run()
