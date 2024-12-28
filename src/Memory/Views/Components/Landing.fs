namespace Memory.Views.Components

open Microsoft.Extensions.Options
open Fun.Blazor
open Memory.Views
open Memory.Options

type Landing =
    static member Title() =
        html.inject (fun (appOptions: IOptions<AppOptions>) -> div {
            class' "flex items-center gap-2"
            img {
                src "favicon.png"
                class' "h-10 w-10"
            }
            a {
                href "/"
                class' "text-2xl text-center font-bold text-primary my-4"
                appOptions.Value.Title
            }
            if not appOptions.Value.DisableAuth then
                a {
                    href "/account/login"
                    Icons.Fingerprint(class' = "w-6 h-6 text-primary")
                }
        })

    static member SubTitle() =
        html.inject (fun (appOptions: IOptions<AppOptions>, cssRules: IScopedCssRules) -> section {
            class' "flex items-center flex-col text-sm mx-2"
            div {
                class' "flex items-center justify-center flex-wrap"
                span {
                    class' "mr-1"
                    appOptions.Value.SubTitleLine1_1
                }
                region {
                    yield!
                        appOptions.Value.SubTitleLine1_2
                        |> Seq.mapi (fun i char ->
                            cssRules.RevealSpan(
                                char,
                                delay = i * 300 + 2500,
                                duration = 1000,
                                attr = domAttr { class' "font-bold text-primary" }
                            )
                        )
                }
                span { appOptions.Value.SubTitleLine1_3 }
            }
            div { appOptions.Value.SubTitleLine2_1 }
        })
