
module ArcCommander.Tests.NetCore

open Expecto

[<EntryPoint>]
let main argv =

  //ArcCommander core tests
  Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv IsaXLSXTests.testDataModelFunctions |> ignore
  Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv IsaXLSXTests.testInvestigationFileReading |> ignore
  Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv IsaXLSXTests.testInvestigationFileManipulations |> ignore
  0