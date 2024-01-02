namespace Memory.Domain

open System
open MediatR


type ``Insert or update memory``(file: string) =
    interface IRequest<int64>
    member _.File = file


type ``Insert or update memory meta``(id: int64, file: string) =
    interface IRequest<unit>
    member _.Id = id
    member _.File = file


type ``Increase views for memory``(id: int64) =
    interface IRequest<unit>
    member _.Id = id


type ``Count ordered memories by creation time desc with filter``(?fromTime: DateTime, ?toTime: DateTime) =
    interface IRequest<int64>
    member val FromTime = fromTime with get, set
    member val ToTime = toTime with get, set
    member val Day = Option<int>.None with get, set
    member val Tags: string list = []  with get, set


type MemoryIdWithExtension =
    {
        Id: int64
        Extension: string
    }

type ``Get memory ids with filter and ordered by creation time desc``(?fromTime: DateTime, ?toTime: DateTime)
    =
    interface IRequest<MemoryIdWithExtension seq>
    member val FromTime = fromTime with get, set
    member val ToTime = toTime with get, set
    member val Tags: string list = []  with get, set
    member val Offset = 0L with get, set
    member val PageSize = 10 with get, set


[<CLIMutable>]
type MemoryCountInDays = { Day: int; Count: int }

type ``Get memory counts by month with filter and ordered by day desc``(year: int, month: int)
    =
    interface IRequest<MemoryCountInDays seq>
    member val Year = year with get, set
    member val Month = month with get, set
    member val Tags: string list = []  with get, set


type ``Get memory max count by day with filter``(?year: int, ?month: int)
    =
    interface IRequest<int>
    member val Year = year with get, set
    member val Month = month with get, set
    member val Tags: string list = [] with get, set

type ``Get memory max count by month with filter``(?year: int, ?month: int)
    =
    interface IRequest<int>
    member val Year = year with get, set
    member val Month = month with get, set
    member val Tags: string list = []  with get, set


type ``Get previous available id``(currentId: int64) =
    interface IRequest<int64 option>
    member _.CurrentId = currentId
    member val Year = Option<int>.None with get, set
    member val ExcludeYear = false with get, set
    member val Month = Option<int>.None with get, set
    member val Day = Option<int>.None with get, set
    member val Tags: string list = []  with get, set
    member val ForHistoryOfToday = false with get, set


type ``Get next available id``(currentId: int64) =
    interface IRequest<int64 option>
    member _.CurrentId = currentId
    member val Year = Option<int>.None with get, set
    member val ExcludeYear = false with get, set
    member val Month = Option<int>.None with get, set
    member val Day = Option<int>.None with get, set
    member val Tags: string list = []  with get, set
