namespace Memory.Services

open System.Linq
open Microsoft.Extensions.Options
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open MediatR
open Memory.Db
open Memory.Options
open Memory.Domain

type DatabaseService(appOptions: IOptions<AppOptions>, memoryDb: MemoryDbContext, mediator: IMediator, logger: ILogger<DatabaseService>) =

    member _.Migrate() = task {
        logger.LogInformation("Migrate database")

        memoryDb.Database.Migrate()

        for user in appOptions.Value.Users do
            if memoryDb.Users.Any(fun x -> x.Name = user.Name) |> not then
                let! result = ``Create user`` (user.Name, user.Password) |> mediator.Send
                match result with
                | UserCreationResult.Success -> ()
                | _ -> failwith $"Create {user.Name} failed {result}"

            else
                logger.LogWarning("User {name} is already added, will ignore it", user.Name)

            memoryDb.SaveChanges() |> ignore
    }
