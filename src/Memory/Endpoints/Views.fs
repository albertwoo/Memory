namespace Memory.Endpoints

open System.Reflection
open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Memory.Views

[<Extension>]
type MemoryViews =

    [<Extension>]
    static member MapMemoryViews(app: WebApplication) =
        let ssrGroup = app.MapGroup("").AddFunBlazor()

        ssrGroup.MapRazorComponentsForSSR(Assembly.GetExecutingAssembly(), enableAntiforgery = true)
        ssrGroup.MapCustomElementsForSSR(Assembly.GetExecutingAssembly(), enableAntiforgery = true)

        app.MapRazorComponents<App>().AddInteractiveServerRenderMode()
