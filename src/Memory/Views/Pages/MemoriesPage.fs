namespace Memory.Views.Pages

open System
open System.Linq
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.AspNetCore.Authorization
open Microsoft.Extensions.Options
open Microsoft.EntityFrameworkCore
open MediatR
open Fun.Result
open Fun.Blazor
open Fun.Htmx
open Memory.Db
open Memory.Options
open Memory.Domain
open Memory.Views
open Memory.Views.Components


[<Route "/">]
[<Authorize>]
type MemoriesPage() as this =
    inherit FunComponent()

    [<Parameter; SupplyParameterFromQuery(Name = GlobalQuery.ViewSize)>]
    member val ViewSize = ThumbnailSize.Medium.ToString() with get, set

    [<Parameter; SupplyParameterFromQuery(Name = GlobalQuery.MemoryId)>]
    member val MemoryId = Nullable<Int64>() with get, set

    [<Parameter; SupplyParameterFromQuery(Name = GlobalQuery.Year)>]
    member val Year = Nullable<int>() with get, set

    [<Parameter; SupplyParameterFromQuery(Name = GlobalQuery.Month)>]
    member val Month = Nullable<int>() with get, set

    [<Parameter; SupplyParameterFromQuery(Name = GlobalQuery.Day)>]
    member val Day = Nullable<int>() with get, set

    [<Parameter; SupplyParameterFromQuery(Name = GlobalQuery.Tags)>]
    member val Tags = "" with get, set


    member _.SafeTags: Parsable<string list> =
        try
            Parsable.Parse(this.Tags)
        with _ ->
            Parsable []

    member _.SafeViewSize =
        try
            ThumbnailSize.Parse this.ViewSize
        with _ ->
            ThumbnailSize.Medium

    member _.PageId = "memories-page"

    member _.PageIndicatorId = "memories-page-indicator"

    member _.Query = QueryBuilder<MemoriesPage>().Add(this).Remove(fun x -> x.MemoryId)

    member _.AttrForUpdateCurrentPage = domAttr {
        hxTarget $"#{this.PageId}"
        hxIndicator $"#{this.PageIndicatorId}"
        hxSwap_outerHTML
    }


    member _.SizeFilter() = fragment {
        yield!
            [
                ThumbnailSize.ExtraSmallByDay, "HD"
                ThumbnailSize.ExtraSmallByMonth, "HM"
                ThumbnailSize.Small, "S"
                ThumbnailSize.Medium, "M"
                ThumbnailSize.Large, "L"
            ]
            |> Seq.map (fun (viewSize, txt) -> div {
                class' "tooltip tooltip-primary"
                data
                    "tip"
                    (match viewSize with
                     | ThumbnailSize.ExtraSmallByDay -> "Heat map by day"
                     | ThumbnailSize.ExtraSmallByMonth -> "Heat map by month"
                     | _ -> string viewSize)
                input {
                    class' "join-item btn btn-sm"
                    hxTrigger' hxEvt.mouse.click
                    hxGetComponent (this.Query.Add((fun x -> x.ViewSize), viewSize.ToString()))
                    this.AttrForUpdateCurrentPage

                    type' InputTypes.radio
                    checked' (this.SafeViewSize = viewSize)
                    aria.label txt
                }
            })
    }

    member _.DateFilter(minYear: int) =
        let selector (labelName: string) (paramName: string) (items: int seq) (isSelected: int -> bool) = select {
            hxGetComponent this.Query
            this.AttrForUpdateCurrentPage
            class' "select select-bordered select-sm join-item"
            name paramName
            option {
                value ""
                labelName
            }
            for item in items do
                option {
                    selected (isSelected item)
                    value item
                    item
                }
        }

        let currentYear = DateTime.Now.Year

        fragment {
            selector "Year" (nameof this.Year) [ currentYear .. (-1) .. minYear ] (fun year -> this.Year.HasValue && this.Year.Value = year)
            selector "Month" (nameof this.Month) [ 12 .. (-1) .. 1 ] (fun month -> this.Month.HasValue && this.Month.Value = month)
            selector "Day" (nameof this.Day) [ 31 .. (-1) .. 1 ] (fun day -> this.Day.HasValue && this.Day.Value = day)
        }

    member _.TagFilter() =
        let tagFilterId = $"tag-filter-{Random.Shared.Next()}"
        fragment {
            div {
                class' "dropdown dropdown-end join-item"
                div {
                    hxTrigger' (hxEvt.mouse.click, once = true)
                    hxGetComponent (
                        QueryBuilder<TagFilterList>()
                            .Add((fun x -> x.SelectTagParamName), GlobalQuery.Tags)
                            .Add((fun x -> x.SelectedTags), this.SafeTags)
                    )
                    hxTarget $"#{this.PageId} #{tagFilterId}"
                    class' "btn btn-sm join-item"
                    tabindex 0
                    role "button"

                    Icons.Tag(class' = "h-3 w-3")
                }
                form {
                    id tagFilterId
                    hxGetComponent this.Query
                    this.AttrForUpdateCurrentPage

                    tabindex 0
                    class'
                        "flex flex-row items-center flex-wrap justify-center gap-3 p-3 mt-3 shadow-md shadow-neutral-500/50 menu dropdown-content z-10 bg-base-100 rounded-box w-[280px] sm:w-[350px] border border-primary/30"
                }
            }
        }

    member _.ClearFilter() = button {
        hxTrigger hxEvt.mouse.click
        hxGetComponent typeof<MemoriesPage>
        this.AttrForUpdateCurrentPage

        class' "join-item btn btn-sm"
        Icons.Clear(class' = "h-3 w-3")
    }

    member _.MemoryUploader() = button {
        hxTrigger' hxEvt.mouse.click
        hxModal (QueryBuilder<UploadFilesModal>())
        class' "btn btn-primary btn-ghost relative"
        Icons.Uploads()
        progress { class' "htmx-indicator absolute left-0 top-0 right-0 bottom-0 loading loading-ring w-full h-full p-1" }
    }

    override _.Render() =
        html.inject (fun (appOptions: IOptions<AppOptions>, memoryDb: MemoryDbContext, mediator: IMediator, cssRules: IScopedCssRules) -> task {
            let! minYear = task {
                try
                    return! memoryDb.Memories.AsNoTracking().Select(fun x -> x.Year).MinAsync()
                with _ ->
                    return DateTime.Now.Year
            }

            let! maxCount =
                match this.SafeViewSize with
                | ThumbnailSize.ExtraSmallByDay ->
                    ``Get memory max count by day with filter`` (
                        Year = Option.ofNullable this.Year,
                        Month = Option.ofNullable this.Month,
                        Tags = this.SafeTags.Value
                    )
                    |> mediator.Send
                | ThumbnailSize.ExtraSmallByMonth ->
                    ``Get memory max count by month with filter`` (
                        Year = Option.ofNullable this.Year,
                        Month = Option.ofNullable this.Month,
                        Tags = this.SafeTags.Value
                    )
                    |> mediator.Send
                | _ -> Task.retn 1

            return main {
                id this.PageId
                PageTitle'() { appOptions.Value.Title }
                section {
                    class' "flex flex-col items-center justify-center sticky top-1 gap-1 z-50"
                    div {
                        class' "hidden md:block join border border-neutral-500 mt-2"
                        this.SizeFilter()
                        this.DateFilter(minYear)
                        this.TagFilter()
                        this.ClearFilter()
                    }
                    div {
                        class' "md:hidden join border border-neutral-500 mt-2"
                        this.SizeFilter()
                    }
                    div {
                        class' "md:hidden join border border-neutral-500"
                        this.DateFilter(minYear)
                        this.TagFilter()
                        this.ClearFilter()
                    }
                    progress {
                        id this.PageIndicatorId
                        class' "htmx-indicator loading loading-infinity loading-lg text-info"
                    }
                }
                section {
                    class' " mt-1 flex flex-col items-center justify-center"
                    this.MemoryUploader()
                }
                if not (this.Year.HasValue || this.Month.HasValue || this.Day.HasValue || this.SafeTags.Value.Length > 0) then
                    section {
                        class' "mt-2 flex items-center flex-col justify-center gap-2 mx-2 lg:mx-auto lg:w-[1024px]"
                        h2 {
                            class' "text-2xl text-primary font-bold"
                            "History of Today"
                        }
                        html.blazor (
                            ComponentAttrBuilder<HistoryOfToday>()
                                .Add((fun x -> x.Year), this.Year)
                                .Add((fun x -> x.Month), this.Month)
                                .Add((fun x -> x.Day), this.Day)
                                .Add((fun x -> x.Tags), this.SafeTags)
                                .Add(
                                    (fun x -> x.ViewSize),
                                    match this.SafeViewSize with
                                    | ThumbnailSize.ExtraSmallByDay
                                    | ThumbnailSize.ExtraSmallByMonth -> ThumbnailSize.Medium
                                    | _ -> this.SafeViewSize
                                )
                        )
                    }
                section {
                    class' "mx-2 lg:mx-auto lg:w-[1024px]"
                    html.blazor (
                        ComponentAttrBuilder<Memories>()
                            .Add((fun x -> x.AlwaysShowYear), true)
                            .Add((fun x -> x.ViewSize), this.SafeViewSize)
                            .Add((fun x -> x.MaxCount), maxCount)
                            .Add(
                                (fun x -> x.Year),
                                (if this.Year.HasValue && this.Year.Value > 0 then
                                     this.Year.Value
                                 else
                                     DateTime.Now.Year)
                            )
                            .Add(
                                (fun x -> x.Month),
                                (if this.Month.HasValue && this.Month.Value > 0 then this.Month.Value
                                 else if this.Year.HasValue && this.Year.Value <> DateTime.Now.Year then 12
                                 else DateTime.Now.Month)
                            )
                            .Add((fun x -> x.Day), this.Day)
                            .Add((fun x -> x.Tags), this.SafeTags)
                            .Add((fun x -> x.ShowRestOfYears), not this.Year.HasValue || this.Year.Value <= 0)
                            .Add((fun x -> x.ShowRestOfMonthes), not this.Month.HasValue || this.Month.Value <= 0)
                    )
                }
                div {
                    class' "fixed bottom-4 right-4 z-20 flex flex-col gap-2"
                    style { cssRules.FadeInUpCss() }
                    html.blazor<BatchTagsIndicatorBtn> ()
                    ScrollToTop.Btn()
                }

                // Scripts
                BatchTagsIndicatorBtn.Scripts()
                script { NativeJs.UpdateQueries(this.Query) }

                // Modal
                if this.MemoryId.HasValue then
                    div {
                        hxTrigger hxEvt.load
                        hxModal (QueryBuilder<MemoryDetailModal>().Add((fun x -> x.Id), this.MemoryId.Value))
                        hxAddQueriesToHtmxParams
                        hxIndicator $"#{this.PageIndicatorId}"
                    }
            }
        })
