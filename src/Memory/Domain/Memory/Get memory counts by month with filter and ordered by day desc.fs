namespace Memory.Domain.MemoryCrud

open System.Linq
open Microsoft.EntityFrameworkCore
open MediatR
open Memory.Db
open Memory.Domain

type ``Get memory counts by month with filter and ordered by day desc handler``(memoryDb: MemoryDbContext) =
    interface IRequestHandler<``Get memory counts by month with filter and ordered by day desc``, MemoryCountInDays seq> with
        member _.Handle(request, _) = task {
            return
                memoryDb.Memories
                    .AsNoTracking()
                    .Where(fun x ->
                        x.Year = request.Year
                        && x.Month = request.Month
                        && (request.Tags.Length = 0 || request.Tags.All(fun tag -> x.MemoryTags.Any(fun mt -> mt.Tag.Name.ToLower() = tag.ToLower())))
                    )
                    .GroupBy(fun x -> x.Day)
                    .Select(fun x -> {
                        Day = x.Key
                        Count = x.Count()
                    })
                    .ToArray()
                    .OrderByDescending(fun x -> x.Day)
        }
