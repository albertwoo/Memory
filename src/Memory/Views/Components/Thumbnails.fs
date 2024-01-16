namespace Memory.Views.Components

open System
open System.Linq
open Microsoft.Extensions.Options
open Microsoft.AspNetCore.Components
open MediatR
open Fun.Htmx
open Fun.Blazor
open Memory
open Memory.Options
open Memory.Domain
open Memory.Views


type Thumbnail() as this =
    inherit FunComponent()

    [<Parameter>]
    member val Id = 0L with get, set

    [<Parameter>]
    member val FileExtension = "" with get, set

    [<Parameter>]
    member val ViewSize = ThumbnailSize.Medium with get, set

    [<Parameter>]
    member val ForHisotryOfToday = false with get, set


    member _.ThumbnailSizeClass =
        match this.ViewSize with
        | ThumbnailSize.Small -> "h-10 w-10"
        | ThumbnailSize.Medium -> "h-20 w-20"
        | ThumbnailSize.Large -> "h-32 w-32"
        | _ -> "h-2 w-2"


    override _.Render() =
        html.inject (fun (cssRules: IScopedCssRules, appOptions: IOptions<AppOptions>) ->
            let modalAttr =
                if this.ForHisotryOfToday then
                    domAttr {
                        hxModal (
                            QueryBuilder<MemoryDetailModal>()
                                .Add((fun x -> x.Id), this.Id)
                                .Add((fun x -> x.Year), Nullable DateTime.Now.Year)
                                .Add((fun x -> x.ExcludeYear), true)
                                .Add((fun x -> x.Month), Nullable DateTime.Now.Month)
                                .Add((fun x -> x.Day), Nullable DateTime.Now.Day)
                        )
                    }
                else
                    domAttr {
                        hxModal (QueryBuilder<MemoryDetailModal>().Add((fun x -> x.Id), this.Id))
                        hxAddQueriesToHtmxParams
                    }
            a {
                id this.Id

                // by default, open a modal
                hxTrigger' hxEvt.mouse.click
                modalAttr

                // url for the new tab
                href $"memory/{this.Id}"

                class' (
                    "group bg-slate-100 cursor-pointer relative overflow-hidden break-all transition-all hover:scale-110 hover:z-10 hover:border-2 hover:border-secondary/50 hover:rounded-md hover:shadow-md hover:shadow-secondary/40 outline-none focus:scale-110 focus:z-10 focus:border-2 focus:border-secondary/50 focus:rounded-md focus:shadow-md focus:shadow-secondary/40 "
                    + this.ThumbnailSizeClass
                )
                style { cssRules.FadeInUpCss() }

                img {
                    class' ("pointer-events-none " + this.ThumbnailSizeClass)
                    src (AppOptions.CreateOptimizedUrlForImage(this.Id, this.ViewSize.ToPixel()) |> appOptions.Value.AppendWithVersion)
                }
                span {
                    class' (
                        "absolute right-0 top-0 badge badge-xs uppercase pointer-events-none focus:opacity-80 "
                        + (
                            match this.FileExtension with
                            | VideoFormat -> "opacity-40 sm:group-hover:opacity-80"
                            | _ -> "badge-secondary opacity-0 sm:hidden sm:group-hover:inline-block"
                        )
                    )
                    if String.IsNullOrEmpty this.FileExtension |> not then
                        this.FileExtension.Substring(1)
                }
                progress { class' "htmx-indicator absolute left-0 top-0 right-0 bottom-0 loading loading-bars w-full h-full p-2" }
                input {
                    hxTrigger hxEvt.mouse.click
                    hxGetComponent (QueryBuilder<BatchTagsIndicatorBtn>().Add((fun x -> x.SelectedId), Nullable this.Id))
                    hxInclude $"#{BatchTagsIndicatorBtn.Id}"
                    hxTarget $"#{BatchTagsIndicatorBtn.Id}"
                    hxSwap_outerHTML
                    name (nameof Unchecked.defaultof<BatchTagsIndicatorBtn>.IncludeSelectedId)

                    type' InputTypes.checkbox
                    on.click "event.stopPropagation()"
                    class'
                        $"{BatchTagsModal.ThumbnailChecker} absolute right-0 bottom-0 checkbox checkbox-primary border-2 opacity-0 sm:opacity-70 sm:hidden sm:group-hover:block checked:opacity-70 checked:block"
                }
            }
        )


type Thumbnails() as this =
    inherit FunComponent()

    [<Parameter>]
    member val Year = DateTime.Now.Year with get, set

    [<Parameter>]
    member val Month = DateTime.Now.Month with get, set

    [<Parameter>]
    member val Day = Nullable<int>() with get, set

    [<Parameter>]
    member val Tags = Parsable List.empty<string> with get, set

    [<Parameter>]
    member val Page = 0L with get, set

    [<Parameter>]
    member val Count = Nullable<int64>() with get, set

    [<Parameter>]
    member val ViewSize = ThumbnailSize.Medium with get, set

    [<Parameter>]
    member val ForHisotryOfToday = false with get, set


    member _.PageSize =
        match this.ViewSize with
        | ThumbnailSize.Small -> 30
        | ThumbnailSize.Medium -> 20
        | ThumbnailSize.Large -> 10
        | _ -> 50


    member _.RestThumbnails() = div {
        hxTrigger' (hxEvt.intersect, once = true)
        hxGetComponent (QueryBuilder<Thumbnails>().Add(this).Add((fun x -> x.Page), this.Page + 1L))
        hxSwap_outerHTML
        class' "loading loading-bars loading-md"
    }

    override _.Render() =
        html.inject (fun (mediator: IMediator) -> task {
            try
                let fromTime = DateTime(this.Year, this.Month, (if this.Day.HasValue then this.Day.Value else 1))
                let toTime = if this.Day.HasValue then fromTime.AddDays(1) else fromTime.AddMonths(1)
                let offset = this.Page * int64 this.PageSize

                if this.Count.HasValue |> not then
                    let! count =
                        Domain.``Count ordered memories by creation time desc with filter`` (fromTime, toTime, Tags = this.Tags.Value)
                        |> mediator.Send
                    this.Count <- count

                let! memories =
                    Domain.``Get memory ids with filter and ordered by creation time desc`` (
                        fromTime,
                        toTime,
                        Offset = offset,
                        PageSize = this.PageSize,
                        Tags = this.Tags.Value
                    )
                    |> mediator.Send

                return fragment {
                    for memory in memories do
                        html.blazor (
                            ComponentAttrBuilder<Thumbnail>()
                                .Add((fun x -> x.Id), memory.Id)
                                .Add((fun x -> x.FileExtension), memory.Extension)
                                .Add((fun x -> x.ViewSize), this.ViewSize)
                                .Add((fun x -> x.ForHisotryOfToday), this.ForHisotryOfToday)
                        )
                    // If still got data
                    if this.Count.Value > (offset + memories.LongCount()) then this.RestThumbnails()
                }

            with :? ArgumentOutOfRangeException as e when e.Message.Contains "DateTime" ->
                return html.none
        })


type ThumbnailsByDay() as this =
    inherit FunComponent()

    [<Parameter>]
    member val Year = DateTime.Now.Year with get, set

    [<Parameter>]
    member val Month = DateTime.Now.Month with get, set

    [<Parameter>]
    member val Tags = Parsable List.empty<string> with get, set

    [<Parameter>]
    member val ViewSize = ThumbnailSize.Medium with get, set

    [<Parameter>]
    member val MaxCountForDay = 1 with get, set


    override _.Render() =
        html.inject (fun (mediator: IMediator) -> task {
            let lastDayInMonth = DateTime.DaysInMonth(this.Year, this.Month)

            let! memories =
                Domain.``Get memory counts by month with filter and ordered by day desc`` (
                    this.Year,
                    this.Month,
                    Tags = this.Tags.Value
                )
                |> mediator.Send

            let createQuery () =
                QueryBuilder()
                    .Add(
                        GlobalQuery.ViewSize,
                        match this.ViewSize with
                        | ThumbnailSize.ExtraSmallByDay
                        | ThumbnailSize.ExtraSmallByMonth -> ThumbnailSize.Medium
                        | _ -> this.ViewSize
                    )
                    .Add(GlobalQuery.Year, this.Year)
                    .Add(GlobalQuery.Month, this.Month)
                    .Add(GlobalQuery.Tags, this.Tags)

            return fragment {
                a {
                    target "_blank"
                    href ("?" + createQuery().ToString())
                    class' "btn btn-ghost btn-xs btn-primary btn-circle"
                    this.Month
                }
                for day in 31 .. (-1) .. 1 do
                    let memory = memories |> Seq.tryFind (fun x -> x.Day = day)
                    let memoryCount = memory |> Option.map (fun x -> x.Count) |> Option.defaultValue 0

                    let heatView transparent = div {
                        class' "bg-success rounded-sm"
                        style {
                            width (this.ViewSize.ToPixel())
                            height (this.ViewSize.ToPixel())
                            opacity (
                                if transparent then
                                    0.
                                else
                                    Math.Max((if memoryCount > 0 then 0.15 else 0.05), float memoryCount / float this.MaxCountForDay)
                            )
                        }
                    }

                    if day <= lastDayInMonth then
                        a {
                            target "_blank"
                            href ("?" + createQuery().Add(GlobalQuery.Day, day).ToString())
                            data "tip" (string day + ": #" + string memoryCount)
                            class' "tooltip tooltip-primary"
                            heatView false
                        }
                    else
                        heatView true
            }
        })


type ThumbnailsByMonth() as this =
    inherit FunComponent()

    [<Parameter>]
    member val Year = DateTime.Now.Year with get, set

    [<Parameter>]
    member val Month = DateTime.Now.Month with get, set

    [<Parameter>]
    member val Tags = Parsable List.empty<string> with get, set

    [<Parameter>]
    member val ViewSize = ThumbnailSize.Medium with get, set

    [<Parameter>]
    member val MaxCountForMonth = 1 with get, set


    override _.Render() =
        html.inject (fun (mediator: IMediator, cssRules: IScopedCssRules) -> task {
            let! count =
                Domain.``Get memory max count by month with filter`` (
                    Year = Some this.Year,
                    Month = Some this.Month,
                    Tags = this.Tags.Value
                )
                |> mediator.Send
            let percent = float count / float this.MaxCountForMonth
            return a {
                target "_blank"
                href (
                    "?"
                    + QueryBuilder()
                        .Add(
                            GlobalQuery.ViewSize,
                            match this.ViewSize with
                            | ThumbnailSize.ExtraSmallByDay
                            | ThumbnailSize.ExtraSmallByMonth -> ThumbnailSize.Medium
                            | _ -> this.ViewSize
                        )
                        .Add(GlobalQuery.Year, this.Year)
                        .Add(GlobalQuery.Month, this.Month)
                        .Add(GlobalQuery.Tags, this.Tags)
                        .ToString()
                )
                class' "relative block"
                style { cssRules.FadeInUpCss() }

                div {
                    class' "bg-success h-full absolute top-0"
                    style {
                        width $"{percent * 100.}%%"
                        left $"{(100. - percent * 100.) / 2.}%%"
                        opacity (Math.Max((if percent > 0 then 0.15 else 0.05), percent))
                    }
                }
                h3 {
                    class' "font-semibold text-center text-primary text-xs"
                    this.Month
                }
            }
        })
