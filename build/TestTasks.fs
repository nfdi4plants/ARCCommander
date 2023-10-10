module TestTasks

open BlackFox.Fake

open Fake.DotNet
open ProjectInfo
open BasicTasks
open Helpers
open System.IO

let runTests = BuildTask.createFn "RunTests" [clean; cleanTestResults; build; copyBinaries] (fun config ->

    Fake.DotNet.DotNet.test(fun testParams ->
        {
            testParams with
                Logger = Some "console;verbosity=detailed"
                Configuration = DotNet.BuildConfiguration.fromString configuration
                NoBuild = true
        }
    ) testProject
)
