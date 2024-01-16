namespace Memory.Views.Components

open System
open System.IO
open System.Security.Claims
open Microsoft.Extensions.Options
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open Microsoft.Net.Http.Headers
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Forms
open Microsoft.AspNetCore.WebUtilities
open Fun.Htmx
open Fun.Blazor
open Memory.Options
open Memory.Views

type UploadFiles() as this =
    inherit FunComponent()

    [<Inject>]
    member val HttpContextAccessor: IHttpContextAccessor = Unchecked.defaultof<_> with get, set

    [<Inject>]
    member val Env: IHostEnvironment = Unchecked.defaultof<_> with get, set

    [<Inject>]
    member val Logger: ILogger<UploadFiles> = Unchecked.defaultof<_> with get, set

    [<Inject>]
    member val AppOptions: IOptions<AppOptions> = Unchecked.defaultof<_> with get, set

    member val FormId = "upload-form-" + Random.Shared.Next().ToString()
    member val FileInputId = "files-" + Random.Shared.Next().ToString()


    member _.HttpContext = this.HttpContextAccessor.HttpContext

    member _.IsMultipartRequest =
        let multipartIndex =
            this.HttpContext.Request.ContentType.IndexOf(
                "multipart/",
                StringComparison.OrdinalIgnoreCase
            )

        multipartIndex >= 0

    member _.UploadFiles() = task {
        let userName =
            this.HttpContext.User.Claims |> Seq.find (fun x -> x.Type = ClaimTypes.Name) |> (fun x -> x.Value)

        let tempFolder = this.AppOptions.Value.GetRootedCacheFolder(this.Env) </> "Uploading"
        let userUploadFolder = this.AppOptions.Value.GetRootedUploadFolder(this.Env) </> userName

        if Directory.Exists tempFolder |> not then
            Directory.CreateDirectory tempFolder |> ignore

        if Directory.Exists userUploadFolder |> not then
            Directory.CreateDirectory userUploadFolder |> ignore

        let fails = Collections.Generic.List<string>()
        let fileNameMappings = Collections.Generic.Dictionary<string, string>()
        let otherFormKeyValues = Collections.Generic.Dictionary<string, string>()

        let contentType = this.HttpContext.Request.ContentType
        let boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value
        let reader = MultipartReader(boundary, this.HttpContext.Request.Body)
        let mutable section: MultipartSection = null

        reader.BodyLengthLimit <- Int64.MaxValue
        // So we can read the form from start
        this.HttpContext.Request.Body.Position <- 0

        let readNext() = task {
            try
                let! result = reader.ReadNextSectionAsync()
                section <- result
                return section <> null
            with ex ->
                fails.Add("Process form failed, try to upload one by one for large files.")
                this.Logger.LogInformation(ex, "Read form section failed")
                return false
        }

        while! readNext() do
            let contentDisposition = ContentDispositionHeaderValue.Parse section.ContentDisposition

            if contentDisposition <> null && contentDisposition.FileName.HasValue then
                let originalName = contentDisposition.FileName.ToString()

                try
                    this.Logger.LogInformation("Start upload {file} by {user}", originalName, userName)
                    
                    let newFileName =
                            tempFolder </>
                            Guid.NewGuid().ToString() + Path.GetExtension(originalName)

                    use fileStream = File.Open(newFileName, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                    do! section.Body.CopyToAsync(fileStream)
                    
                    fileNameMappings[originalName] <- newFileName

                    this.Logger.LogInformation("Created file {name}", newFileName)

                with ex ->
                    this.Logger.LogError(ex, "Upload {file} failed", originalName)
                    fails.Add(originalName)

            else
                use streamReader =
                    new StreamReader(
                        section.Body,
                        Text.Encoding.UTF8,
                        detectEncodingFromByteOrderMarks = true,
                        bufferSize = 1024,
                        leaveOpen = true
                    )
                let key = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value
                let! value = streamReader.ReadToEndAsync()
                otherFormKeyValues[key] <- value

        for KeyValue(originalName, newFileName) in fileNameMappings do
            try
                // Frontend js will add last modified time to the form content, we can use it to improve file creation time accuracy
                match otherFormKeyValues.TryGetValue(originalName) with
                | true, value ->
                    let time = DateTimeOffset.FromUnixTimeMilliseconds(Int64.Parse(string value))
                    File.SetCreationTimeUtc(newFileName, time.DateTime)
                    this.Logger.LogInformation("Update file creation time {time}", time)
                | _ -> ()
            with ex ->
                this.Logger.LogWarning(ex, "Update file creation time failed")

            let userFile = userUploadFolder </> Path.GetFileName(newFileName)
            File.Move(newFileName, userFile)
            this.Logger.LogInformation("File upload finished: {file}", originalName)

        if fails.Count = 0 then return Ok() else return Error(fails |> Seq.toList)
    }


    member _.Form(?message: NodeRenderFragment) = form {
        id this.FormId
        hxPostComponent typeof<UploadFiles>
        enctype "multipart/form-data"

        html.blazor<AntiforgeryToken> ()

        div {
            class' "join w-full"
            label {
                class' "join-item form-control w-full"
                input {
                    id this.FileInputId
                    class' "join-item file-input file-input-bordered w-full"
                    type' InputTypes.file
                    multiple true
                    name "files"
                }
            }
            button {
                class' "join-item btn btn-primary"
                type' InputTypes.submit
                "Upload"
            }
        }

        progress { class' "htmx-indicator progress progress-primary" }
        script { NativeJs.AppendFileCreationHiddenFields($"#{this.FormId}", $"#{this.FileInputId}") }
        defaultArg message html.none
    }

    member _.ResultMessageView = 
        function 
        | Ok() -> p {
            class' "text-success text-lg text-center"
            "Uploaded success, please refresh after some time to check the result."
          }
        | Error (es: string list) -> div {
            class' "text-error text-center"
            p {
                class' "text-lg"
                "Uploaded failed"
            }
            ul {
                class' "text-sm"
                for e in es do
                    li { e }
            }
          }


    override _.Render() =
        html.inject (fun () -> task {
            if this.HttpContext.Request.Method.Equals("post", StringComparison.OrdinalIgnoreCase) then
                if this.IsMultipartRequest then
                    let! result = this.UploadFiles()
                    return this.Form(message = this.ResultMessageView result)
                else
                    return
                        this.Form(
                            message = p {
                                class' "text-warning text-lg text-center"
                                "No files are selected"
                            }
                        )
            else
                return this.Form()
        })


type UploadFilesModal() =
    inherit FunComponent()

    override _.Render() =
        Modal.Create(
            title = span { "Upload memories (for large files, please upload one by one)" },
            content = section {
                class' "mt-2"
                html.blazor<UploadFiles> ()
            },
            size = ModalSize.Medium,
            actions = fragment {
                button {
                    class' "link"
                    on.click (NativeJs.ReloadPage())
                    "Refresh"
                }
            }
        )
