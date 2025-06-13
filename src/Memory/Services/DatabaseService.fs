namespace Memory.Services

open Microsoft.Extensions.Options
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open MediatR
open Memory.Db
open Memory.Options

type DatabaseService(appOptions: IOptions<AppOptions>, memoryDb: MemoryDbContext, mediator: IMediator, logger: ILogger<DatabaseService>) =

    member _.Migrate() = task {
        logger.LogInformation("Migrate database")

        memoryDb.Database.Migrate()
    }
