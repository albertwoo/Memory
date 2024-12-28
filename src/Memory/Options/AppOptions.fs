namespace Memory.Options

open System
open System.IO
open Microsoft.Extensions.Hosting
open System.ComponentModel.DataAnnotations

type AppUser = { Name: string; Password: string }

[<CLIMutable>]
type AppOptions =
    {
        [<Required; MinLength(1)>]
        Version: string

        DisableAuth: bool

        [<Required; MinLength(1)>]
        Theme: string

        [<Required; MinLength(1)>]
        Title: string
        [<Required>]
        SubTitleLine1_1: string
        SubTitleLine1_2: string
        SubTitleLine1_3: string
        [<Required>]
        SubTitleLine2_1: string

        [<Required; MinLength(1)>]
        Users: AppUser[]

        [<Required; MinLength(1)>]
        SourceFolders: string[]
        [<Required; MinLength(1)>]
        CacheFolder: string
        [<Required; MinLength(1)>]
        UploadFolder: string
        // The file name will be used as the tag for the face, and every file should contains one face which will be used to tag memory
        [<Required; MinLength(1)>]
        FamiliarFacesFolder: string
        [<Required; MinLength(1)>]
        FFmpegBinFolder: string

        // This will used to check the source folders file's path, if the segment is in the exclude array, we should ignore that file
        ExcludePathSegments: string[]
    }

    static let ensureFolder dir =
        if Directory.Exists dir |> not then Directory.CreateDirectory dir |> ignore
        dir

    static let ensureRootFolder (env: IHostEnvironment) (dir: string) =
        if String.IsNullOrEmpty dir then failwith "Folder should not be null or empty"

        if Path.IsPathRooted(dir) then dir else env.ContentRootPath </> dir
        |> ensureFolder


    member this.GetSourceFolders() = if this.SourceFolders = null then [] else Seq.toList this.SourceFolders

    member this.GetExcludePathSegments() =
        if this.ExcludePathSegments = null then
            []
        else
            Seq.toList this.ExcludePathSegments

    member this.GetRootedCacheFolder(env: IHostEnvironment) = ensureRootFolder env this.CacheFolder

    member this.GetRootedUploadFolder(env: IHostEnvironment) = ensureRootFolder env this.UploadFolder

    member this.GetRootedFFmpegBinFolder(env: IHostEnvironment) = ensureRootFolder env this.FFmpegBinFolder

    member this.GetFamiliarFacesFolder(env: IHostEnvironment) = ensureRootFolder env this.FamiliarFacesFolder

    member this.GetOptimizedFolder(env: IHostEnvironment) = this.GetRootedCacheFolder env </> "Optimized" |> ensureFolder

    member this.AppendWithVersion(x: string) =
        if x.Contains "?" then
            x + "&version=" + this.Version
        else
            x + "?version=" + this.Version


    static member OptimizedUrlPrefix = "/memory/optimized"

    static member OptimizedImageFormat = "webp"
    static member CreateOptimizedNameForImage(id: int64, ?size: int) =
        match size with
        | Some size -> $"{id}-{size}x{size}.{AppOptions.OptimizedImageFormat}"
        | None -> $"{id}.{AppOptions.OptimizedImageFormat}"
    static member CreateOptimizedUrlForImage(id: int64, ?size: int) =
        match size with
        | Some size -> $"{AppOptions.OptimizedUrlPrefix}/{AppOptions.CreateOptimizedNameForImage(id, size)}"
        | None -> $"{AppOptions.OptimizedUrlPrefix}/{AppOptions.CreateOptimizedNameForImage(id)}"

    static member OptimizedVideoFormat = "mp4"
    static member CreateOptimizedNameForVideo(id: int64) = $"{id}.{AppOptions.OptimizedVideoFormat}"
    static member CreateOptimizedUrlForVideo(id: int64) = $"{AppOptions.OptimizedUrlPrefix}/{AppOptions.CreateOptimizedNameForVideo id}"
