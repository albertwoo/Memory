namespace Memory.Services

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Threading.Channels
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Microsoft.Extensions.DependencyInjection
open MediatR
open Fun.Result
open Memory
open Memory.Options
open Memory.Domain

type MemoryBackgroundService
    (appOptions: IOptions<AppOptions>, sp: IServiceProvider, env: IHostEnvironment, logger: ILogger<MemoryBackgroundService>) as this
    =
    inherit BackgroundService()

    let folders = [
        yield! appOptions.Value.GetSourceFolders()
        appOptions.Value.GetRootedUploadFolder(env)
    ]      

    let excludePathSegments = appOptions.Value.GetExcludePathSegments()

    let watchers = Collections.Generic.List<FileSystemWatcher>()
    let newFilesChannel = Channel.CreateUnbounded<string>()

    let processFile (file: string) = async {
        logger.LogInformation("Process {file} for memory", file)

        let shouldIgnore = excludePathSegments |> Seq.exists (fun x -> file.Contains(x, StringComparison.OrdinalIgnoreCase))

        if shouldIgnore then
            logger.LogInformation("Ignored file {file} because of configured ExcludePathSegments", file)
        
        else
            match file with
            | VideoFormat
            | ImageFormat ->
                try
                    use scope = sp.CreateScope()
                    let mediator = scope.ServiceProvider.GetRequiredService<IMediator>()
                
                    let! id = Domain.``Insert or update memory`` (file = file) |> mediator.Send |> Async.AwaitTask
                    do! Domain.``Insert or update memory meta`` (id, file) |> mediator.Send |> Async.AwaitTask

                with ex ->
                    logger.LogError(ex, "Process {file} failed for sync media", file)

            | _ -> logger.LogWarning("Format is not support for {file}", file)
    }

    let isFileReadyToRead file =
        try
            use _ = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)
            true
        with _ ->
            false

    member _.SyncMedias(cancellationToken: CancellationToken) = task {
        let largeFileLimit = 1024 * 1024 * 30
        let largeFiles = Collections.Generic.List<string>()

        let batch para (files: string seq) =
            let tasks = files |> Seq.map processFile

            Async.Parallel(tasks, maxDegreeOfParallelism = Math.Min(para, Environment.ProcessorCount))
            |> Async.map ignore
            |> fun x -> Async.StartAsTask(x, cancellationToken = cancellationToken)

        for folder in folders do
            logger.LogInformation("Start process {folder} for sync media", folder)
            do!
                Directory.GetFiles(
                    folder,
                    searchPattern = "*.*",
                    enumerationOptions = EnumerationOptions(RecurseSubdirectories = true)
                )
                |> Seq.choose (fun f ->
                    let fileInfo = FileInfo f
                    if fileInfo.Length > largeFileLimit then
                        largeFiles.Add f
                        None
                    else
                        Some f
                )
                |> batch 8

        logger.LogInformation("Start process large files")
        do! largeFiles |> batch 4
    }

    member _.StartMonitor() =
        for folder in folders do
            let watcher = new FileSystemWatcher(folder)
            watcher.IncludeSubdirectories <- true
            watcher.NotifyFilter <- NotifyFilters.FileName ||| NotifyFilters.LastWrite ||| NotifyFilters.CreationTime
            watcher.Created.Add(fun e -> newFilesChannel.Writer.WriteAsync(e.FullPath) |> ignore)
            watcher.EnableRaisingEvents <- true
            watchers.Add watcher


    override _.ExecuteAsync(cancellationToken) = task {
        logger.LogInformation($"Start {nameof MemoryBackgroundService}")

        // Start the long running task without waiting
        this.SyncMedias(cancellationToken) |> ignore

        logger.LogInformation($"Monitor memory source")
        this.StartMonitor()

        while not cancellationToken.IsCancellationRequested do
            let! file = newFilesChannel.Reader.ReadAsync()

            let mutable retry = 0
            while retry < 10 && not (isFileReadyToRead file) do
                logger.LogInformation("File {file} is not ready to process", file)
                retry <- retry + 1
                do! Task.Delay 1000
            
            if retry >= 10 then
                logger.LogInformation("File {file} is not ready to be processed in 10 seconds, will ignore it", file)

            do! processFile file
    }

    override _.Dispose() =
        for watcher in watchers do
            watcher.Dispose()
