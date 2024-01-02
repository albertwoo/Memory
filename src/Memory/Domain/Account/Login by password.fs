namespace Memory.Domain.Account

open System
open System.Security.Claims
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open MediatR
open Fun.Result
open Memory.Db
open Memory.Domain

type ``Login by password handler``(memoryDb: MemoryDbContext, ctx: IHttpContextAccessor, logger: ILogger<``Login by password handler``>) =
    interface IRequestHandler<``Login by password``, LoginResult> with
        member _.Handle(request, _) = task {
            logger.LogInformation("User {name} tried to login", request.Name)

            let passwordHash = AccountUtils.CreatePasswordHash(request.Name, request.Password)
            let! user = memoryDb.Users.FirstOrDefaultAsync(fun x -> x.Name = request.Name)

            if isNull user then
                logger.LogInformation("User {name} is not found", request.Name)
                return LoginResult.InvalidCredential

            else if
                user.LockoutRetryCount >= AccountUtils.MaxRetryBeforeLockout
                && user.LockoutTime.HasValue
                && DateTime.Now - user.LockoutTime.Value < AccountUtils.LockoutDuration
            then
                logger.LogInformation("User {name} is been locked out", request.Name)
                return LoginResult.Lockedout

            else if user.Password <> passwordHash then
                logger.LogInformation("User for {name} credential is not valid", request.Name)

                user.LockoutTime <- DateTime.Now
                user.LockoutRetryCount <-
                    if user.LockoutRetryCount >= AccountUtils.MaxRetryBeforeLockout then
                        0
                    else
                        user.LockoutRetryCount + 1

                do! memoryDb.SaveChangesAsync() |> Task.map ignore

                return LoginResult.InvalidCredential

            else
                let issuer = "memory"

                let claims = [ Claim(ClaimTypes.Name, request.Name, ClaimValueTypes.String, issuer) ]

                let userIdentity =
                    ClaimsIdentity(
                        claims,
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        ClaimsIdentity.DefaultNameClaimType,
                        ClaimsIdentity.DefaultRoleClaimType
                    )

                let userPrincipal = ClaimsPrincipal(userIdentity)

                let authProperties =
                    AuthenticationProperties(ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1), IsPersistent = true, AllowRefresh = false)

                do! ctx.HttpContext.SignInAsync(userPrincipal, authProperties)

                user.LockoutTime <- Nullable()
                user.LockoutRetryCount <- 0
                do! memoryDb.SaveChangesAsync() |> Task.map ignore

                logger.LogInformation("User {name} logged in", request.Name)

                return LoginResult.Success
        }
