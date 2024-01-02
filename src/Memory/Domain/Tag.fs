namespace Memory.Domain

open System
open MediatR


type TagUtils =

    static member Spliter = ","

    static member ToString(tags: string seq) = tags |> Seq.map _.ToLower() |> String.concat TagUtils.Spliter
    
    static member FromString(x: string) =
        if String.IsNullOrEmpty x then
            []
        else
            x.Split TagUtils.Spliter |> Seq.filter (String.IsNullOrEmpty >> not) |> Seq.map _.ToLower() |> Seq.toList


type ``Add tag to memeory``(memoryIds: int64 seq, tags: string seq) =
    interface IRequest<unit>
    member _.MemoryIds = memoryIds
    member _.Tags = tags


type ``Delete tag from memeory``(memoryId: int64, tag: string) =
    interface IRequest<unit>
    member _.MemoryId = memoryId
    member _.Tag = tag
