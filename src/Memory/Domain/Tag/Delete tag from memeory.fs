namespace Memory.Domain.Tag

open System.Linq
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open MediatR
open Memory.Db
open Memory.Domain

type ``Delete tag from memeory handler``(memoryDb: MemoryDbContext, logger: ILogger<``Delete tag from memeory handler``>) =
    interface IRequestHandler<``Delete tag from memeory``, unit> with
        member _.Handle(request, _) = task {
            let! count =
                memoryDb.MemoryTags
                    .Where(fun x -> x.MemoryId = request.MemoryId && x.Tag.Name.ToLower() = request.Tag.ToLower())
                    .ExecuteDeleteAsync()

            if count > 0 then
                logger.LogInformation("Deleted tag {name} from memory {id}", request.Tag, request.MemoryId)
        }
