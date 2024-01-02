namespace Memory.Services

open System
open System.IO
open System.Linq
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open System.Threading
open System.Threading.Tasks
open System.Security.Cryptography
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Microsoft.Extensions.DependencyInjection
open Microsoft.EntityFrameworkCore
open MediatR
open SixLabors.ImageSharp
open ViewFaceCore
open ViewFaceCore.Core
open Fun.Result
open Memory.Db
open Memory.Options
open Memory.Domain

type private ExecuteState =
    | Running
    | Stopping
    | Stopped

[<CLIMutable>]
type FaceTagCache = { LastProcessedMemoryId: int64; Hash: string }

type FaceTagBackgroundService
    (appOptions: IOptions<AppOptions>, sp: IServiceProvider, env: IHostEnvironment, logger: ILogger<FaceTagBackgroundService>) as this =
    inherit BackgroundService()

    let optimizedFolder = appOptions.Value.GetOptimizedFolder(env)
    let familiarFacesFolder = appOptions.Value.GetFamiliarFacesFolder(env)
    let familiarFacesWatcher = new FileSystemWatcher(familiarFacesFolder)

    let faceTagCacheFile = appOptions.Value.GetRootedCacheFolder(env) </> "face-tag"

    let restartTagTaskLocker = new SemaphoreSlim(1)

    let mutable faceDetector = new FaceDetector()
    let mutable faceMark = new FaceLandmarker()
    let mutable faceRecognizer = new FaceRecognizer()

    let mutable executeState = Stopped
    let mutable familiarFaces = []
    let mutable lastProcessedMemoryId = 0L
    let mutable isFamiliarFacesChanged = false
    let mutable lastTimeForRestartTagTask = DateTime.MinValue

    let extractDuplicateTag (input: string) =
        let pattern = @"(\w+)_\d+[.\w+]*"

        match Regex.Match(input, pattern) with
        | matchResult when matchResult.Success ->
            let group = matchResult.Groups.[1]
            if group.Success then Some group.Value else None
        | _ -> None

    member private _.ComputeHashForFamiliarFolders() =
        use md5 = MD5.Create()
        let stringBuilder = StringBuilder()
        for file in Directory.GetFiles(familiarFacesFolder) do
            stringBuilder.Append(file).Append(FileInfo(file).CreationTime.ToString()) |> ignore
        let hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()))
        BitConverter.ToString(hashBytes).Replace("-", "").ToLower()


    member private _.SaveCache() =
        File.WriteAllText(
            faceTagCacheFile,
            JsonSerializer.Serialize {
                FaceTagCache.LastProcessedMemoryId = lastProcessedMemoryId
                Hash = this.ComputeHashForFamiliarFolders()
            }
        )

    member private _.GetCache() =
        try
            JsonSerializer.Deserialize<FaceTagCache>(File.ReadAllText(faceTagCacheFile))
        with ex ->
            logger.LogError(ex, "Read cache failed")
            { FaceTagCache.LastProcessedMemoryId = 0L; Hash = "" }


    member private _.EnsureFaceModels() =
        if faceDetector = null then faceDetector <- new FaceDetector()
        if faceMark = null then faceMark <- new FaceLandmarker()
        if faceRecognizer = null then faceRecognizer <- new FaceRecognizer()

    member private _.CleanFaceModels() =
        if faceDetector <> null then
            faceDetector.Dispose()
            faceDetector <- null
        if faceMark <> null then
            faceMark.Dispose()
            faceMark <- null
        if faceRecognizer <> null then
            faceRecognizer.Dispose()
            faceRecognizer <- null
        logger.LogInformation("Face detection models are released for saving memory")


    member private _.PrepareFamiliarFaces() =
        this.EnsureFaceModels()

        familiarFaces <- [
            for file in Directory.GetFiles familiarFacesFolder do
                try
                    logger.LogInformation("Prepare tag for familiar face from {file}", file)
                    use fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)
                    use image = Image.Load(imageSharpDecoderOptions.Value, fileStream)
                    use faceImage = image.ToFaceImage()

                    let faces = faceDetector.Detect(faceImage)
                    if faces.Length > 1 || faces.Length = 0 then
                        logger.LogError("{file} should only contains one face for tag other memory", file)
                    else
                        let points = faceMark.Mark(faceImage, faces[0])
                        let face = faceRecognizer.Extract(faceImage, points)
                        let fileName = Path.GetFileNameWithoutExtension file
                        let tag = extractDuplicateTag fileName |> Option.defaultValue fileName
                        logger.LogInformation("Familiar face for tag {tag} is ready", tag)
                        tag, face

                with ex ->
                    logger.LogError(ex, "Prepare familiar face tag for {file} failed", file)
        ]


    member private _.RestartTagTask() = task {
        logger.LogInformation("Folder {folder} changed, will restart for face tagging", familiarFacesFolder)

        // Some times, the file system watcher will raise multiple signals int very short time
        if DateTime.Now - lastTimeForRestartTagTask < TimeSpan.FromMilliseconds 500 then
            logger.LogInformation("Duplicate restart will be ignored")

        else
            do! restartTagTaskLocker.WaitAsync()
            lastTimeForRestartTagTask <- DateTime.Now
            executeState <- Stopping

            while executeState <> Stopped do
                do! Task.Delay 10

            isFamiliarFacesChanged <- true
            lastProcessedMemoryId <- 0L
            this.PrepareFamiliarFaces()

            executeState <- Running
            restartTagTaskLocker.Release() |> ignore

            logger.LogInformation("Restart for face tagging success")
    }


    member private _.TagNextFace() = task {
        logger.LogInformation("Try tag next memory. Last processed memory {id}", lastProcessedMemoryId)

        use scope = sp.CreateScope()
        let memoryDb = scope.ServiceProvider.GetRequiredService<MemoryDbContext>()

        let! memory = memoryDb.Memories.Where(fun x -> x.Id > lastProcessedMemoryId).OrderBy(fun x -> x.Id).FirstOrDefaultAsync()

        if memory <> null && (isFamiliarFacesChanged || not memory.IsTaggedByFace) then
            let mutable retry = 0
            let mutable isFinished = false

            while retry < 10 && not isFinished do
                retry <- retry + 1

                let filePath =
                    match transformedFormat memory.FileExtension with
                    | Some _ -> optimizedFolder </> AppOptions.CreateOptimizedNameForImage(memory.Id)
                    | _ -> memory.FilePath

                logger.LogDebug("Try to tag for memory {id} {file}, original {file}", memory.Id, filePath, memory.FilePath)

                try
                    this.EnsureFaceModels()
                    
                    let mediator = scope.ServiceProvider.GetRequiredService<IMediator>()
                    use fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                    use! image = Image.LoadAsync(imageSharpDecoderOptions.Value, fileStream)
                    use faceImage = image.ToFaceImage()
                    let faces = faceDetector.Detect(faceImage)

                    if faces.Length = 0 then
                        logger.LogInformation("There is no faces found for memory {id} {file}", memory.Id, filePath)

                    else
                        for face in faces do
                            let points = faceMark.Mark(faceImage, face)
                            let face = faceRecognizer.Extract(faceImage, points)

                            let tags =
                                familiarFaces
                                |> Seq.choose (fun (tag, taggedFace) -> if faceRecognizer.IsSelf(taggedFace, face) then Some tag else None)

                            for tag in tags do
                                do! Memory.Domain.``Add tag to memeory`` ([ memory.Id ], [ tag ]) |> mediator.Send

                            if tags.Any() |> not then
                                logger.LogInformation("There is no familiar face found for memory {id} {file}", memory.Id, filePath)

                    isFinished <- true
                    memory.IsTaggedByFace <- true
                    do! memoryDb.SaveChangesAsync() |> Task.map ignore

                with
                | :? IOException as ex ->
                    logger.LogError(ex, "Tag memory by face failed for memory {id} {file}", memory.Id, filePath)
                    do! Task.Delay(20_000)

                | ex ->
                    logger.LogError(ex, "Tag memory by face failed for memory {id} {file}", memory.Id, filePath)
                    do! Task.Delay(10_000)

        if memory <> null then lastProcessedMemoryId <- memory.Id

        return memory
    }


    override _.ExecuteAsync(cancellationToken) = task {
        logger.LogInformation($"Start {nameof FaceTagBackgroundService}")

        this.PrepareFamiliarFaces()

        familiarFacesWatcher.IncludeSubdirectories <- true
        familiarFacesWatcher.Created.Add(ignore >> this.RestartTagTask >> ignore)
        familiarFacesWatcher.Changed.Add(ignore >> this.RestartTagTask >> ignore)
        familiarFacesWatcher.Renamed.Add(ignore >> this.RestartTagTask >> ignore)
        familiarFacesWatcher.Deleted.Add(ignore >> this.RestartTagTask >> ignore)
        familiarFacesWatcher.EnableRaisingEvents <- true

        let cache = this.GetCache()
        let currentHash = this.ComputeHashForFamiliarFolders()
        isFamiliarFacesChanged <- currentHash <> cache.Hash
        lastProcessedMemoryId <- if isFamiliarFacesChanged then 0L else cache.LastProcessedMemoryId

        executeState <- Running

        let mutable cacheSavedTime = DateTime.Now
        let mutable noMoreMemoriesToTagCount = 0

        while not cancellationToken.IsCancellationRequested do
            if executeState = Running then
                // With this we can avoid tag face which is already been process when we restart our service
                if DateTime.Now - cacheSavedTime > TimeSpan.FromMinutes 15 then
                    this.SaveCache()
                    cacheSavedTime <- DateTime.Now

                if familiarFaces.IsEmpty then
                    logger.LogWarning("No familiar faces provided in {folder}", familiarFacesFolder)
                    do! Task.Delay 30_000

                else
                    let! memory = this.TagNextFace()
                    if memory <> null then
                        noMoreMemoriesToTagCount <- 0
                        do! Task.Delay 100
                    else
                        logger.LogWarning("No more memories to tag by face. Last processed memory {id}", lastProcessedMemoryId)
                        // When running to the end of the memory table we should set this to false
                        // So it can only be toggled by folder watcher at runtime
                        isFamiliarFacesChanged <- false
                        noMoreMemoriesToTagCount <- noMoreMemoriesToTagCount + 1

                        if noMoreMemoriesToTagCount = 6 * 5 then this.CleanFaceModels()

                        do! Task.Delay 30_000

            if executeState = Stopping then executeState <- Stopped

            if executeState = Stopped then do! Task.Delay 10
    }


    override _.Dispose() =
        this.SaveCache()

        restartTagTaskLocker.Dispose()
        familiarFacesWatcher.Dispose()
