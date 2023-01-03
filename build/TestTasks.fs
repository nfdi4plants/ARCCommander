module TestTasks

open Fake.Core
open Fake.DotNet
open BlackFox.Fake

open ProjectInfo
open BasicTasks
open Helpers

let runTests = BuildTask.create "RunTests" [clean; cleanTestResults; build; copyBinaries] {
    run dotnet "watch run --project tests\ArcCommander.Tests.NetCore" ""
    //let standardParams = Fake.DotNet.MSBuild.CliArguments.Create ()
    //Fake.DotNet.DotNet.test(fun testParams ->
    //    {
    //        testParams with
    //            Logger = Some "console;verbosity=detailed"
    //            Configuration = DotNet.BuildConfiguration.fromString configuration
    //            NoBuild = true
    //    }
    //) testProject
}
