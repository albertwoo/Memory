namespace Memory.Domain


[<RequireQualifiedAccess; Struct>]
type ThumbnailSize =
    | ExtraSmallByDay
    | ExtraSmallByMonth
    | Small
    | Medium
    | Large

    member this.ToPixel() =
        match this with
        | ExtraSmallByDay
        | ExtraSmallByMonth -> 20
        | Small -> 32
        | Medium -> 80
        | Large -> 128

    static member Parse(x: string) =
        match x with
        | nameof ExtraSmallByDay -> ExtraSmallByDay
        | nameof ExtraSmallByMonth -> ExtraSmallByMonth
        | nameof Small -> Small
        | nameof Medium -> Medium
        | nameof Large -> Large
        | _ -> failwith $"Size {x} is not supported"
