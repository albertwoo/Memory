namespace Memory.Views.Components

open System
open Fun.Htmx
open Fun.Blazor
open Fun.Blazor.Operators
open Memory.Views

[<RequireQualifiedAccess>]
type ModalSize = 
    | Full
    | Medium

type Modal =

    static member ContainerId = "modals-container"

    /// Should put at the root of the body
    static member Container() = section { id Modal.ContainerId }

    static member Target = "closest .modal"
    static member Indicator = ".modal .htmx-indicator"

    static member RemoveLastModalJs() =
        js
            $$"""
            (function(){
                const id = window.modalIds[window.modalIds.length - 1];
                document.querySelector(`${id}`).remove();
                window.modalIds = window.modalIds.slice(0, -1);
            })()
            """

    static member Create
        (
            title: NodeRenderFragment,
            content: NodeRenderFragment,
            ?size: ModalSize,
            ?actions: NodeRenderFragment,
            ?containerAttrs: AttrRenderFragment,
            ?oncloseJs: string
        ) =
        html.inject (fun (cssRules: IScopedCssRules) ->
            let content' = content
            let randomId = Random.Shared.Next()
            let dialogId = $"dialog-{randomId}"

            let modalSize = defaultArg size ModalSize.Full

            let sizeClasses =
                match modalSize with
                | ModalSize.Full -> "w-full sm:w-[calc(100%-80px)] max-w-full h-full sm:h-[calc(100%-80px)] max-h-full sm:border sm:border-primary/20 px-0 sm:px-2"
                | ModalSize.Medium -> "border border-primary/20"

            let removeModalJs = 
                js
                    $$"""
                    (function(){
                        {{defaultArg oncloseJs ""}}
                        document.querySelector('#{{dialogId}}').remove();
                        window.modalIds = window.modalIds.slice(0, -1);
                    })()"""

            let modalJs =
                js
                    $$"""
                    (function(){
                        if (!window.modalIds) {
                            window.modalIds = [];
                        }
                        window.modalIds = [...window.modalIds, {{randomId}}];
                        const closeModalHanlder = e => {
                            if (e.code === 'Escape') {
                                e.stopImmediatePropagation();
                                document.removeEventListener("keyup", closeModalHanlder);
                                try {
                                    const id = window.modalIds[window.modalIds.length-1];
                                    window.modalIds = window.modalIds.slice(0, -1);
                                    document.querySelector(`#dialog-${id}`).remove();
                                } catch {}
                                {{defaultArg oncloseJs ""}}
                            }
                        }
                        document.addEventListener("keyup", closeModalHanlder);
                    })()"""

            let closeAttr = domAttr { 
                tabindex 0
                autofocus
                on.click removeModalJs 
            }

            dialog {
                id dialogId
                closeAttr
                defaultArg containerAttrs html.emptyAttr
                class' "modal modal-open outline-none"

                div {
                    on.click "event.stopPropagation()"
                    class' $"modal-box bg-base-100/90 p-2 md:p-5 flex flex-col items-stretch overflow-hidden gap-1 sm:gap-2 {sizeClasses}"
                    style { cssRules.FadeInUpCss() }
                    h3 {
                        class' "font-bold text-lg"
                        title
                    }
                    section { 
                        class' "h-full overflow-hidden flex flex-col items-stretch"
                        content'
                    }
                    section {
                        class' "modal-action items-center mt-0"
                        button { class' "htmx-indicator btn loading loading-spinner text-info" }
                        defaultArg actions html.none
                        button {
                            class' "btn btn-ghost btn-circle"
                            closeAttr
                            Icons.Clear()
                        }
                    }
                    script { modalJs }
                }
            }
        )

[<AutoOpen>]
module ModalDsl =
    type DomAttrBuilder with

        /// Insert a component to the modal container
        [<CustomOperation "hxModal">]
        member inline _.hxModal<'T>([<InlineIfLambda>] render: AttrRenderFragment, ?query: QueryBuilder<'T>) =
            render
            ==> domAttr {
                hxGetComponent (query |> Option.defaultWith (fun () -> QueryBuilder<'T>()))
                hxTarget $"#{Modal.ContainerId}"
                hxSwap_beforeend
            }

        /// Update closest modal with new modal content, the element which use this attribute should be in the modal
        [<CustomOperation "hxUpdateModal">]
        member inline _.hxUpdateModal<'T>
            (
                [<InlineIfLambda>] render: AttrRenderFragment,
                ?query: QueryBuilder<'T>
            ) =
            render
            ==> domAttr {
                hxGetComponent (query |> Option.defaultWith (fun () -> QueryBuilder<'T>()))
                hxSwap_outerHTML
                hxTarget Modal.Target
                hxIndicator Modal.Indicator
            }
