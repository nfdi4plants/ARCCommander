module ConfigurationTests

open Expecto
open TestingUtils
open Fake.IO
open Fake.IO.Globbing.Operators

  
let arguments =  
    testList "ArgumentHandling" [
        testCase "Structure" (fun () ->
            Argu.ArgumentParser<ArcCommander.Commands.ArcCommand>.CheckStructure()
        )   
    
    ]

[<Tests>]
let configuration =
    testList "Configuration" [
        arguments
    ]