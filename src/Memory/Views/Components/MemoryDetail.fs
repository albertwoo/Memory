namespace Memory.Views.Components

open Microsoft.Extensions.Options
open Microsoft.AspNetCore.Components
open Microsoft.EntityFrameworkCore
open MediatR
open Fun.Htmx
open Fun.Blazor
open Memory
open Memory.Db
open Memory.Domain
open Memory.Options
open Memory.Views

[<ComponentResponseCacheFor1Day>]
type MemoryDetail() as this =
    inherit FunComponent()

    [<Parameter>]
    member val Id = 0L with get, set

    override _.Render() =
        html.inject (fun (mediator: IMediator, memoryDb: MemoryDbContext, cssRules: IScopedCssRules, appOptions: IOptions<AppOptions>) -> task {
            let! memory = memoryDb.Memories.AsNoTracking().FirstOrDefaultAsync(fun x -> x.Id = this.Id)
            do! Domain.``Increase views for memory`` (this.Id) |> mediator.Send

            let openMetaAttr = domAttr {
                hxTrigger hxEvt.mouse.click
                hxModal (QueryBuilder<MemoryMetaModal>().Add((fun x -> x.Id), this.Id))
            }

            match memory with
            | null -> return div { "not found" }
            | _ ->
                let mediaView =
                    div {
                        class' "h-full flex-shrink overflow-hidden flex flex-col items-center justify-center"
                        childContent [|
                            match memory.FileExtension with
                            | VideoFormat -> video {
                                controls
                                autoplay
                                loop
                                class' "max-h-full w-auto cursor-pointer shadow-2xl shadow-neutral-500/30"
                                poster (AppOptions.CreateOptimizedUrlForImage(this.Id) |> appOptions.Value.AppendWithVersion)
                                source {
                                    src (
                                        match transformedFormat memory.FileExtension with
                                        | None -> $"/memory/original/{memory.Id}"
                                        | Some _ -> AppOptions.CreateOptimizedUrlForVideo(this.Id)
                                        |> appOptions.Value.AppendWithVersion
                                    )
                                }
                              }
                            | ImageFormat -> img {
                                openMetaAttr
                                class' "max-h-full w-auto cursor-pointer shadow-2xl shadow-neutral-500/30"
                                src (
                                    match transformedFormat memory.FileExtension with
                                    | None -> $"/memory/original/{memory.Id}"
                                    | Some _ -> AppOptions.CreateOptimizedUrlForImage(this.Id)
                                    |> appOptions.Value.AppendWithVersion
                                )
                              }
                            | _ -> p {
                                class' "text-warning"
                                $"Unsupported format: {memory.FileExtension}"
                              }
                        |]
                    }

                return section {
                    class' "flex flex-col items-center justify-start gap-2 overflow-hidden h-full mx-auto max-w-[1920px]"
                    style { cssRules.FadeInUpCss(delay = 200) }
                    childContent [|
                        mediaView
                        div {
                            class' "flex justify-center items-center flex-wrap gap-2"
                            html.blazor (ComponentAttrBuilder<TagLists>().Add((fun x -> x.MemoryId), this.Id).Add((fun x -> x.EnableEdit), true))
                        }
                        div {
                            class' "flex items-end gap-2"
                            childContent [|
                                p {
                                    class' "flex items-center gap-1 cursor-pointer opacity-40 hover:opacity-75 sm:text-lg md:text-2xl text-neutral-500 font-bold md:font-extrabold"
                                    openMetaAttr
                                    childContent [|
                                        Icons.InfoCircle(class' = "w-6 h-6")
                                        span { memory.CreationTime.ToString("yyyy-MM-dd HH:mm:ss") + $" 👁️ {memory.Views + 1}" }
                                    |]
                                }
                                a {
                                    href ($"/memory/original/{memory.Id}" |> appOptions.Value.AppendWithVersion)
                                    download true
                                    class' "btn btn-link btn-sm btn-square"
                                    Icons.ArrowDownTray()
                                }
                            |]
                        }
                    |]
                }
        })
