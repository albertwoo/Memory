namespace Memory.Domain

open System
open System.Text
open System.Security.Cryptography
open MediatR


type AccountUtils =
    static member CreatePasswordHash(name: string, password: string) =
        use md5 = MD5.Create()
        BitConverter.ToString(md5.ComputeHash(Encoding.ASCII.GetBytes($"{name.ToLower()}#{password}")))

    static member MaxRetryBeforeLockout = 3
    static member LockoutDuration = TimeSpan.FromMinutes(15)


[<RequireQualifiedAccess>]
type UserCreationResult =
    | Success
    | InvalidName
    | InvalidPassword

type ``Create user``(name: string, password: string) =
    interface IRequest<UserCreationResult>
    member _.Name = name
    member _.Password = password


[<RequireQualifiedAccess>]
type LoginResult =
    | Success
    | InvalidCredential
    | Lockedout

type ``Login by password``(name: string, password: string) =
    interface IRequest<LoginResult>
    member _.Name = name
    member _.Password = password


type ``Reset password``(name: string, password: string, newPassword: string) =
    interface IRequest<bool>
    member _.Name = name
    member _.Password = password
    member _.NewPassword = newPassword
