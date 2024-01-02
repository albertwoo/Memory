namespace Memory.Views.Components

open System
open System.Linq
open Microsoft.AspNetCore.Components
open Microsoft.EntityFrameworkCore
open Fun.Blazor
open Memory.Db
open Memory.Domain


type HistoryOfToday() as this =
    inherit FunComponent()

    [<Parameter>]
    member val Year = Nullable<int>() with get, set

    [<Parameter>]
    member val Month = Nullable<int>() with get, set

    [<Parameter>]
    member val Day = Nullable<int>() with get, set

    [<Parameter>]
    member val ViewSize = ThumbnailSize.Medium with get, set

    [<Parameter>]
    member val Tags = Parsable List.empty<string> with get, set


    override _.Render() =
        html.inject (fun (memoryDb: MemoryDbContext) -> task {
            let currentYear = this.Year |> Option.ofNullable |> Option.defaultValue DateTime.Now.Year
            let currentMonth = this.Month |> Option.ofNullable |> Option.defaultValue DateTime.Now.Month
            let currentDay = this.Day |> Option.ofNullable |> Option.defaultValue DateTime.Now.Day

            let! hasMemory =
                memoryDb.Memories
                    .AsNoTracking()
                    .AnyAsync(fun x -> x.Year <> currentYear && x.Month = currentMonth && x.Day = currentDay)

            if hasMemory then
                let! minYear = task {
                    try
                        return! memoryDb.Memories.AsNoTracking().Select(fun x -> x.Year).MinAsync()
                    with _ ->
                        return DateTime.Now.Year
                }

                return div {
                    class' "flex flex-wrap gap-2 items-center justify-center"
                    if currentYear > minYear then
                        for year in DateTime.Now.Year .. (-1) .. minYear do
                            if year <> currentYear then
                                html.blazor (
                                    ComponentAttrBuilder<Thumbnails>()
                                        .Add((fun x -> x.Year), year)
                                        .Add((fun x -> x.Month), currentMonth)
                                        .Add((fun x -> x.Day), currentDay)
                                        .Add((fun x -> x.Tags), this.Tags)
                                        .Add((fun x -> x.ViewSize), this.ViewSize)
                                        .Add((fun x -> x.ForHisotryOfToday), true)
                                )
                }

            else
                return div {
                    class' "text-lg text-neutral-500/70 text-center"
                    "none for today"
                }
        })
