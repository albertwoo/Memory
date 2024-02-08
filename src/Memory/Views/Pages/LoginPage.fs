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

[<Route "/account/login">]
type LoginPage() as this =
    inherit FunComponent()

    [<Parameter>]
    member val IsFirstLoad = true with get, set

    [<Parameter>]
    member val Name = "" with get, set

    [<Parameter>]
    member val Password = "" with get, set

    [<Parameter; SupplyParameterFromQuery>]
    member val ReturnUrl = "" with get, set

    member _.Form(?invalidCredential: bool) = form {
        hxPostComponent (QueryBuilder<LoginPage>().Add((fun x -> x.ReturnUrl), this.ReturnUrl).Add((fun x -> x.IsFirstLoad), false))
        hxSwap_outerHTML
        class' "mx-auto rounded-md shadow-md shadow-neutral-500/30 my-10 w-[300px] flex flex-col gap-4 border border-primary/20 p-5"
        childContent [|
            PageTitle'() { "Login" }

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
                placeholder "Password"
                type' InputTypes.password
                name (nameof this.Password)
                value this.Password
            }
            button {
                class' "btn btn-primary w-full"
                type' InputTypes.submit
                "Login"
            }
            a {
                class' "link link-info text-center"
                href "/account/reset"
                "Reset password"
            }

            if
                not this.IsFirstLoad
                && ((String.IsNullOrEmpty this.Name && String.IsNullOrEmpty this.Password) || defaultArg invalidCredential false)
            then
                div {
                    class' "text-error text-center"
                    "Invalid credential"
                }
        |]
    }

    override _.Render() =
        html.inject (fun (mediator: IMediator, ctx: IHttpContextAccessor) -> task {
            if ctx.HttpContext.User <> null && ctx.HttpContext.User.Identity.IsAuthenticated then
                do! ctx.HttpContext.SignOutAsync()
                return script { NativeJs.ReloadPage() }

            else if String.IsNullOrEmpty this.Name || String.IsNullOrEmpty this.Password then
                return this.Form()

            else
                let! result = ``Login by password`` (this.Name, this.Password) |> mediator.Send

                match result with
                | LoginResult.Success ->
                    let returnUrl = if String.IsNullOrEmpty this.ReturnUrl then "/" else this.ReturnUrl
                    return
                        div.create [|
                            script { NativeJs.GoTo returnUrl }
                            p {
                                class' "text-success text-3xl font-bold mt-20 text-center uppercase"
                                "Success, redirecting..."
                            }
                        |]

                | LoginResult.InvalidCredential -> return this.Form(invalidCredential = true)

                | LoginResult.Lockedout ->
                    return div {
                        class' "text-warning text-3xl font-bold mt-20 text-center"
                        "You have been locked out!!!"
                    }
        })
