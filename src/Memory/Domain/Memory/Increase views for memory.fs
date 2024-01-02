namespace Memory.Domain.MemoryCrud

open System.Linq
open Microsoft.EntityFrameworkCore
open MediatR
open Fun.Result
open Memory.Db
open Memory.Domain

type ``Increase views for memory handler``(memoryDb: MemoryDbContext) =
    interface IRequestHandler<``Increase views for memory``, unit> with
        member _.Handle(request, _) = task {
            do!
                memoryDb
                    .Memories
                    .AsNoTracking()
                    .Where(fun x -> x.Id = request.Id)
                    .ExecuteUpdateAsync(fun m ->
                        m.SetProperty((fun x -> x.Views), (fun m -> m.Views + 1))
                    )
                |> Task.map ignore
        }
