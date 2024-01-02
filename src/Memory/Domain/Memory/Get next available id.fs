namespace Memory.Domain.MemoryCrud

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open MediatR
open Memory.Db
open Memory.Domain

[<CLIMutable>]
type RowAndId = { Id: int64; Row: int64 }

type ``Get next available id handler``(memoryDb: MemoryDbContext) =
    interface IRequestHandler<``Get next available id``, int64 option> with
        member _.Handle(request, _) = task {
            let! currentTime =
                memoryDb.Memories
                    .AsNoTracking()
                    .Where(fun x -> x.Id = request.CurrentId)
                    .Select(fun x -> x.CreationTime)
                    .FirstOrDefaultAsync()

            if currentTime = Unchecked.defaultof<DateTime> then
                return None
            else
                let! result =
                    memoryDb.Memories
                        .AsNoTracking()
                        .Where(fun x ->
                            (request.Year.IsNone
                             || (request.ExcludeYear && x.Year <> request.Year.Value)
                             || (not request.ExcludeYear && x.Year = request.Year.Value))
                            && (request.Month.IsNone || x.Month = request.Month.Value)
                            && (request.Day.IsNone || x.Day = request.Day.Value)
                            && (request.Tags.Length = 0
                                || request.Tags.All(fun tag -> x.MemoryTags.Any(fun mt -> mt.Tag.Name.ToLower() = tag.ToLower())))
                            && (x.CreationTime < currentTime || (x.CreationTime = currentTime && x.Id > request.CurrentId))
                        )
                        .OrderByDescending(fun x -> x.CreationTime)
                        .ThenBy(fun x -> x.Id)
                        .Select(fun x -> x.Id)
                        .FirstOrDefaultAsync()

                if result = 0 then return None else return Some result
        }
