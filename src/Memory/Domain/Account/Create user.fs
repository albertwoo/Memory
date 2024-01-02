namespace Memory.Domain.Account

open System
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open MediatR
open Memory.Db
open Memory.Domain

type ``Create user handler``(memoryDb: MemoryDbContext, logger: ILogger<``Create user handler``>) =
    interface IRequestHandler<``Create user``, UserCreationResult> with
        member _.Handle(request, _) = task {
            logger.LogInformation("Start create user {name}", request.Name)

            if String.IsNullOrEmpty request.Name then
                logger.LogError("User name is empty")
                return UserCreationResult.InvalidName

            else if String.IsNullOrEmpty request.Password then
                logger.LogError("Password is empty")
                return UserCreationResult.InvalidPassword

            else
                let! isUserExist = memoryDb.Users.AnyAsync(fun x -> x.Name.ToLower() = request.Name.ToLower())

                if isUserExist then
                    logger.LogError("Cannot create with same user {name}", request.Name)
                    return UserCreationResult.InvalidName

                else
                    let passwordHash = AccountUtils.CreatePasswordHash(request.Name, request.Password)
                    let _ = memoryDb.Users.Add(User(Name = request.Name, Password = passwordHash))
                    let! _ = memoryDb.SaveChangesAsync()

                    logger.LogInformation("User {name} is created", request.Name)
                    return UserCreationResult.Success
        }
