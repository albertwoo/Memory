namespace Memory.Views.Pages

open Microsoft.Extensions.Options
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.AspNetCore.Authorization
open Fun.Blazor
open Memory.Options
open Memory.Views.Components

[<Route "/memory/{id:long}">]
[<Authorize>]
type MemoryDetailPage() as this =
    inherit FunComponent()

    [<Parameter>]
    member val id = 0L with get, set

    override _.Render() =
        html.inject (fun (appOptions: IOptions<AppOptions>) -> main {
            class' "p-5"
            PageTitle'' { appOptions.Value.Title + " #" + string this.id }
            section {
                class' "max-h-[calc(100vh-100px)] overflow-hidden flex flex-col items-center justify-start"
                html.blazor (ComponentAttrBuilder<MemoryDetail>().Add((fun x -> x.Id), this.id))
            }
        })
