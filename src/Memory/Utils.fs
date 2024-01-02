[<AutoOpen>]
module Utils

open System.IO


// Combine two parts to path
let inline (</>) x y = Path.Combine(x, y)


[<RequireQualifiedAccess>]
module Option =
    let inline ofTryResult (x: bool * _) =
        match x with
        | true, x -> Some x
        | _ -> None
