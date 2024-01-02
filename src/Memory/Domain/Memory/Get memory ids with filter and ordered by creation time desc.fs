namespace Memory.Domain.MemoryCrud

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open MediatR
open Memory.Db
open Memory.Domain

type ``Get memory ids with filter and ordered by creation time desc handler``(memoryDb: MemoryDbContext) =
    interface IRequestHandler<``Get memory ids with filter and ordered by creation time desc``, MemoryIdWithExtension seq> with
        member _.Handle(request, _) = task {
            let fromCreationTime = defaultArg request.FromTime DateTime.MinValue
            let toCreationTime = defaultArg request.ToTime DateTime.MaxValue

            return
                memoryDb.Memories
                    .AsNoTracking()
                    .Where(fun x ->
                        x.CreationTime >= fromCreationTime
                        && x.CreationTime <= toCreationTime
                        && (request.Tags.Length = 0 || request.Tags.All(fun tag -> x.MemoryTags.Any(fun mt -> mt.Tag.Name.ToLower() = tag.ToLower())))
                    )
                    .OrderByDescending(fun x -> x.CreationTime)
                    .ThenBy(fun x -> x.Id)
                    .Skip(int request.Offset)
                    .Take(request.PageSize)
                    .Select(fun x -> {
                        MemoryIdWithExtension.Id = x.Id
                        Extension = x.FileExtension
                    })
                    .ToArray()
        }
