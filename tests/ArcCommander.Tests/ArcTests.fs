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

let testArcInit = 

    let testListName = "ArcInit"
    testList testListName [
        testCase "Simple" (fun () -> 
            let config = createConfigFromDir testListName "Simple"
            let identifier = "MyInvestigation"
            let investigationArgs = [ArcInitArgs.InvestigationIdentifier identifier]

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
            let investigationArgs = [ArcInitArgs.InvestigationIdentifier identifier]

            let secondIdentifier = "MySecondInvestigation"
            let secondInvestigationArgs = [ArcInitArgs.InvestigationIdentifier secondIdentifier]

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

let testArcUpdate = 

    let testListName = "ArcUpdate"
    testList testListName [
        testCase "DontPutAssayPerformerIntoStudy" (fun () -> 
            let config = createConfigFromDir testListName "DontPutAssayPerformerIntoStudy"

            let identifier = "MyInvestigation"
            let investigationArgs = [ArcInitArgs.InvestigationIdentifier identifier]
            processCommand config ArcAPI.init investigationArgs

            let assayIdentifier = "MyAssay"
            let assayArgs = [AssayAddArgs.AssayIdentifier assayIdentifier]
            processCommand config AssayAPI.add assayArgs

            let personFirstName = "John"
            let personLastName = "Doe"
            let expectedPerson = Person.create(FirstName = personFirstName, LastName = personLastName)
            let personArgs = [AssayContacts.PersonRegisterArgs.AssayIdentifier assayIdentifier; AssayContacts.PersonRegisterArgs.FirstName personFirstName; AssayContacts.PersonRegisterArgs.LastName personLastName]

            processCommand config AssayAPI.Contacts.register personArgs

            let arc = ARC.load(config)
            let isa = Expect.wantSome arc.ISA "ISA was not created"
            let study = Expect.wantSome (isa.TryGetStudy assayIdentifier) "Study was not created"
            Expect.equal study.Contacts.Length 0 "Study should not have any contacts"
            let assay = Expect.wantSome (study.TryGetRegisteredAssay assayIdentifier) "Assay was not created"
            Expect.equal assay.Performers.Length 1 "Assay should have one contact"
            Expect.equal assay.Performers[0] expectedPerson "Assay contact was not set correctly"
        )
       
    ]
    |> testSequenced

let testArcExport = 

    let testListName = "ArcExport"
    testList testListName [
        testCase "Simple" (fun () -> 
            let config = createConfigFromDir testListName "Simple"

            let identifier = "MyInvestigation"
            let investigationArgs = [ArcInitArgs.InvestigationIdentifier identifier]
            processCommand config ArcAPI.init investigationArgs

            let assayIdentifier = "MyAssay"
            let assayArgs = [AssayAddArgs.AssayIdentifier assayIdentifier]
            processCommand config AssayAPI.add assayArgs

            let exportPath = Path.Combine(GeneralConfiguration.getWorkDirectory config, "export.json")
            let exportArgs = [ArcExportArgs.Output exportPath]    
            processCommand config ArcAPI.export exportArgs


            Expect.isTrue (System.IO.File.Exists exportPath) "Export was not created"

            let isa = Json.ArcInvestigation.fromJsonString (System.IO.File.ReadAllText exportPath)

            Expect.equal isa.Identifier identifier "Identifier was not set correctly in exported json"
            let study = Expect.wantSome (isa.TryGetStudy assayIdentifier) "Study was not exported"
            Expect.sequenceEqual study.RegisteredAssayIdentifiers [assayIdentifier] "Assay was not exported"

        )
        testCase "OnlyExportRegistered" (fun () ->
            let config = createConfigFromDir testListName "OnlyExportRegistered"

            let identifier = "MyInvestigation"
            let investigationArgs = [ArcInitArgs.InvestigationIdentifier identifier]
            processCommand config ArcAPI.init investigationArgs

            let registeredAssayIdentifier =  "RegisteredAssay"
            let assayArgs = [AssayAddArgs.AssayIdentifier registeredAssayIdentifier]
            processCommand config AssayAPI.add assayArgs

            let unregisteredAssayIdentifier = "UnregisteredAssay"
            let assayArgs = [AssayInitArgs.AssayIdentifier unregisteredAssayIdentifier]
            processCommand config AssayAPI.init assayArgs

            let unregisteredStudyIdentifier = "UnregisteredStudy"
            let studyArgs = [StudyInitArgs.StudyIdentifier unregisteredStudyIdentifier]
            processCommand config StudyAPI.init studyArgs

            let exportPath = Path.Combine(GeneralConfiguration.getWorkDirectory config, "export.json")
            let exportArgs = [ArcExportArgs.Output exportPath]
            processCommand config ArcAPI.export exportArgs

            Expect.isTrue (System.IO.File.Exists exportPath) "Export was not created"

            let isa = Json.ArcInvestigation.fromJsonString (System.IO.File.ReadAllText exportPath)

            Expect.equal isa.Identifier identifier "Identifier was not set correctly in exported json"
            Expect.equal isa.Studies.Count 1 "Only one study should be exported"
            let study = Expect.wantSome (isa.TryGetStudy registeredAssayIdentifier) "Study was not exported"

            Expect.equal isa.AssayCount 1 "Only one assay should be exported"
            Expect.sequenceEqual study.RegisteredAssayIdentifiers [registeredAssayIdentifier] "Assay was not exported"            
        )
    ]
    |> testSequenced


[<Tests>]
let arcTests = 
    testList "ARC" [
        testArcInit
        testArcUpdate
        testArcExport
    ]
    |> testSequenced
