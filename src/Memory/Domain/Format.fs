namespace Memory.Domain

open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats
open HeyRed.ImageSharp.Heif.Formats
open Fun.Result


[<AutoOpen>]
module Format =
    let imageSharpDecoderOptions =
        lazy
            (DecoderOptions(
                MaxFrames = 1u,
                Configuration =
                    Configuration(
                        Avif.AvifConfigurationModule(),
                        Heif.HeifConfigurationModule(),
                        Jpeg.JpegConfigurationModule(),
                        Png.PngConfigurationModule(),
                        Gif.GifConfigurationModule(),
                        Bmp.BmpConfigurationModule(),
                        Tiff.TiffConfigurationModule(),
                        Pbm.PbmConfigurationModule(),
                        Qoi.QoiConfigurationModule(),
                        Tga.TgaConfigurationModule(),
                        Webp.WebpConfigurationModule()
                    )
            ))

    let (|VideoFormat|_|) (x: string) =
        match x with
        | SafeStringExtension(_, ".avi")
        | SafeStringExtension(_, ".webm")
        | SafeStringExtension(_, ".wmv")
        | SafeStringExtension(_, ".mkv")
        | SafeStringExtension(_, ".mov")
        | SafeStringExtension(_, ".mp4") -> Some()
        | _ -> None

    let (|Heic|_|) (x: string) =
        match x with
        | SafeStringExtension(_, ".heic") -> Some()
        | _ -> None

    let (|Nef|_|) (x: string) =
        match x with
        | SafeStringExtension(_, ".nef") -> Some()
        | _ -> None

    let (|Tiff|_|) (x: string) =
        match x with
        | SafeStringExtension(_, ".tif")
        | SafeStringExtension(_, ".tiff") -> Some()
        | _ -> None

    let (|Gif|_|) (x: string) =
        match x with
        | SafeStringExtension(_, ".gif") -> Some()
        | _ -> None

    let (|Png|_|) (x: string) =
        match x with
        | SafeStringExtension(_, ".png") -> Some()
        | _ -> None

    let (|ImageFormat|_|) (x: string) =
        match x with
        | SafeStringExtension(_, ".jpg")
        | SafeStringExtension(_, ".jpeg")
        | SafeStringExtension(_, ".bmp")
        | SafeStringExtension(_, ".qoi")
        | SafeStringExtension(_, ".webp")
        | SafeStringExtension(_, ".pbm")
        | SafeStringExtension(_, ".tga")
        | Nef
        | Heic
        | Tiff
        | Gif
        | Png -> Some()
        | _ -> None

    // For those file's formats which cannot be displayed on browser very well and efficient, 
    // We should transform original file to some supported formats.
    // And put the transformed files into the optimized folder.
    let transformedFormat =
        function
        | Gif -> None
        | ImageFormat -> Some ".webp"
        | VideoFormat -> Some ".mp4"
        | _ -> None
