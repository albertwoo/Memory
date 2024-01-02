namespace Memory.Domain.MemoryCrud

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open MediatR
open Memory.Db
open Memory.Domain

type ``Count ordered memories by creation time desc with filter handler``(memoryDb: MemoryDbContext) =
    interface IRequestHandler<``Count ordered memories by creation time desc with filter``, int64> with
        member _.Handle(request, _) = task {
            let fromCreationTime = defaultArg request.FromTime DateTime.MinValue
            let toCreationTime = defaultArg request.ToTime DateTime.MaxValue

            return!
                memoryDb.Memories
                    .AsNoTracking()
                    .Where(fun x ->
                        x.CreationTime >= fromCreationTime
                        && x.CreationTime <= toCreationTime
                        && (request.Tags.Length = 0 || request.Tags.All(fun tag -> x.MemoryTags.Any(fun mt -> mt.Tag.Name.ToLower() = tag.ToLower())))
                        && (request.Day.IsNone || request.Day.Value = x.Day)
                    )
                    .OrderByDescending(fun x -> x.CreationTime)
                    .LongCountAsync()
        }
