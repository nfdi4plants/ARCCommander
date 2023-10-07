
module ArcCommander.Tests.NetCore

open Expecto

[<EntryPoint>]
let main argv =

  //ArcCommander core tests
  Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv AssayTests.testAssayTestFunction |> ignore
  Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv AssayTests.testAssayRegister |> ignore
  Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv AssayTests.testAssayUpdate |> ignore
  Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv AssayTests.testAssayUnregister |> ignore
  Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv AssayTests.testAssayMove |> ignore

  //Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv StudyTests.testStudyRegister |> ignore
  //Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv StudyTests.testStudyProtocolLoad |> ignore
  //Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv StudyTests.testStudyContacts |> ignore


  //Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv InvestigationTests.testInvestigationReading |> ignore
  //Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv InvestigationTests.testInvestigationCreate |> ignore
  //Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv InvestigationTests.testInvestigationUpdate |> ignore

  //Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv InvestigationTests.testInvestigationContacts |> ignore


  //Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv IsaXLSXTests.testInvestigationFileReading |> ignore
  //Tests.runTestsWithCLIArgs [Tests.CLIArguments.Sequenced] argv IsaXLSXTests.testInvestigationFileManipulations |> ignore
  0