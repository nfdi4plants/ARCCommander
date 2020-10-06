#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target 
nuget Fake.Core.ReleaseNotes
nuget Fake.Dotnet.Paket //"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

Target.initEnvironment ()

let release = ReleaseNotes.load "RELEASE_NOTES.md"

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ "temp"
    |> Shell.cleanDirs 
)

Target.create "Build" (fun _ ->
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.build id)
)

//Use Dotnet.pack if you dont use paket
Target.create "Pack" (fun _ ->
    Paket.pack(fun p -> 
        { p with
            ToolType = ToolType.CreateLocalTool()
            OutputPath = "temp"
            Version = release.NugetVersion
            ReleaseNotes = release.Notes |> String.toLines })
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "All"
  
"All"
  ==> "Pack"

Target.runOrDefaultWithArguments "All"