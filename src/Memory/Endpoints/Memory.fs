namespace Memory.Endpoints

open Memory.Db
open Memory.Domain
open Memory.Options

open System
open System.IO
open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Options
open Microsoft.Extensions.DependencyInjection
open Microsoft.EntityFrameworkCore

[<Extension>]
type Memory =

    [<Extension>]
    static member MapMemory(app: WebApplication) =
        let appOptions = app.Services.GetRequiredService<IOptions<AppOptions>>().Value
        let optimizedFolder = appOptions.GetOptimizedFolder(app.Environment)

        app
            .MapGet(
                $"{AppOptions.OptimizedUrlPrefix}/{{fileName}}",
                Func<_, _, _>(fun (fileName: string) (ctx: HttpContext) -> task {
                    let file = optimizedFolder </> fileName
                    if File.Exists file then
                        let contentType =
                            match transformedFormat fileName, fileName with
                            | Some VideoFormat, _ -> $"video/{AppOptions.OptimizedVideoFormat}"
                            | Some ImageFormat, _ -> $"image/{AppOptions.OptimizedImageFormat}"
                            | _, Gif -> "image/gif"
                            | _ -> "*/*"
                        ctx.Response.Headers.CacheControl <- $"public, max-age={60 * 60 * 24 * 7}"
                        return Results.File(file, enableRangeProcessing = true, contentType = contentType, fileDownloadName = fileName)
                    else
                        return Results.NotFound()
                })
            )
            .RequireAuthorization()
        |> ignore

        app
            .MapGet(
                "/memory/original/{memoryId:long}",
                Func<_, _, _, _>(fun (memoryId: int64) (memoryDb: MemoryDbContext) (ctx: HttpContext) -> task {
                    let! memory = memoryDb.Memories.FirstOrDefaultAsync(fun x -> x.Id = memoryId)
                    match memory with
                    | null
                    | _ when not (File.Exists memory.FilePath) -> return Results.NotFound()
                    | _ ->
                        ctx.Response.Headers.CacheControl <- $"public, max-age={60 * 60 * 24 * 7}"
                        let contentType =
                            match memory.FileExtension with
                            | VideoFormat -> "video/*"
                            | ImageFormat -> "image/*"
                            | _ -> "*/*"
                        return
                            Results.File(
                                memory.FilePath,
                                lastModified = memory.CreationTime,
                                enableRangeProcessing = true,
                                contentType = contentType,
                                fileDownloadName = Path.GetFileName memory.FilePath
                            )
                })
            )
            .RequireAuthorization()
        |> ignore
