namespace Memory.Domain.MemoryCrud

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open MediatR
open Memory.Db
open Memory.Domain

type ``Get memory max count by month with filter handler``(memoryDb: MemoryDbContext) =

    interface IRequestHandler<``Get memory max count by month with filter``, int> with
        member _.Handle(request, _) = task {
            try
                return!
                    memoryDb.Memories
                        .AsNoTracking()
                        .Where(fun x ->
                            (request.Year.IsNone || x.Year = request.Year.Value)
                            && (request.Month.IsNone || x.Month = request.Month.Value)
                            && (request.Tags.Length = 0
                                || request.Tags.All(fun tag -> x.MemoryTags.Any(fun mt -> mt.Tag.Name.ToLower() = tag.ToLower())))
                        )
                        .GroupBy(fun x -> x.Month)
                        .Select(fun x -> x.Count())
                        .MaxAsync()
            with :? InvalidOperationException ->
                return 0
        }
