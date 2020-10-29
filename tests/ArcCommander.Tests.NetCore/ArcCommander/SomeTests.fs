module SomeTests

open ArcCommander
open FSharpSpreadsheetML
open ISA_XLSX
open Expecto


[<Tests>]
let testStuff = 
    testList "AminoAcids" [
        testCase "example" (fun () -> 
            let testSymbols = ['a','b','c']
            Expect.sequenceEqual
                testSymbols
                ['a','b','c']
                "wrooong"
        )
    ]