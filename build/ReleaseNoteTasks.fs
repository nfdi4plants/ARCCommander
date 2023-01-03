module ReleaseNoteTasks

open System.IO
open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.Globbing.Operators
open BlackFox.Fake
open Fake.Tools

open ProjectInfo
open Helpers

open ProjectInfo
open BasicTasks
open TestTasks
open PackageTasks
open Fake.Extensions.Release

let updateVersionOfReleaseWorkflow (stableVersionTag) = 
    printfn "Start updating release-github workflow to version %s" stableVersionTag
    let filePath = Path.getFullName ".github/workflows/release-github.yml"
    let s = File.readAsString filePath
    let lastVersion = System.Text.RegularExpressions.Regex.Match(s,@"v\d+.\d+.\d+").Value
    s.Replace(lastVersion,$"v{stableVersionTag}")
    |> File.writeString false filePath

let updateVersionOfReadme (stableVersionTag) = 
    printfn "Start updating readme to version %s" stableVersionTag
    let filePath = Path.getFullName "README.md"
    let s = File.readAsString filePath
    let lastVersion = System.Text.RegularExpressions.Regex.Match(s,@"v\d+.\d+.\d+").Value
    s.Replace(lastVersion,$"v{stableVersionTag}")
    |> File.writeString false filePath


let updateReleaseNotes = BuildTask.createFn "ReleaseNotes" [] (fun config ->
    ReleaseNotes.ensure()

    ReleaseNotes.update(ProjectInfo.gitOwner, ProjectInfo.gitName, config)

    let release = ReleaseNotes.load "RELEASE_NOTES.md"

    Fake.DotNet.AssemblyInfoFile.createFSharp  "src/arcCommander/Server/Version.fs"
        [   Fake.DotNet.AssemblyInfo.Title "SWATE"
            Fake.DotNet.AssemblyInfo.Version release.AssemblyVersion
            Fake.DotNet.AssemblyInfo.Metadata ("ReleaseDate", release.Date |> Option.defaultValue System.DateTime.Today |> fun d -> d.ToShortDateString())
        ]

    let stableVersion = SemVer.parse release.NugetVersion

    let stableVersionTag = (sprintf "%i.%i.%i" stableVersion.Major stableVersion.Minor stableVersion.Patch )

    updateVersionOfReleaseWorkflow (stableVersionTag)
    updateVersionOfReadme (stableVersionTag)
)

let githubDraft = BuildTask.createFn "GithubDraft" [] (fun config ->

    let body = "We are ready to go for the first release!"

    Github.draft(
        ProjectInfo.gitOwner,
        ProjectInfo.gitName,
        (Some body),
        None,
        config
    )
)
