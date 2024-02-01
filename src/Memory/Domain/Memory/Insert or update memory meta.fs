namespace Memory.Domain.Memory

open System
open System.IO
open System.Linq
open System.Globalization
open System.Diagnostics
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Options
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing
open SixLabors.ImageSharp.Formats.Jpeg
open SixLabors.ImageSharp.Formats.Webp
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Metadata.Profiles.Exif
open Sdcb.LibRaw
open FFMediaToolkit.Decoding
open MediatR
open Fun.Result
open Memory.Db
open Memory.Domain
open Memory.Options


module private Meta =

    let convertDecimalDegreeToDMS (isoString: string) =
        let parseIso6709 (isoString: string) =
            let pattern = @"([+-]\d+\.\d+)([+-]\d+\.\d+)([+-]\d+\.\d+)?"
            let regex = Text.RegularExpressions.Regex(pattern)

            let matchResult = regex.Match(isoString)

            if matchResult.Success then
                let latitude = double matchResult.Groups.[1].Value
                let longitude = double matchResult.Groups.[2].Value
                Ok(struct (latitude, longitude))
            else
                Error "Invalid ISO 6709 location string"

        let convertToDMS (decimalValue: float) =
            let degrees = int decimalValue
            let minutes = int ((decimalValue - float degrees) * 60.0)
            let seconds = (decimalValue - float degrees - (float minutes / 60.0)) * 3600.0
            [| Rational(degrees); Rational(minutes); Rational(uint32 (seconds * 1000.), uint 1000) |]

        parseIso6709 isoString |> Result.map (fun struct (lat, long) -> convertToDMS lat, convertToDMS long)

    let convertFromDMSToDecimalDegree (value: Rational[]) =
        let degree = value[0].Numerator |> float
        let minutes = value[1].Numerator |> float
        let seconds = float value[2].Numerator / float value[2].Denominator
        Math.Round(degree + minutes / 60.0 + seconds / 3600., 8)


    let createOptimizedWebp (fromFile: string, toFile: string) = task {
        use! image = Image.LoadAsync(imageSharpDecoderOptions.Value, fromFile)
        let hratio = float (Math.Min(image.Height, 1920)) / float image.Height
        let wratio = float (Math.Min(image.Width, 1920)) / float image.Width
        let ratio = Math.Min(hratio, wratio)
        if ratio < 1. then
            image.Mutate(fun ctx -> ctx.Resize(int (float image.Width * ratio), int (float image.Height * ratio)) |> ignore)
        do! image.SaveAsWebpAsync(toFile, WebpEncoder(Quality = 90))
    }


    /// Should convert any video to webm and generate image based on the first frame
    let createOptimizedVideo (ffmpegFile: string) (fromFile: string, toFile: string, toImageFile: string) =
        use mediaFile = MediaFile.Open fromFile
        let mutable height, width = 0, 0

        if mediaFile.HasVideo then
            let imageData = mediaFile.Video.GetNextFrame()
            use image = Image.LoadPixelData<Bgr24>(imageData.Data, imageData.ImageSize.Width, imageData.ImageSize.Height)

            height <- image.Height
            width <- image.Width

            if image.Metadata.ExifProfile = null then
                image.Metadata.ExifProfile <- ExifProfile()

            mediaFile.Info.Metadata.Metadata.TryGetValue "creation_time"
            |> Option.ofTryResult
            |> Option.iter (
                function
                | DATETIME creationTime -> image.Metadata.ExifProfile.SetValue(ExifTag.DateTimeOriginal, creationTime.ToString("yyyy:MM:dd HH:mm:ss"))
                | _ -> ()
            )

            mediaFile.Info.Metadata.Metadata.TryGetValue "location"
            |> Option.ofTryResult
            |> Option.defaultWithOption (fun () ->
                mediaFile.Info.Metadata.Metadata.TryGetValue "com.apple.quicktime.location.ISO6709" |> Option.ofTryResult
            )
            |> Option.bind (convertDecimalDegreeToDMS >> Result.toOption)
            |> Option.iter (fun (lat, long) ->
                image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitude, lat)
                image.Metadata.ExifProfile.SetValue(ExifTag.GPSLongitude, long)
            )

            mediaFile.Info.Metadata.Metadata.TryGetValue "make"
            |> Option.ofTryResult
            |> Option.defaultWithOption (fun () -> mediaFile.Info.Metadata.Metadata.TryGetValue "com.apple.quicktime.make" |> Option.ofTryResult)
            |> Option.iter (fun make -> image.Metadata.ExifProfile.SetValue(ExifTag.Make, make))

            mediaFile.Info.Metadata.Metadata.TryGetValue "model"
            |> Option.ofTryResult
            |> Option.defaultWithOption (fun () -> mediaFile.Info.Metadata.Metadata.TryGetValue "com.apple.quicktime.model" |> Option.ofTryResult)
            |> Option.iter (fun make -> image.Metadata.ExifProfile.SetValue(ExifTag.Model, make))

            image.SaveAsWebp(toImageFile, WebpEncoder(Quality = 90))

        else
            failwith "No video in the file"

        let logLevel =
#if DEBUG
            "info"
#else
            "error"
#endif

        let hratio = float (Math.Min(height, 1920)) / float height
        let wratio = float (Math.Min(width, 1920)) / float width
        let ratio = Math.Min(hratio, wratio)
        if ratio > 1. then
            height <- int (float height * ratio)
            width <- int (float width * ratio)
            
        let makeEven x = if x % 2 = 0 then x else x + 1
        height <- makeEven height
        width <- makeEven width

        let processStartInfo = ProcessStartInfo()
        processStartInfo.FileName <- ffmpegFile
        processStartInfo.Arguments <-
            $"""-i "{fromFile}" -c:v libx264 -c:a aac -b:v 0 -b:a 48k -preset fast -crf 28 -y -vf "scale=w={width}:h={height},fps=20,format=yuv420p" {toFile} -loglevel {logLevel}"""

        use ps = new Process()
        ps.StartInfo <- processStartInfo

        ps.Start() |> ignore

        ps.PriorityClass <- ProcessPriorityClass.BelowNormal

        let cleanup () =
            try
                ps.Kill()
                ps.Close()
            with _ ->
                ()

        task {
            ps.WaitForExit(TimeSpan.FromMinutes 60) |> ignore
            if ps.ExitCode <> 0 then
                cleanup ()
                failwith "FFmpeg process failed"
            cleanup ()
        }


    let createThumbnail (image: Image) (size: int) (fileName: string) =
        use firstFrame = image.Frames.CloneFrame(0)
        firstFrame.Mutate(fun ctx ->
            ctx.Resize(ResizeOptions(Mode = ResizeMode.Crop, Size = Size(width = size, height = size), Compand = true)) |> ignore
        )
        firstFrame.SaveAsWebpAsync(fileName, WebpEncoder(Quality = 90))


    let extractExifMeta (logger: ILogger) (profile: ExifProfile) (meta: MemoryMeta) =
        match profile.TryGetValue(ExifTag.DateTimeOriginal) with
        | true, x ->
            try
                meta.DateTimeOriginal <- DateTime.ParseExact(x.Value, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture) |> Nullable
            with ex ->
                logger.LogError(ex, $"Parse {nameof meta.DateTimeOriginal} failed")
        | _ -> ()

        match profile.TryGetValue(ExifTag.OffsetTimeOriginal) with
        | true, x -> meta.OffsetTimeOriginal <- x.Value
        | _ -> ()

        match profile.TryGetValue(ExifTag.Make) with
        | true, x -> meta.Make <- x.Value
        | _ -> ()

        match profile.TryGetValue(ExifTag.Model) with
        | true, x -> meta.Modal <- x.Value
        | _ -> ()

        match profile.TryGetValue(ExifTag.LensModel) with
        | true, x -> meta.LensModal <- x.Value
        | _ -> ()

        match profile.TryGetValue(ExifTag.GPSLatitude) with
        | true, x ->
            try
                meta.Latitude <- Nullable(convertFromDMSToDecimalDegree x.Value)
            with ex ->
                logger.LogError(ex, $"Parse {nameof meta.Latitude} failed")
        | _ -> ()

        match profile.TryGetValue(ExifTag.GPSLongitude) with
        | true, x ->
            try
                meta.Longitude <- Nullable(convertFromDMSToDecimalDegree x.Value)
            with ex ->
                logger.LogError(ex, $"Parse {nameof meta.Longitude} failed")
        | _ -> ()


type ``Insert or update memory meta handler``
    (appOptions: IOptions<AppOptions>, env: IHostEnvironment, memoryDb: MemoryDbContext, logger: ILogger<``Insert or update memory meta handler``>) =

    interface IRequestHandler<``Insert or update memory meta``, unit> with
        member _.Handle(request, cancellationToken) = task {
            let optimizedFolder = appOptions.Value.GetOptimizedFolder(env)

            let createThumbnail image (size: ThumbnailSize) =
                Meta.createThumbnail image (size.ToPixel()) (optimizedFolder </> AppOptions.CreateOptimizedNameForImage(request.Id, size.ToPixel()))

            let isSmallThumbnailFileExist =
                optimizedFolder </> AppOptions.CreateOptimizedNameForImage(request.Id, ThumbnailSize.Small.ToPixel()) |> File.Exists

            let! meta = memoryDb.MemoryMetas.Where(fun x -> x.MemoryId = request.Id).FirstOrDefaultAsync()

            if not isSmallThumbnailFileExist || meta = null then
                logger.LogInformation("Start optimize memory {id} {file}", request.Id, request.File)

                let webpFile = optimizedFolder </> AppOptions.CreateOptimizedNameForImage(request.Id)

                let! file = task {
                    // Optimized all files to webp
                    match request.File with
                    | Gif -> return request.File

                    // From NEF -> TIFF -> JPG -> WEBP
                    | Nef ->
                        let tiffFile = optimizedFolder </> string request.Id + ".tiff"
                        use ctx = RawContext.OpenFile(request.File)
                        ctx.SaveRawImage(tiffFile)
                        logger.LogInformation("Create tiff {file} for nef", tiffFile)

                        do! Meta.createOptimizedWebp (tiffFile, webpFile)
                        return webpFile

                    | ImageFormat ->
                        do! Meta.createOptimizedWebp (request.File, webpFile)
                        return webpFile

                    | VideoFormat ->
                        do!
                            Meta.createOptimizedVideo
                                (appOptions.Value.FFmpegBinFolder </> "ffmpeg")
                                (request.File, optimizedFolder </> AppOptions.CreateOptimizedNameForVideo(request.Id), webpFile)
                        return webpFile

                    | _ ->
                        failwith "Not supported format for meta process"
                        return ""
                }

                use! image = Image.LoadAsync(file, cancellationToken)

                if image.Metadata.ExifProfile |> isNull |> not then
                    // Update or create new meta
                    let meta =
                        if meta = null then
                            let meta = MemoryMeta()
                            memoryDb.MemoryMetas.Add(meta) |> ignore
                            meta
                        else
                            meta

                    meta.MemoryId <- request.Id
                    Meta.extractExifMeta logger image.Metadata.ExifProfile meta
                    do! memoryDb.SaveChangesAsync() |> Task.map ignore

                    if meta.DateTimeOriginal.HasValue then
                        let dateTime = meta.DateTimeOriginal.Value
                        do!
                            memoryDb.Memories
                                .AsNoTracking()
                                .Where(fun x -> x.Id = request.Id)
                                .ExecuteUpdateAsync(fun memory -> memory.SetProperty((fun p -> p.CreationTime), dateTime))
                            |> Task.map ignore

                else if meta = null then
                    memoryDb.MemoryMetas.Add(MemoryMeta(MemoryId = request.Id)) |> ignore
                    do! memoryDb.SaveChangesAsync() |> Task.map ignore

                do! createThumbnail image ThumbnailSize.Large
                do! createThumbnail image ThumbnailSize.Medium
                do! createThumbnail image ThumbnailSize.Small

                logger.LogInformation("Updated meta and thumbnails for memory {id} {file}", request.Id, request.File)

            return ()
        }
