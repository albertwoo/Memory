namespace Memory.Views.Components

open System.IO
open Microsoft.Extensions.Options
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Components
open Microsoft.EntityFrameworkCore
open Fun.Blazor
open Memory.Db
open Memory.Options
open Memory.Views


type MemoryMeta() as this =
    inherit FunComponent()

    [<Parameter>]
    member val Id = 0L with get, set

    override _.Render() =
        html.inject (fun (memoryDb: MemoryDbContext, appOptions: IOptions<AppOptions>, env: IHostEnvironment, cssRules: IScopedCssRules) -> task {
            let! memory = memoryDb.Memories.AsNoTracking().Include(fun x -> x.MemoryMeta).FirstOrDefaultAsync(fun x -> x.Id = this.Id)

            match memory with
            | null -> return div { "Not found" }
            | _ ->
                let path =
                    [ yield! appOptions.Value.SourceFolders; appOptions.Value.GetRootedUploadFolder(env) ]
                    |> Seq.tryPick (fun f ->
                        if memory.FilePath.StartsWith f then
                            Some(memory.FilePath.Substring(f.Length))
                        else
                            None
                    )
                    |> Option.defaultWith (fun () -> Path.GetFileName memory.FilePath)

                return div.create [
                    h3 {
                        class' "text-xl text-primary font-bold"
                        style { "overflow-wrap", "break-word" }
                        path
                    }
                    p {
                        class' "font-semibold my-2"
                        memory.CreationTime.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                    match memory.MemoryMeta with
                    | null -> ()
                    | memoryMeta -> section {
                        class' "text-sm mt-2"
                        style { cssRules.FadeInUpCss() }
                        childContent [|
                            p { memoryMeta.Make + " " + memoryMeta.Modal }
                            p { memoryMeta.LensModal }
                            match Option.ofNullable memoryMeta.Latitude, Option.ofNullable memoryMeta.Longitude with
                            | Some latitude, Some longitude -> p {
                                class' "flex items-center gap-2"
                                childContent [|
                                    Icons.MapPin(class' = "h-3 w-3")
                                    a {
                                        target "_blank"
                                        href $"https://ditu.amap.com/regeo?lng={longitude}&lat={latitude}"
                                        // href $"https://cn.bing.com/maps?cp={memoryMeta.Latitude}~{memoryMeta.Longitude}&lvl=17"
                                        class' "link"
                                        $"%.8f{latitude}, %.8f{longitude}"
                                    }
                                |]
                              }
                            | _ -> ()
                        |]
                      }
                ]
        })


[<ComponentResponseCacheFor1Day>]
type MemoryMetaModal() as this =
    inherit FunComponent()

    [<Parameter>]
    member val Id = 0L with get, set

    override _.Render() =
        Modal.Create(
            title = html.none,
            content = html.blazor (ComponentAttrBuilder<MemoryMeta>().Add((fun x -> x.Id), this.Id)),
            size = ModalSize.Medium
        )
