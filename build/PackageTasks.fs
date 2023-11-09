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


let publishBinaries (version : string) (versionSuffix : string Option) (runtime : RunTime) = 
    printfn "Published version %s and runtime %O" version runtime
    let outputFolder = runtime.GetRuntimeFolder()
    let outputPath = sprintf "%s/%s" publishDir outputFolder
    project
    |> DotNet.publish (fun p ->
        let standardParams = Fake.DotNet.MSBuild.CliArguments.Create ()
        {
            p with
                Runtime = Some (runtime.GetRuntime())
                Configuration = DotNet.BuildConfiguration.fromString configuration
                OutputPath = Some outputPath
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



let publishBinariesWin = BuildTask.create "PublishBinariesWin" [clean.IfNeeded; build.IfNeeded] {
    publishBinaries stableVersionTag None RunTime.Win
}

let publishBinariesLinux = BuildTask.create "PublishBinariesLinux" [clean.IfNeeded; build.IfNeeded] {
    publishBinaries stableVersionTag None RunTime.Linux
}

let publishBinariesMac = BuildTask.create "PublishBinariesMac" [clean.IfNeeded; build.IfNeeded] {
    publishBinaries stableVersionTag None RunTime.Mac
}

let publishBinariesMacARM = BuildTask.create "PublishBinariesMacARM" [clean.IfNeeded; build.IfNeeded] {
    publishBinaries stableVersionTag None RunTime.MacARM
}

let publishBinariesAll = BuildTask.createEmpty "PublishBinariesAll" [clean; build; publishBinariesWin; publishBinariesLinux; publishBinariesMac; publishBinariesMacARM]

let publishBinariesWinPrerelease = BuildTask.create "PublishBinariesWinPrerelease" [clean.IfNeeded; build.IfNeeded; setPrereleaseTag] {
    publishBinaries stableVersionTag (Some prereleaseSuffix) RunTime.Win
}

let publishBinariesLinuxPrerelease = BuildTask.create "PublishBinariesLinuxPrerelease" [clean.IfNeeded; build.IfNeeded; setPrereleaseTag] {
    publishBinaries stableVersionTag (Some prereleaseSuffix) RunTime.Linux
}

let publishBinariesMacPrerelease = BuildTask.create "PublishBinariesMacPrerelease" [clean.IfNeeded; build.IfNeeded; setPrereleaseTag] {
    publishBinaries stableVersionTag (Some prereleaseSuffix) RunTime.Mac
}

let publishBinariesMacARMPrerelease = BuildTask.create "PublishBinariesMacARMPrerelease" [clean.IfNeeded; build.IfNeeded; setPrereleaseTag] {
    publishBinaries stableVersionTag (Some prereleaseSuffix) RunTime.MacARM
}

let publishBinariesAllPrerelease = BuildTask.createEmpty "PublishBinariesAllPrerelease" [clean; build; setPrereleaseTag; publishBinariesWinPrerelease; publishBinariesLinuxPrerelease; publishBinariesMacPrerelease; publishBinariesMacARMPrerelease]



// as of now (july 2022), it seems there is now possibility to run lipo on Windows
//let packMacBinaries = BuildTask.create "PackMacBinaries" [publishBinariesMacBoth] {
//    let pr = new System.Diagnostics.Process()
//    pr.StartInfo.FileName <- "lipo"
//    pr.StartInfo.Arguments <- "-create -output ArcCommander ./"   // TO DO: add filepaths to both executables (see https://www.kenmuse.com/blog/notarizing-dotnet-console-apps-for-macos/ Chapter "Creating Universal binaries"
//    pr.Start() |> ignore
//}