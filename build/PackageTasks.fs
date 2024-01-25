module PackageTasks

open Fake.Core
open Fake.DotNet
open Fake.IO.Globbing.Operators
open BlackFox
open BlackFox.Fake

open ProjectInfo
open Helpers

open BasicTasks
open TestTasks

type RunTime = 
    | Win
    | Linux
    | Mac
    | MacARM

    member this.GetRuntime() =
        match this with
        | Win -> "win-x64"
        | Linux -> "linux-x64"
        | Mac -> "osx-x64"
        | MacARM -> "osx-arm64"

    member this.GetRuntimeFolder() =
        match this with
        | Win -> "win-x64"
        | Linux -> "linux-x64"
        | Mac -> "osx-x64"
        | MacARM -> "osx-arm64"

    member this.GetPlatform() =
        match this with
        | Win -> "x64"
        | Linux -> "x64"
        | Mac -> "x64"
        | MacARM -> "arm64"


let publishBinaryInPath (path : string) (version : string) (versionSuffix : string Option) (runtime : RunTime) = 
    project
    |> DotNet.publish (fun p ->
        let standardParams = Fake.DotNet.MSBuild.CliArguments.Create ()
        {
            p with
                Runtime = Some (runtime.GetRuntime())
                Configuration = DotNet.BuildConfiguration.fromString configuration
                OutputPath = Some path
                MSBuildParams = {
                    standardParams with
                        Properties = [
                            "VersionPrefix", version
                            if versionSuffix.IsSome then "VersionSuffix", versionSuffix.Value
                            "Platform", runtime.GetPlatform()
                            "PublishSingleFile", "true"
                        ]
                }               
        }        
    )

let publishBinary (version : string) (versionSuffix : string Option) (runtime : RunTime) = 
    let runTimeFolder = runtime.GetRuntimeFolder()
    let outputFolder = sprintf "%s/%s" publishDir runTimeFolder
    publishBinaryInPath outputFolder version versionSuffix runtime
   
let publishBinaryForFat (version : string) (versionSuffix : string Option) (runtime : RunTime) = 
    let versionString = if versionSuffix.IsSome then sprintf "%s-%s" version versionSuffix.Value else version
    let outputFolder = sprintf "%s/%s" publishDir versionString
    let initialFileName = 
        if runtime = RunTime.Win then 
            sprintf "%s/%s.exe" outputFolder "arc"
        else 
            sprintf "%s/%s" outputFolder "arc"
    let newFileName = 
        if runtime = RunTime.Win then 
            //sprintf "%s/%s_%s.exe" outputFolder "arc" (runtime.GetRuntime())
            sprintf "%s/%s.exe" outputFolder "arc"
        else
            sprintf "%s/%s_%s" outputFolder "arc" (runtime.GetRuntime())
    publishBinaryInPath outputFolder version versionSuffix runtime  
    System.IO.File.Move(initialFileName, newFileName) |> ignore

let publishBinariesWin = BuildTask.create "PublishBinariesWin" [clean.IfNeeded; build.IfNeeded] {
    publishBinary stableVersionTag None RunTime.Win
}

let publishBinariesLinux = BuildTask.create "PublishBinariesLinux" [clean.IfNeeded; build.IfNeeded] {
    publishBinary stableVersionTag None RunTime.Linux
}

let publishBinariesMac = BuildTask.create "PublishBinariesMac" [clean.IfNeeded; build.IfNeeded] {
    publishBinary stableVersionTag None RunTime.Mac
}

let publishBinariesMacARM = BuildTask.create "PublishBinariesMacARM" [clean.IfNeeded; build.IfNeeded] {
    publishBinary stableVersionTag None RunTime.MacARM
}

let publishBinariesAll = BuildTask.createEmpty "PublishBinariesAll" [clean; build; publishBinariesWin; publishBinariesLinux; publishBinariesMac; publishBinariesMacARM]

let publishBinariesWinPrerelease = BuildTask.create "PublishBinariesWinPrerelease" [clean.IfNeeded; build.IfNeeded; setPrereleaseTag] {
    publishBinary stableVersionTag (Some prereleaseSuffix) RunTime.Win
}

let publishBinariesLinuxPrerelease = BuildTask.create "PublishBinariesLinuxPrerelease" [clean.IfNeeded; build.IfNeeded; setPrereleaseTag] {
    publishBinary stableVersionTag (Some prereleaseSuffix) RunTime.Linux
}

let publishBinariesMacPrerelease = BuildTask.create "PublishBinariesMacPrerelease" [clean.IfNeeded; build.IfNeeded; setPrereleaseTag] {
    publishBinary stableVersionTag (Some prereleaseSuffix) RunTime.Mac
}

let publishBinariesMacARMPrerelease = BuildTask.create "PublishBinariesMacARMPrerelease" [clean.IfNeeded; build.IfNeeded; setPrereleaseTag] {
    publishBinary stableVersionTag (Some prereleaseSuffix) RunTime.MacARM
}

let publishBinariesAllPrerelease = BuildTask.createEmpty "PublishBinariesAllPrerelease" [clean; build; setPrereleaseTag; publishBinariesWinPrerelease; publishBinariesLinuxPrerelease; publishBinariesMacPrerelease; publishBinariesMacARMPrerelease]

let publishBinariesFatPrerelease = BuildTask.create "PublishBinariesFatPrerelease" [clean; build; setPrereleaseTag; runTests] {
    publishBinaryForFat stableVersionTag (Some prereleaseSuffix) RunTime.Win
    publishBinaryForFat stableVersionTag (Some prereleaseSuffix) RunTime.Linux
    publishBinaryForFat stableVersionTag (Some prereleaseSuffix) RunTime.Mac
    publishBinaryForFat stableVersionTag (Some prereleaseSuffix) RunTime.MacARM
}

let publishBinariesFat = BuildTask.create "PublishBinariesFat" [clean; build; runTests] {
    publishBinaryForFat stableVersionTag None RunTime.Win
    publishBinaryForFat stableVersionTag None RunTime.Linux
    publishBinaryForFat stableVersionTag None RunTime.Mac
    publishBinaryForFat stableVersionTag None RunTime.MacARM
}

// as of now (july 2022), it seems there is now possibility to run lipo on Windows
//let packMacBinaries = BuildTask.create "PackMacBinaries" [publishBinariesMacBoth] {
//    let pr = new System.Diagnostics.Process()
//    pr.StartInfo.FileName <- "lipo"
//    pr.StartInfo.Arguments <- "-create -output ArcCommander ./"   // TO DO: add filepaths to both executables (see https://www.kenmuse.com/blog/notarizing-dotnet-console-apps-for-macos/ Chapter "Creating Universal binaries"
//    pr.Start() |> ignore
//}