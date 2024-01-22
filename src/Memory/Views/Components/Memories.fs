namespace Memory.Views.Components

open System
open Microsoft.AspNetCore.Components
open MediatR
open Fun.Htmx
open Fun.Result
open Fun.Blazor
open Memory
open Memory.Domain
open Memory.Views

type Memories() as this =
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
    member val AlwaysShowYear = false with get, set

    [<Parameter>]
    member val ViewSize = ThumbnailSize.Medium with get, set

    [<Parameter>]
    member val MaxCount = 1 with get, set

    [<Parameter>]
    member val ShowRestOfYears = true with get, set

    [<Parameter>]
    member val ShowRestOfMonthes = true with get, set


    member _.MemoryLoader() = html.fragment [|
        span { class' "loading loading-ring loading-xs" }
        span { class' "loading loading-ring loading-sm" }
        span { class' "loading loading-ring loading-md" }
        span { class' "loading loading-ring loading-lg" }
        span { class' "loading loading-ring loading-md" }
        span { class' "loading loading-ring loading-sm" }
        span { class' "loading loading-ring loading-xs" }
    |]

    member _.RestMemories() = section {
        hxTrigger' (hxEvt.intersect, once = true)
        hxGetComponent (
            QueryBuilder<Memories>()
                .Add(this)
                .Add((fun x -> x.AlwaysShowYear), false)
                .Add(
                    (fun x -> x.Year),
                    (if this.ShowRestOfMonthes then
                         if this.Month = 1 then this.Year - 1 else this.Year
                     else
                         this.Year - 1)
                )
                .Add(
                    (fun x -> x.Month),
                    (if this.ShowRestOfMonthes then
                         if this.Month = 1 then 12 else this.Month - 1
                     else
                         this.Month)
                )
        )
        hxSwap_outerHTML
        class' "flex items-center justify-center gap-1 my-4"
        this.MemoryLoader()
    }

    override _.Render() =
        html.inject (fun (mediator: IMediator, cssRules: IScopedCssRules) -> task {
            let! restCount =
                if this.ShowRestOfYears || this.ShowRestOfMonthes then
                    let toTime = DateTime(year = this.Year, month = this.Month, day = 1)
                    Domain.``Count ordered memories by creation time desc with filter`` (toTime = toTime, Tags = this.Tags.Value)
                    |> mediator.Send
                else
                    Task.retn 0

            return html.fragment [|
                if this.Month = 12 || this.AlwaysShowYear || not this.ShowRestOfMonthes then
                    a {
                        target "_blank"
                        href ("?" + QueryBuilder().Add(GlobalQuery.ViewSize, this.ViewSize).Add(GlobalQuery.Year, this.Year).ToString())
                        style { cssRules.FadeInUpCss() }
                        h2 {
                            class' "divider text-2xl font-bold text-center text-primary mt-10"
                            this.Year
                        }
                    }
                match this.ViewSize with
                | ThumbnailSize.ExtraSmallByDay -> section {
                    class' "thumbnails flex flex-wrap gap-1 items-center justify-end md:justify-center mb-1"
                    style { cssRules.FadeInUpCss() }
                    html.blazor (
                        ComponentAttrBuilder<ThumbnailsByDay>()
                            .Add((fun x -> x.Year), this.Year)
                            .Add((fun x -> x.Month), this.Month)
                            .Add((fun x -> x.Tags), this.Tags)
                            .Add((fun x -> x.ViewSize), this.ViewSize)
                            .Add((fun x -> x.MaxCountForDay), this.MaxCount)
                    )
                  }
                | ThumbnailSize.ExtraSmallByMonth ->
                    html.blazor (
                        ComponentAttrBuilder<ThumbnailsByMonth>()
                            .Add((fun x -> x.Year), this.Year)
                            .Add((fun x -> x.Month), this.Month)
                            .Add((fun x -> x.Tags), this.Tags)
                            .Add((fun x -> x.ViewSize), this.ViewSize)
                            .Add((fun x -> x.MaxCountForMonth), this.MaxCount)
                    )
                | _ ->
                    a {
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
                                .Add(GlobalQuery.Day, this.Day)
                                .Add(GlobalQuery.Tags, this.Tags)
                                .ToString()
                        )
                        class' "relative"
                        style { cssRules.FadeInUpCss() }
                        h3 {
                            class' "font-semibold text-center text-primary/80 my-2 text-xl "
                            $"{this.Month}" + (if this.Day.HasValue then $" / {this.Day}" else "")
                        }
                    }
                    section {
                        class' "thumbnails flex flex-wrap gap-2 items-center justify-center"
                        style { cssRules.FadeInUpCss() }
                        html.blazor (
                            ComponentAttrBuilder<Thumbnails>()
                                .Add((fun x -> x.Year), this.Year)
                                .Add((fun x -> x.Month), this.Month)
                                .Add((fun x -> x.Day), this.Day)
                                .Add((fun x -> x.Tags), this.Tags)
                                .Add((fun x -> x.ViewSize), this.ViewSize)
                        )
                    }

                if this.Month = 1 then
                    div {
                        class' "divider opacity-75"
                        $"THE END OF {this.Year}"
                    }
                if (this.ShowRestOfYears && restCount > 0) || (this.ShowRestOfMonthes && this.Month > 1) then
                    this.RestMemories()
            |]
        })
