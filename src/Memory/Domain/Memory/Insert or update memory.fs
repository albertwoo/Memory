namespace Memory.Domain.MemoryCrud

open System
open System.IO
open System.Linq
open System.Text
open System.Security.Cryptography
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open Fun.Result
open MediatR
open Memory.Db
open Memory.Domain
open System.Diagnostics


module private Hash =
    let getStringHash (str: string) =
        use md5 = MD5.Create()
        let hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(str))
        BitConverter.ToString(hashBytes).Replace("-", "").ToLower()

    let getFileContentHash filePath =
        use sha256 = SHA256.Create()
        use stream = new FileStream(filePath, FileMode.Open, FileAccess.Read)
        let hashBytes = sha256.ComputeHash(stream)
        BitConverter.ToString(hashBytes).Replace("-", "").ToLower()


type ``Insert or update memory handler``(memoryDb: MemoryDbContext, logger: ILogger<``Insert or update memory handler``>) =

    interface IRequestHandler<``Insert or update memory``, int64> with
        member _.Handle(request, _) = task {
            let fileInfo = FileInfo request.File
            let filePathHash = Hash.getStringHash request.File

            let smallFileContentHash =
                if fileInfo.Length < int64 (1024 * 1024 * 10) then
                    let sw = Stopwatch.StartNew()
                    let hash = Hash.getFileContentHash request.File
                    logger.LogInformation("Calculate file {name} content hash in {time}ms", request.File, sw.ElapsedMilliseconds)
                    Some hash
                else
                    None

            let fileCreationTime =
                if fileInfo.LastWriteTime < fileInfo.CreationTime then
                    fileInfo.LastWriteTime
                else
                    fileInfo.CreationTime

            let! id =
                memoryDb.Memories
                    .AsNoTracking()
                    .Where(fun f ->
                        // If the file path is same, we treat it as same
                        f.FilePathHash = filePathHash
                        // For large file, we check file size and creation time to see if they are same
                        || (smallFileContentHash.IsNone
                            && f.FileSize > 0
                            && f.FileSize = fileInfo.Length
                            && f.CreationTime = fileCreationTime)
                        // For small file, we check the content hash directly
                        || (smallFileContentHash.IsSome && f.FileContentHash = smallFileContentHash.Value)
                    )
                    .Select(fun x -> x.Id)
                    .FirstOrDefaultAsync()

            if id = 0 then
                let result =
                    memoryDb.Memories.Add(
                        Memory(
                            FilePath = request.File,
                            FilePathHash = filePathHash,
                            FileExtension = fileInfo.Extension,
                            FileSize = fileInfo.Length,
                            FileContentHash = Option.toObj smallFileContentHash,
                            CreationTime = fileCreationTime
                        )
                    )

                do! memoryDb.SaveChangesAsync() |> Task.map ignore

                logger.LogInformation("Created memory {id} for {file}", result.Entity.Id, request.File)

                return result.Entity.Id

            else
                logger.LogInformation("Already created memory {id} for {file} before", id, request.File)

                return id
        }
