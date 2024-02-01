namespace rec Memory.Views.Components

open System
open Microsoft.AspNetCore.Components
open MediatR
open Fun.Htmx
open Fun.Blazor
open Memory
open Memory.Views


type BatchTagsModal() as this =
    inherit FunComponent()

    static member ThumbnailChecker = "thumbnail-checker"

    [<Parameter>]
    member val Ids = Parsable List.empty<int64> with get, set

    [<Parameter>]
    member val Tags = Parsable List.empty<string> with get, set

    [<Parameter>]
    member val Submit = false with get, set


    member _.FormId = "batch-tags"


    override _.Render() =
        html.inject (fun (mediator: IMediator) -> task {
            if this.Submit && this.Ids.Value.Length > 0 && this.Tags.Value.Length > 0 then
                do! Domain.``Add tag to memeory``(this.Ids.Value, this.Tags.Value) |> mediator.Send
                
                return script {
                    hxTrigger hxEvt.load
                    hxGetComponent typeof<BatchTagsIndicatorBtn>
                    hxTarget $"#{BatchTagsIndicatorBtn.Id}"
                    hxSwap_outerHTML

                    $"document.querySelectorAll('.{BatchTagsModal.ThumbnailChecker}').forEach(x => x.checked = '')"
                }

            else
                return
                    Modal.Create(
                        title = span { $"Add tags to selected memories {this.Ids.Value.Length} in batch" },
                        content = form {
                            id this.FormId
                            hxGetComponent (QueryBuilder<TagFilterList>().Add((fun x -> x.SelectedTagsParamName), nameof this.Tags))
                            class' "flex flex-wrap items-center gap-2 mt-2"
                            html.blazor(ComponentAttrBuilder<TagFilterList>().Add((fun x -> x.SelectedTagsParamName), nameof this.Tags))
                        },
                        actions = fragment {
                            button {
                                hxTrigger hxEvt.mouse.click
                                hxUpdateModal (
                                    QueryBuilder<BatchTagsModal>().Add(this).Add((fun x -> x.Submit), true)
                                )
                                hxInclude $"#{this.FormId}"
                               
                                disabled this.Ids.Value.IsEmpty
                                class' "btn btn-primary btn-ghost"
                               
                                "Confirm"
                            }
                        },
                        size = ModalSize.Medium
                    )
        })


type BatchTagsIndicatorBtn() as this =
    inherit FunComponent()
      
    [<Parameter>]
    member val SelectedIds = Parsable List.empty<int64> with get, set


    // Ids from this property will always be merged into selected ids
    [<Parameter>]
    member val IncludeIds = Parsable List.empty<int64> with get, set


    [<Parameter>]
    member val SelectedId = Nullable<int64>() with get, set
    // This comes from checkbox, expected to be on
    [<Parameter>]
    member val IncludeSelectedId = "" with get, set


    override _.Render() =
        let selectedIds = 
            if this.SelectedId.HasValue && this.IncludeSelectedId = "on" then
                [this.SelectedId.Value] |> List.append this.SelectedIds.Value
            else if this.SelectedId.HasValue then
                this.SelectedIds.Value |> List.filter ((<>) this.SelectedId.Value)
            else
                this.SelectedIds.Value
            |> List.append this.IncludeIds.Value
            |> List.distinct

        let count = selectedIds.Length

        this.SelectedIds <- Parsable selectedIds

        button {
            id BatchTagsIndicatorBtn.Id
            hxTrigger hxEvt.mouse.click
            hxModal (QueryBuilder<BatchTagsModal>().Add((fun x -> x.Ids), this.SelectedIds))
            
            class' "btn btn-circle shadow-md opacity-70 hover:opacity-100 relative"
            
            childContent [|
                // So other components can include this parameters for htmx
                input {
                    type' InputTypes.hidden
                    name (nameof this.SelectedIds)
                    value this.SelectedIds
                }
                
                Icons.Tag()

                progress { class' "htmx-indicator absolute left-0 top-0 right-0 bottom-0 loading loading-spinner w-full h-full p-1" }

                // Indicator
                if count > 0 then
                    div {
                        class' "absolute left-0 top-0 right-0 bottom-0 w-full indicator"
                        span {
                            class' "indicator-item badge badge-secondary"
                            if count > 99 then "99+" else string count
                        }
                    }
            |]
        }

        
    static member Id = "batch-tags-indicator-btn"
    static member BatchSelectionBtnId = "batch-tags-selection-btn"

    static member Scripts() = html.fragment [|
        button {
            id BatchTagsIndicatorBtn.BatchSelectionBtnId
            hxTrigger hxEvt.mouse.click
            hxGetComponent typeof<BatchTagsIndicatorBtn>
            hxTarget $"#{BatchTagsIndicatorBtn.Id}"
            hxInclude $"#{BatchTagsIndicatorBtn.Id}"
            hxSwap_outerHTML
            name (nameof Unchecked.defaultof<BatchTagsIndicatorBtn>.IncludeIds)
            type' InputTypes.hidden
        }
        script {
            $$"""
            document.addEventListener("mouseup", () => {
                const selection = window.getSelection();
                if (selection.baseNode && selection.baseNode.tagName === 'A' 
                    && selection.focusNode && selection.focusNode.tagName === 'A'
                    && selection.baseNode.parentNode === selection.focusNode.parentNode
                    && selection.baseNode.parentNode.classList.contains("thumbnails")
                ) {
                    let i1 = Array.prototype.indexOf.call(selection.baseNode.parentNode.children, selection.baseNode);
                    let i2 = Array.prototype.indexOf.call(selection.focusNode.parentNode.children, selection.focusNode);
                    if (i1 > i2) {
                        let temp = i1;
                        i1 = i2;
                        i2 = temp;
                    }
                    if (i1 >= 0 && i2 - i1 >= 0) {
                        let ids = "";
                        for (var i = i1; i <= i2; i++) {
                            const a = selection.focusNode.parentNode.children[i];
                            const checkbox = a.querySelector("input");
                            checkbox.checked = true;
                            if (ids === "") {
                                ids = a.id.toString();
                            }
                            else {
                                ids += "," + a.id.toString();
                            }
                        }
                        const btn = document.querySelector("#{{BatchTagsIndicatorBtn.BatchSelectionBtnId}}");
                        btn.value = "[" + ids + "]";
                        btn.click();
                        selection.empty();
                    }
                }
            });
            """
        }
    |]
