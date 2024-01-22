namespace Memory.Views.Components

open System
open Microsoft.AspNetCore.Components
open MediatR
open Fun.Htmx
open Fun.Result
open Fun.Blazor
open Memory
open Memory.Views

[<ComponentResponseCacheFor1Day>]
type MemoryDetailModal() as this =
    inherit FunComponent()

    [<Parameter>]
    member val Id = 0L with get, set

    [<Parameter>]
    member val Year = Nullable<int>() with get, set

    [<Parameter>]
    member val Month = Nullable<int>() with get, set

    [<Parameter>]
    member val Day = Nullable<int>() with get, set

    [<Parameter>]
    member val Tags = Parsable List.empty<string> with get, set

    [<Parameter>]
    member val ForPreviousOrNext = Nullable<bool>() with get, set

    [<Parameter>]
    member val ExcludeYear = false with get, set


    member _.Modal(memoryId: int64 option) =
        let title = fragment {
            match memoryId with
            | Some memoryId -> a {
                target "_blank"
                href $"memory/{memoryId}"
                class' "block text-sm md:text-lg text-center pb-2 link text-neutral-500/80 uppercase"
                $"Memory #{memoryId}"
              }
            | None -> ()
        }

        let contentId = $"content-{Random.Shared.Next()}"
        let previousButtonId = $"previous-{Random.Shared.Next()}"
        let nextButtonId = $"next-{Random.Shared.Next()}"
        let content = div {
            id contentId
            class' "h-full overflow-hidden flex flex-col items-stretch justify-center bg-transparent"
            childContent [|
                script {
                    $$"""
                    (function(){
                        const container = document.getElementById('{{contentId}}');

                        // Variables to store touch start and end positions
                        var touchStartX = 0;
                        var touchEndX = 0;

                        container.addEventListener('touchstart', function (event) {
                            touchStartX = event.touches[0].clientX;
                        });
                        container.addEventListener('touchend', function (event) {
                            touchEndX = event.changedTouches[0].clientX;
                            
                            const swipeDistance = touchEndX - touchStartX;
                            const swipeThreshold = 50;

                            if (swipeDistance > swipeThreshold) {
                                document.getElementById('{{previousButtonId}}').click()
                            } else if (swipeDistance < -swipeThreshold) {
                                document.getElementById('{{nextButtonId}}').click()
                            }
                        });
                    })()
                    """
                }
                match memoryId with
                | Some memoryId ->
                    script { NativeJs.UpdateQueries(queriesToAdd = [ GlobalQuery.CreateForMemoryId memoryId ]) }
                    html.blazor (ComponentAttrBuilder<MemoryDetail>().Add((fun x -> x.Id), memoryId))

                | None ->
                    script { NativeJs.UpdateQueries(queriesToDelete = [ GlobalQuery.MemoryId ]) }
                    div {
                        class' "text-3xl text-warning text-center font-bold"
                        "Not found"
                    }
            |]
        }

        let id' = defaultArg memoryId this.Id

        let sharedActionAttrs (forPreviousOrNext: bool) = domAttr {
            hxUpdateModal (
                QueryBuilder<MemoryDetailModal>()
                    .Add(this)
                    .Add((fun x -> x.Id), id')
                    .Add((fun x -> x.ForPreviousOrNext), (if memoryId.IsNone then Nullable() else Nullable forPreviousOrNext))
            )
            tabindex 0
            class' "join-item btn btn-sm"
        }

        let actions = html.fragment [|
            div {
                class' "join"
                childContent [|
                    if not (memoryId.IsNone && this.ForPreviousOrNext.Value) then
                        button {
                            id previousButtonId
                            hxTrigger' (hxEvt.mouse.click + "," + hxEvt.keyboard.keyup + "[key=='ArrowLeft']", from = "closest dialog")
                            sharedActionAttrs true
                            Icons.Left()
                        }
                    if not (memoryId.IsNone && this.ForPreviousOrNext.HasValue && not this.ForPreviousOrNext.Value) then
                        button {
                            id nextButtonId
                            hxTrigger' (hxEvt.mouse.click + "," + hxEvt.keyboard.keyup + "[key=='ArrowRight']", from = "closest dialog")
                            sharedActionAttrs false
                            Icons.Right()
                        }
                |]
            }
        |]

        Modal.Create(
            title = title,
            content = content,
            actions = actions,
            oncloseJs = NativeJs.UpdateQueries(queriesToDelete = [ GlobalQuery.MemoryId ])
        )

    override _.Render() =
        html.inject (fun (mediator: IMediator) -> task {
            let! currentId =
                match ValueOption.ofNullable this.ForPreviousOrNext with
                | ValueNone -> Task.retn (Some this.Id)
                | ValueSome true ->
                    mediator.Send(
                        Domain.``Get previous available id`` (
                            this.Id,
                            Year = Option.ofNullable this.Year,
                            Month = Option.ofNullable this.Month,
                            Day = Option.ofNullable this.Day,
                            Tags = this.Tags.Value,
                            ExcludeYear = this.ExcludeYear
                        )
                    )
                | ValueSome false ->
                    mediator.Send(
                        Domain.``Get next available id`` (
                            this.Id,
                            Year = Option.ofNullable this.Year,
                            Month = Option.ofNullable this.Month,
                            Day = Option.ofNullable this.Day,
                            Tags = this.Tags.Value,
                            ExcludeYear = this.ExcludeYear
                        )
                    )
            return this.Modal(currentId)
        })
