module ToolTasks

open Fake.IO
open BlackFox.Fake

open ProjectInfo
open Helpers
open PackageTasks

//let installPackagedTool = BuildTask.create "InstallPackagedTool" [packPrerelease] {
//    Directory.ensure "tests/tool-tests"
//    run dotnet "new tool-manifest --force" "tests/tool-tests"
//    run dotnet (sprintf "tool install --add-source ../../%s ArcCommander --version %s" pkgDir prereleaseTag) "tests/tool-tests"
//}

//let testPackagedTool = BuildTask.create "TestPackagedTool" [installPackagedTool] {
//    run dotnet "ArcCommander --help" "tests/tool-tests"
//}