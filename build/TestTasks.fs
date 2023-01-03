module TestTasks

open BlackFox.Fake

open ProjectInfo
open BasicTasks
open Helpers

let runTests = BuildTask.createFn "RunTests" [clean; cleanTestResults; build; copyBinaries] (fun config ->
    let isWatch = 
        config.Context.Arguments
        |> List.exists (fun x -> x.ToLower() = "watch")

    let singleRunTestsCommand = "run --project tests\ArcCommander.Tests.NetCore"
    let watchRunTestsCommand = "watch " + singleRunTestsCommand

    if isWatch then
        run dotnet watchRunTestsCommand ""
    else
        run dotnet singleRunTestsCommand ""
    //let standardParams = Fake.DotNet.MSBuild.CliArguments.Create ()
    //Fake.DotNet.DotNet.test(fun testParams ->
    //    {
    //        testParams with
    //            Logger = Some "console;verbosity=detailed"
    //            Configuration = DotNet.BuildConfiguration.fromString configuration
    //            NoBuild = true
    //    }
    //) testProject
)
