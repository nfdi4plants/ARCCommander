module ConfigurationTests

open Expecto
open TestingUtils

[<PTests>]
let testConfiguration = 

    let testDirectory = __SOURCE_DIRECTORY__ + @"/TestFiles/"
    let referenceInvestigationFilePath = System.IO.Path.Combine(testDirectory,"isa.investigation.xlsx")
    let outputInvestigationFilePath = System.IO.Path.Combine(testDirectory,"new.isa.investigation.xlsx")

    testCase "Empty" (fun () -> 
        Expect.isTrue true ""
    )
