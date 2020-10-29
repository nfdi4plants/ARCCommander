module ArcCommander.Tests.NetCore

open Expecto

[<EntryPoint>]
let main argv =

    //ArcCommander core tests
    Tests.runTestsWithCLIArgs [] argv SomeTests.testStuff         |> ignore
    0