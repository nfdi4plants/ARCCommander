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

let pack = BuildTask.create "Pack" [clean; build; runTests; copyBinaries] {
    if promptYesNo (sprintf "creating stable package with version %s OK?" stableVersionTag ) 
        then
            !! "src/**/*.*proj"
            |> Seq.iter (Fake.DotNet.DotNet.pack (fun p ->
                let msBuildParams =
                    {p.MSBuildParams with 
                        Properties = ([
                            "Version",stableVersionTag
                            "PackageReleaseNotes",  (release.Notes |> String.concat "\r\n")
                        ] @ p.MSBuildParams.Properties)
                    }
                {
                    p with 
                        MSBuildParams = msBuildParams
                        OutputPath = Some pkgDir
                        NoBuild = true
                        Configuration = DotNet.BuildConfiguration.fromString configuration
                }
            ))
    else failwith "aborted"
}

let packPrerelease = BuildTask.create "PackPrerelease" [setPrereleaseTag; clean; build; runTests; copyBinaries] {
    if promptYesNo (sprintf "package tag will be %s OK?" prereleaseTag )
        then 
            !! "src/**/*.*proj"
            //-- "src/**/Plotly.NET.Interactive.fsproj"
            |> Seq.iter (Fake.DotNet.DotNet.pack (fun p ->
                        let msBuildParams =
                            {p.MSBuildParams with 
                                Properties = ([
                                    "Version", prereleaseTag
                                    "PackageReleaseNotes",  (release.Notes |> String.toLines )
                                ] @ p.MSBuildParams.Properties)
                            }
                        {
                            p with 
                                VersionSuffix = Some prereleaseSuffix
                                OutputPath = Some pkgDir
                                NoBuild = true
                                Configuration = DotNet.BuildConfiguration.fromString configuration
                        }
            ))
    else
        failwith "aborted"
}

let publishBinariesWin = BuildTask.create "PublishBinariesWin" [clean.IfNeeded; build.IfNeeded] {
    let outputPath = sprintf "%s/win-x64" publishDir
    solutionFile
    |> DotNet.publish (fun p ->
        let standardParams = Fake.DotNet.MSBuild.CliArguments.Create ()
        {
            p with
                Runtime = Some "win-x64"
                Configuration = DotNet.BuildConfiguration.fromString configuration
                OutputPath = Some outputPath
                MSBuildParams = {
                    standardParams with
                        Properties = [
                            "Version", stableVersionTag
                            "Platform", "x64"
                            "PublishSingleFile", "true"
                        ]
                };
        }
    )
    printfn "Beware that assemblyName differs from projectName!"
}

let publishBinariesLinux = BuildTask.create "PublishBinariesLinux" [clean.IfNeeded; build.IfNeeded] {
    let outputPath = sprintf "%s/linux-x64" publishDir
    solutionFile
    |> DotNet.publish (fun p ->
        let standardParams = Fake.DotNet.MSBuild.CliArguments.Create ()
        {
            p with
                Runtime = Some "linux-x64"
                Configuration = DotNet.BuildConfiguration.fromString configuration
                OutputPath = Some outputPath
                MSBuildParams = {
                    standardParams with
                        Properties = [
                            "Version", stableVersionTag
                            "Platform", "x64"
                            "PublishSingleFile", "true"
                        ]
                }
        }
    )
    printfn "Beware that assemblyName differs from projectName!"
}

let publishBinariesMac = BuildTask.create "PublishBinariesMac" [clean.IfNeeded; build.IfNeeded] {
    let outputPath = sprintf "%s/osx-x64" publishDir
    solutionFile
    |> DotNet.publish (fun p ->
        let standardParams = Fake.DotNet.MSBuild.CliArguments.Create ()
        {
            p with
                Runtime = Some "osx-x64"
                Configuration = DotNet.BuildConfiguration.fromString configuration
                OutputPath = Some outputPath
                MSBuildParams = {
                    standardParams with
                        Properties = [
                            "Version", stableVersionTag
                            "Platform", "x64"
                            "PublishSingleFile", "true"
                        ]
                }
        }
    )
    printfn "Beware that assemblyName differs from projectName!"
}

let publishBinariesMacARM = BuildTask.create "PublishBinariesMacARM" [clean.IfNeeded; build.IfNeeded] {
    let outputPath = sprintf "%s/osx-arm64" publishDir
    solutionFile
    |> DotNet.publish (fun p ->
        let standardParams = Fake.DotNet.MSBuild.CliArguments.Create ()
        {
            p with
                Runtime = Some "osx.12-arm64"
                Configuration = DotNet.BuildConfiguration.fromString configuration
                OutputPath = Some outputPath
                MSBuildParams = {
                    standardParams with
                        Properties = [
                            "Version", stableVersionTag
                            //"Platform", "arm64"   // throws MSBuild Error although it should work: error MSB4126: The specified solution configuration "Release|ARM64" is invalid. Please specify a valid solution configuration using the Configuration and Platform properties (e.g. MSBuild.exe Solution.sln /p:Configuration=Debug /p:Platform="Any CPU") or leave those properties blank to use the default solution configuration. [C:\Repos\omaus\arcCommander\ArcCommander.sln]
                            "PublishSingleFile", "true"
                        ]
                }
        }
    )
    printfn "Beware that assemblyName differs from projectName!"
}

let publishBinariesAll = BuildTask.createEmpty "PublishBinariesAll" [clean; build; publishBinariesWin; publishBinariesLinux; publishBinariesMac]

let publishBinariesMacBoth = BuildTask.createEmpty "PublishBinariesMacBoth" [clean; build; publishBinariesMac; publishBinariesMacARM]

// as of now (july 2022), it seems there is now possibility to run lipo on Windows
//let packMacBinaries = BuildTask.create "PackMacBinaries" [publishBinariesMacBoth] {
//    let pr = new System.Diagnostics.Process()
//    pr.StartInfo.FileName <- "lipo"
//    pr.StartInfo.Arguments <- "-create -output ArcCommander ./"   // TO DO: add filepaths to both executables (see https://www.kenmuse.com/blog/notarizing-dotnet-console-apps-for-macos/ Chapter "Creating Universal binaries"
//    pr.Start() |> ignore
//}