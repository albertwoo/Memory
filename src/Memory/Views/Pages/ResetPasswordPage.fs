namespace Memory.Views.Pages

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.AspNetCore.Components.Forms
open Microsoft.AspNetCore.Authentication
open MediatR
open Fun.Htmx
open Fun.Blazor
open Memory.Domain
open Memory.Views

[<Route "/account/reset">]
type ResetPasswordPage() as this =
    inherit FunComponent()

    [<Parameter>]
    member val IsFirstLoad = true with get, set

    [<Parameter>]
    member val Name = "" with get, set

    [<Parameter>]
    member val Password = "" with get, set

    [<Parameter>]
    member val NewPassword = "" with get, set

    [<Parameter>]
    member val ConfirmNewPassword = "" with get, set


    member _.IsInvalidForReset =
        String.IsNullOrEmpty this.Name
        || String.IsNullOrEmpty this.NewPassword
        || this.Password = this.NewPassword
        || this.NewPassword <> this.ConfirmNewPassword


    member _.Form() = form {
        hxPostComponent (QueryBuilder<ResetPasswordPage>().Add((fun x -> x.IsFirstLoad), false))
        hxSwap_outerHTML

        class' "mx-auto rounded-md shadow-md shadow-neutral-500/30 my-10 w-[300px] flex flex-col gap-4 border-primary/20 p-5"

        PageTitle'() { "Reset" }
        html.blazor<AntiforgeryToken> ()

        input {
            class' "input input-bordered input-primary w-full"
            placeholder "Name"
            type' InputTypes.text
            name (nameof this.Name)
            value this.Name
        }
        input {
            class' "input input-bordered input-primary w-full"
            placeholder "Old password"
            type' InputTypes.password
            name (nameof this.Password)
            value this.Password
        }
        input {
            class' "input input-bordered input-primary w-full"
            placeholder "New password"
            type' InputTypes.password
            name (nameof this.NewPassword)
            value this.NewPassword
        }
        input {
            class' "input input-bordered input-primary w-full"
            placeholder "Confirm password"
            type' InputTypes.password
            name (nameof this.ConfirmNewPassword)
            value this.ConfirmNewPassword
        }
        button {
            class' "btn btn-primary w-full"
            type' InputTypes.submit
            "Reset"
        }

        if not this.IsFirstLoad && this.IsInvalidForReset then
            div {
                class' "text-error text-center"
                "Invalid credential"
            }
    }

    override _.Render() =
        html.inject (fun (mediator: IMediator, ctx: IHttpContextAccessor) -> task {
            if this.IsInvalidForReset then
                return this.Form()

            else
                let! result = ``Reset password`` (this.Name, this.Password, this.NewPassword) |> mediator.Send

                match result with
                | true ->
                    do! ctx.HttpContext.SignOutAsync()

                    return div {
                        script { NativeJs.GoTo("/account/login") }
                        p {
                            class' "text-success text-3xl opacity-75 mt-20 text-center"
                            "Success, redirecting..."
                        }
                    }

                | false -> return this.Form()
        })
