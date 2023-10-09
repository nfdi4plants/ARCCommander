module ArcTests

open Argu
open Expecto
open TestingUtils
open ARCtrl
open ARCtrl.ISA
open ARCtrl.ISA.Spreadsheet
open ARCtrl.NET
open ArcCommander
open ArcCommander.CLIArguments
open ArcCommander.APIs
open System
open System.IO

let setupArc (arcConfiguration : ArcConfiguration) =

    let arcArgs : ArcInitArgs list =  [] 

    processCommand arcConfiguration ArcAPI.init             arcArgs


let testArcInit = 

    let testListName = "ArcInit"
    testList testListName [
        testCase "Simple" (fun () -> 
            let config = createConfigFromDir testListName "Simple"
            let identifier = "MyInvestigation"
            let investigationArgs = [ArcInitArgs.Identifier identifier]

            processCommand config ArcAPI.init investigationArgs

            let arc = ARC.load(config)
            let isa = Expect.wantSome arc.ISA "ISA was not created"

            Expect.equal isa.Identifier identifier "Identifier was not set correctly"
        )
        testCase "NoIdentifierGiven" (fun () -> 
            let workdirName = "NoIdentifierGiven"
            let config = createConfigFromDir testListName workdirName
            let investigationArgs : ArcInitArgs list = []

            processCommand config ArcAPI.init investigationArgs

            let arc = ARC.load(config)
            let isa = Expect.wantSome arc.ISA "ISA was not created"

            Expect.equal isa.Identifier workdirName "Identifier was not set correctly"
        )
        testCase "ShouldNotOverwrite" (fun () -> 
            let config = createConfigFromDir testListName "Simple"
            let identifier = "MyInvestigation"
            let investigationArgs = [ArcInitArgs.Identifier identifier]

            let secondIdentifier = "MySecondInvestigation"
            let secondInvestigationArgs = [ArcInitArgs.Identifier secondIdentifier]

            processCommand config ArcAPI.init investigationArgs
            processCommand config ArcAPI.init secondInvestigationArgs

            let arc = ARC.load(config)
            let isa = Expect.wantSome arc.ISA "ISA was not created"

            Expect.equal isa.Identifier identifier "Identifier was overwritten"
        )
        testCase "CreateGitIgnore" (fun () ->
            let config = createConfigFromDir testListName "CreateGitIgnore"
            let investigationArgs = [ArcInitArgs.Gitignore]
        
            processCommand config ArcAPI.init investigationArgs

            let gitignorePath = Path.Combine(GeneralConfiguration.getWorkDirectory config, ".gitignore")

            Expect.isTrue (System.IO.File.Exists gitignorePath) "Gitignore was not created"
        )
    ]
    |> testSequenced

[<Tests>]
let arcTests = 
    testList "ARC" [
        testArcInit
    ]
    |> testSequenced
