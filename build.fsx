#r "nuget: Fun.Build"
#r "nuget: Fake.IO.Zip"

open System.IO
open Fun.Build
open Fake.IO
open Fake.IO.Globbing.Operators

let (</>) x y = Path.Combine(x, y)

let sourceCss = __SOURCE_DIRECTORY__ </> "src" </> "Memory" </> "wwwroot" </> "app.css"
let targetCss = __SOURCE_DIRECTORY__ </> "src" </> "Memory" </> "wwwroot" </> "app-generated.css"

let publishDir = __SOURCE_DIRECTORY__ </> "publish"
let publishLinux64Dir = publishDir </> "linux64"
let publishLinuxArm64Dir = publishDir </> "linuxarm64"
let publishWin64Dir = publishDir </> "win64"
let publishWin64IISDir = publishDir </> "win64-iis"
let ffmpegDirName = "ffmpeg"

let options = {|
    platform = CmdArg.Create(longName = "--platform", values = [ "win64"; "linux64"; "linuxarm64" ])
    iis = CmdArg.Create(longName = "--iis")
    styles = EnvArg.Create("STYLES")
|}


let stage_checkEnvs = stage "check-env" {
    run "dotnet --version"
    stage "styles" {
        run "npm -v"
        whenAny {
            cmdArg "--styles"
            envVar options.styles
        }
        workingDir "styles"
        run "npm install"
    }
}

let stage_donwloadFFmpeg targetDir = stage "download_ffmpeg" {
    whenCmdArg options.platform
    whenCmdArg "--ffmpeg" "" "Enable to download after publish for bundling"
    run (fun ctx -> task {
        let platform = ctx.GetCmdArg options.platform
        let zipType = if platform.Contains "linux" then "tar.xz" else "zip"
        let sourceDir = __SOURCE_DIRECTORY__ </> $"ffmpeg-{platform}"
        let zipFile = sourceDir + "." + zipType

        if File.Exists zipFile |> not then
            let url =
                $"https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-{platform}-gpl-shared.{zipType}"
            
            printfn "Download ffmpeg from %s" url

            use client = new System.Net.Http.HttpClient()
            let! stream = client.GetStreamAsync(url)
            use file = File.Create(zipFile)
            stream.CopyTo(file)

        if Directory.Exists sourceDir |> not then Zip.unzip sourceDir zipFile

        Directory.ensure targetDir

        !!(sourceDir </> "**" </> "bin" </> "*.*")
        |> Seq.iter (fun f ->
            printfn "Copy %s" f
            File.Copy(f, targetDir </> Path.GetFileName f, overwrite = true)
        )
    })
}


pipeline "dev" {
    description "Run the projects locally"
    stage_checkEnvs
    stage_donwloadFFmpeg (__SOURCE_DIRECTORY__ </> "src" </> "Memory" </> ffmpegDirName)
    stage "run" {
        paralle
        stage "styles" {
            whenCmdArg "--styles"
            workingDir "styles"
            run $"npx tailwindcss -i {sourceCss} -o {targetCss} --watch"
        }
        stage "server" {
            workingDir ("src" </> "Memory")
            run "dotnet clean"
            run "dotnet watch run"
        }
    }
    runIfOnlySpecified
}

pipeline "publish" {
    description "Publish single executable"
    envVars [ options.styles.Name, "" ]
    stage_checkEnvs
    stage "styles" {
        workingDir "styles"
        run $"npx tailwindcss -i {sourceCss} -o {targetCss} --minify"
    }
    stage "bundle" {
        workingDir ("src" </> "Memory")
        stage "linux64" {
            whenCmdArg { options.platform with Values = [ "linux64" ] }
            run $"""dotnet publish -c Release -r linux-x64 /p:PublishSingleFile=true /p:PublishTrimmed=false -o {publishLinux64Dir}"""
            stage_donwloadFFmpeg (publishLinux64Dir </> ffmpegDirName)
        }
        stage "linuxarm64" {
            whenCmdArg { options.platform with Values = [ "linuxarm64" ] }
            run $"""dotnet publish -c Release -r linux-arm64 /p:PublishSingleFile=true /p:PublishTrimmed=false -o {publishLinuxArm64Dir}"""
            stage_donwloadFFmpeg (publishLinuxArm64Dir </> ffmpegDirName)
        }
        stage "win" {
            whenCmdArg { options.platform with Values = [ "win64" ] }
            whenNot { cmdArg options.iis }
            run $"""dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=false -o {publishWin64Dir}"""
            stage_donwloadFFmpeg (publishWin64Dir </> ffmpegDirName)
        }
        stage "win-iis" {
            whenCmdArg { options.platform with Values = [ "win64" ] }
            whenCmdArg options.iis
            run $"""dotnet publish -c Release -r win-x64 /p:PublishSingleFile=false /p:PublishTrimmed=false -o {publishWin64IISDir}"""
            stage_donwloadFFmpeg (publishWin64IISDir </> ffmpegDirName)
        }
    }
    runIfOnlySpecified
}

tryPrintPipelineCommandHelp ()
