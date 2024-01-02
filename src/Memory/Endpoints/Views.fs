namespace Memory.Endpoints

open System.Reflection
open System.Threading.Tasks
open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Fun.Htmx
open Fun.Blazor
open Memory.Views
open Memory.Views.Pages

[<Extension>]
type MemoryViews =

    [<Extension>]
    static member MapMemoryViews(app: WebApplication) =
        let loginPageUrl = $"/fun-blazor-server-side-render-components/{typeof<LoginPage>.FullName}"
        let resetPasswordPageUrl = $"/fun-blazor-server-side-render-components/{typeof<ResetPasswordPage>.FullName}"

        let ssrGroup =
            app
                .MapGroup("")
                .AddFunBlazor()
                .AddEndpointFilter(fun ctx nxt ->
                    task {
                        if
                            ctx.HttpContext.Request.Path.StartsWithSegments(loginPageUrl)
                            || ctx.HttpContext.Request.Path.StartsWithSegments(resetPasswordPageUrl)
                            || (ctx.HttpContext.User <> null && ctx.HttpContext.User.Identity.IsAuthenticated)
                        then
                            return! nxt.Invoke(ctx)
                        else
                            return box (script { NativeJs.GoTo "/account/login" })
                    }
                    |> ValueTask<obj>
                )

        ssrGroup.MapRazorComponentsForSSR(Assembly.GetExecutingAssembly(), enableAntiforgery = true)
        ssrGroup.MapCustomElementsForSSR(Assembly.GetExecutingAssembly(), enableAntiforgery = true)

        app.MapRazorComponents<App>().AddInteractiveServerRenderMode()
