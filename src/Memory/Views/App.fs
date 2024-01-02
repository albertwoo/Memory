namespace Memory.Views

open System.Reflection
open Microsoft.Extensions.Options
open Microsoft.AspNetCore.Components.Web
open Microsoft.AspNetCore.Components.Authorization
open Fun.Blazor
open Memory.Options
open Memory.Views
open Memory.Views.Components


type App() =
    inherit FunComponent()

    override _.Render() =
        html.inject (fun (appOptions: IOptions<AppOptions>) -> fragment {
            doctype "html"
            html' {
                lang "CN"
                data "theme" appOptions.Value.Theme
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
                        href ("favicon.png" |> appOptions.Value.AppendWithVersion)
                    }

                    stylesheet ("app-generated.css" |> appOptions.Value.AppendWithVersion)
                    html.scopedCssRules

                    HeadOutlet'()
                    CustomElement.lazyBlazorJs ()
                }
                body {
                    Modal.Container()
                    section {
                        class' "flex flex-col items-center justify-center"
                        Landing.Title()
                        Landing.SubTitle()
                    }
                    Router'() {
                        AppAssembly(Assembly.GetExecutingAssembly())
                        Found(fun routeData ->
                            html.blazor (
                                ComponentAttrBuilder<AuthorizeRouteView>()
                                    .Add((fun x -> x.RouteData), routeData)
                                    .Add((fun x -> x.NotAuthorized), html.renderFragment (fun _ -> html.blazor<Pages.LoginPage> ()))
                            )
                        )
                    }
                    script { src "htmx.org@1.9.9.js" }
                }
            }
        })
