namespace Memory.Domain.Tag

open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open MediatR
open Fun.Result
open Memory.Db
open Memory.Domain

type ``Add tag to memeory handler``
    (memoryDb: MemoryDbContext, logger: ILogger<``Add tag to memeory handler``>) =
    
    interface IRequestHandler<``Add tag to memeory``, unit> with
        member _.Handle(request, _) = task {
            let mutable shouldSave = false

            for tagName in request.Tags do
                let! tag = memoryDb.Tags.FirstOrDefaultAsync(fun x -> x.Name.ToLower() = tagName.ToLower())
                let memmoryTag = if tag = null then Tag(Name = tagName) else tag

                if request.MemoryIds |> Seq.length > 0 then
                    for memoryId in request.MemoryIds do
                        let! isAlreadyTagged =
                            if tag = null then
                                Task.retn false
                            else
                                memoryDb.MemoryTags
                                    .AsNoTracking()
                                    .AnyAsync(fun x -> x.TagId = memmoryTag.Id && x.MemoryId = memoryId)

                        if not isAlreadyTagged then
                            logger.LogInformation("Tagged memory {id} with tag {name}", memoryId, tagName)
                            memoryDb.MemoryTags.Add(MemoryTag(MemoryId = memoryId, Tag = memmoryTag)) |> ignore
                            shouldSave <- true
                        else
                            logger.LogInformation("Tag {name} is already added to memory {id} before", tagName, memoryId)

                else if tag = null then
                    logger.LogInformation("Add new tags {names}", request.Tags)
                    memoryDb.Tags.Add(memmoryTag) |> ignore
                    shouldSave <- true

            if shouldSave then do! memoryDb.SaveChangesAsync() |> Task.map ignore
        }
