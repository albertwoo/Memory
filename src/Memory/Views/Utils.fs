namespace Memory.Views

open Fun.Blazor
open Memory.Domain

[<RequireQualifiedAccess>]
module GlobalQuery =
    [<Literal>]
    let ViewSize = "ViewSize"

    [<Literal>]
    let MemoryId = "MemoryId"

    [<Literal>]
    let Year = "Year"

    [<Literal>]
    let Month = "Month"

    [<Literal>]
    let Day = "Day"

    [<Literal>]
    let Tags = "Tags"

type GlobalQuery =
    static member CreateForViewSize(size: ThumbnailSize) = GlobalQuery.MemoryId, box (string size)
    static member CreateForMemoryId(id: int64) = GlobalQuery.MemoryId, box id
    static member CreateForYear(x: int) = GlobalQuery.Year, box x
    static member CreateForMonth(x: int) = GlobalQuery.Month, box x
    static member CreateForDay(x: int) = GlobalQuery.Day, box x
    static member CreateForTags(x: string) = GlobalQuery.Tags, box x


type ComponentResponseCacheFor1HourAttribute() =
    inherit ComponentResponseCacheAttribute(CacheControl = "public, max-age=3600")

type ComponentResponseCacheFor1DayAttribute() =
    inherit ComponentResponseCacheAttribute(CacheControl = "public, max-age=86400")

type ComponentResponseCacheFor7DayAttribute() =
    inherit ComponentResponseCacheAttribute(CacheControl = "public, max-age=604800")

type ComponentResponseCacheFor1MonthAttribute() =
    inherit ComponentResponseCacheAttribute(CacheControl = "public, max-age=2592000")
