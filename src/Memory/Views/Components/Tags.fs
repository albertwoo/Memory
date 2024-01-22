namespace Memory.Views.Components

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Forms
open MediatR
open Fun.Htmx
open Fun.Result
open Fun.Blazor
open Memory
open Memory.Db
open Memory.Domain
open Memory.Views


type TagItem() as this =
    inherit FunComponent()

    static member RotateClass = if Random.Shared.Next() % 2 = 0 then "rotate-12" else "rotate-6"

    [<Parameter>]
    member val MemoryId = 0L with get, set

    [<Parameter>]
    member val Name = "" with get, set

    [<Parameter>]
    member val IsDeleting = false with get, set


    override _.Render() =
        html.inject (fun (mediator: IMediator) -> task {
            if this.IsDeleting then
                do! Domain.``Delete tag from memeory`` (this.MemoryId, this.Name) |> mediator.Send
                return html.none

            else
                return form {
                    hxPostComponent (QueryBuilder<TagItem>().Add(this).Add((fun x -> x.IsDeleting), true))
                    hxSwap_outerHTML
                    class' $"group text-neutral-600 bg-warning px-2 py-1 text-sm flex items-center gap-1 {TagItem.RotateClass}"
                    childContent [|
                        html.blazor<AntiforgeryToken> ()
                        label { this.Name }
                        button {
                            type' InputTypes.submit
                            class' "hidden group-hover:block btn btn-xs btn-ghost btn-circle btn-error"
                            Icons.Clear()
                        }
                    |]
                }
        })


type TagCreation() as this =
    inherit FunComponent()

    [<Parameter>]
    member val MemoryId = 0L with get, set

    [<Parameter>]
    member val Name = "" with get, set

    [<Parameter>]
    member val IsEditing = false with get, set

    [<Parameter>]
    member val ExistingTags = "" with get, set


    member _.Form() = form {
        hxPostComponent (QueryBuilder<TagCreation>().Add(this))
        hxSwap_outerHTML
        childContent [|
            html.blazor<AntiforgeryToken> ()
            input {
                type' InputTypes.text
                class' "input input-sm input-bordered w-[100px]"
                name (nameof this.Name)
                autofocus
                tabindex 0
            }
            input {
                hxTrigger' (hxEvt.keyboard.keyup + "[key=='Escape']", from = "body")
                hxGetComponent (QueryBuilder<TagCreation>().Add(this).Add((fun x -> x.IsEditing), false).Remove(fun x -> x.Name))
                hxTarget "closest form"
                hxSwap_outerHTML
                type' InputTypes.hidden
            }
        
        |]
    }

    member _.Creation() = button {
        hxGetComponent (QueryBuilder<TagCreation>().Add(this).Add((fun x -> x.IsEditing), true).Remove(fun x -> x.Name))
        hxSwap_outerHTML

        class' "btn btn-sm btn-ghost"
        Icons.Tag()
    }


    override _.Render() =
        html.inject (fun (mediator: IMediator) -> task {
            match this.Name, this.IsEditing with
            | SafeString _, _ ->
                let isTagExist = this.ExistingTags |> TagUtils.FromString |> Seq.contains (this.Name.ToLower())
                if not isTagExist then
                    do! Domain.``Add tag to memeory`` ([ this.MemoryId ], [ this.Name ]) |> mediator.Send

                return html.fragment [|
                    if not isTagExist then
                        html.blazor (
                            ComponentAttrBuilder<TagItem>()
                                .Add((fun x -> x.MemoryId), this.MemoryId)
                                .Add((fun x -> x.Name), this.Name)
                        )
                    this.Creation()
                |]

            | NullOrEmptyString, true -> return this.Form()

            | NullOrEmptyString, false -> return this.Creation()
        })


type TagLists() as this =
    inherit FunComponent()

    [<Parameter>]
    member val MemoryId = 0L with get, set

    [<Parameter>]
    member val EnableEdit = false with get, set


    override _.Render() =
        html.inject (fun (memoryDb: MemoryDbContext) -> task {
            let! tags =
                memoryDb.MemoryTags
                    .AsNoTracking()
                    .Where(fun x -> x.MemoryId = this.MemoryId)
                    .Select(fun x -> x.Tag.Name)
                    .ToListAsync()

            return html.fragment [|
                for tag in tags do
                    html.blazor (
                        ComponentAttrBuilder<TagItem>()
                            .Add((fun x -> x.MemoryId), this.MemoryId)
                            .Add((fun x -> x.Name), tag)
                    )

                if this.EnableEdit then
                    html.blazor (
                        ComponentAttrBuilder<TagCreation>()
                            .Add((fun x -> x.MemoryId), this.MemoryId)
                            .Add((fun x -> x.ExistingTags), TagUtils.ToString tags)
                    )
            |]
        })


type TagFilterList() as this =
    inherit FunComponent()

    [<Parameter>]
    member val SelectTagParamName = "" with get, set

    [<Parameter>]
    member val SelectedTagsParamName = "" with get, set

    [<Parameter>]
    member val SelectedTags = Parsable List.empty<string> with get, set

    override _.Render() =
        html.inject (fun (memoryDb: MemoryDbContext) -> task {
            let! tags = memoryDb.Tags.AsNoTracking().ToListAsync()
            
            return html.fragment [|
                // So other components can include this params for htmx
                if String.IsNullOrEmpty this.SelectedTagsParamName |> not then
                    input {
                        type' InputTypes.hidden
                        name this.SelectedTagsParamName
                        value this.SelectedTags
                    }
                for tag in tags do
                    let isSelected =
                        this.SelectedTags.Value
                        |> Seq.exists (fun x -> x.Equals(tag.Name, StringComparison.OrdinalIgnoreCase))
                    let selectedClass = if isSelected then "bg-warning" else ""
                    button {
                        name (
                            if String.IsNullOrEmpty this.SelectTagParamName then
                                nameof this.SelectedTags
                            else
                                this.SelectTagParamName
                        )
                        value (
                            if isSelected then
                                this.SelectedTags.Value
                                |> List.filter (fun x -> x.Equals(tag.Name, StringComparison.OrdinalIgnoreCase) |> not)
                                |> Parsable
                            else if this.SelectedTags.Value.Length > 0 then
                                Parsable (this.SelectedTags.Value @[ tag.Name])
                            else
                                Parsable [ tag.Name ]
                        )
                        type' InputTypes.submit
                        class' $"text-neutral-600 px-2 py-1 text-sm flex items-center gap-1 hover:bg-warning {selectedClass} {TagItem.RotateClass}"
                        tag.Name
                    }
            |]
        })
