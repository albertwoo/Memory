namespace Memory.Views

open System.IO
open System.Reflection
open Microsoft.Extensions.Options
open Microsoft.AspNetCore.Components.Web
open Microsoft.AspNetCore.Components.Authorization
open Fun.Blazor
open Memory.Options
open Memory.Views
open Memory.Views.Components
open Microsoft.AspNetCore.Components


type App() as this =
    inherit FunComponent()


    [<Inject>]
    member val AppOptions = Unchecked.defaultof<IOptions<AppOptions>> with get, set


    override _.Render() = fragment {
        doctype "html"
        html' {
            lang "CN"
            data "theme" this.AppOptions.Value.Theme
            head {
                baseUrl "/"
                meta { charset "utf-8" }
                meta {
                    name "viewport"
                    content "width=device-width, initial-scale=1.0"
                }
                link {
                    rel "icon"
                    type' "image/png"
                    href ("favicon.png" |> this.AppOptions.Value.AppendWithVersion)
                }

                stylesheet ("app-generated.css" |> this.AppOptions.Value.AppendWithVersion)
                html.scopedCssRules

                HeadOutlet''
                CustomElement.lazyBlazorJs ()
            }
            body {
                Modal.Container()
                section {
                    class' "flex flex-col items-center justify-center"
                    Landing.Title()
                    Landing.SubTitle()
                }
                Router'' {
                    AppAssembly(Assembly.GetExecutingAssembly())
                    Found(fun routeData ->
                        html.blazor (
                            ComponentAttrBuilder<AuthorizeRouteView>()
                                .Add((fun x -> x.RouteData), routeData)
                                .Add((fun x -> x.NotAuthorized), html.renderFragment (fun _ -> html.blazor<Pages.LoginPage> ()))
                        )
                    )
                }
                script { src "htmx.org@2.0.3.js" }
                script { src "htmx-ext-sse@2.2.2.js" }
                if File.Exists "analytics.txt" then html.raw (File.ReadAllText "analytics.txt")
            }
        }
    }
