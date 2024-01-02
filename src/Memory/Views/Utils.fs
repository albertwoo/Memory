namespace Memory.Views

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
