module ToolTasks

open System.IO
open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.Globbing.Operators
open BlackFox.Fake

open ProjectInfo
open Helpers

open ProjectInfo
open BasicTasks
open TestTasks
open PackageTasks

let installPackagedTool = BuildTask.create "InstallPackagedTool" [packPrerelease] {
    Directory.ensure "tests/tool-tests"
    run dotnet "new tool-manifest --force" "tests/tool-tests"
    run dotnet (sprintf "tool install --add-source ../../%s ArcCommander --version %s" pkgDir prereleaseTag) "tests/tool-tests"
}

let testPackagedTool = BuildTask.create "TestPackagedTool" [installPackagedTool] {
    run dotnet "ArcCommander --help" "tests/tool-tests"
}