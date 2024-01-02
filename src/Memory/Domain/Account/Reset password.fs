namespace Memory.Domain.Account

open System
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open MediatR
open Fun.Result
open Memory.Db
open Memory.Domain

type ``Reset password handler``(memoryDb: MemoryDbContext, logger: ILogger<``Reset password handler``>) =
    interface IRequestHandler<``Reset password``, bool> with
        member _.Handle(request, _) = task {
            logger.LogInformation("User {name} tried to reset password", request.Name)

            let passwordHash = AccountUtils.CreatePasswordHash(request.Name, request.Password)
            let! user = memoryDb.Users.FirstOrDefaultAsync(fun x -> x.Name = request.Name && x.Password = passwordHash)

            if isNull user then
                logger.LogError("User {name} is not found or old password is wrong", request.Name)
                return false

            else
                logger.LogError("User {name} resetted his password", request.Name)

                let newPasswordHash = AccountUtils.CreatePasswordHash(request.Name, request.NewPassword)
                user.Password <- newPasswordHash
                user.LockoutTime <- Nullable()
                user.LockoutRetryCount <- 0

                do! memoryDb.SaveChangesAsync() |> Task.map ignore

                return true
        }
