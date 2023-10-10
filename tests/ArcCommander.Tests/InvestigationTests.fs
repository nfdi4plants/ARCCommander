module InvestigationTests

open Argu
open Expecto
open ARCtrl
open ARCtrl.ISA
open TestingUtils 
open ARCtrl.NET
open ArcCommander
open ArgumentProcessing
open ArcCommander.CLIArguments
open ArcCommander.APIs
open System
open System.IO


let setupArc (arcConfiguration : ArcConfiguration) =

    let arcArgs : ArcInitArgs list =  [] 

    processCommand arcConfiguration ArcAPI.init             arcArgs


let testInvestigationUpdate = 

    let testListName = "InvestigationUpdateTests"
    testList testListName [
        testCase "UpdateStandard" (fun () -> 
            let config = createConfigFromDir testListName "UpdateStandard"

            let investigationName = "TestInvestigation"
            let initArgs = [ArcInitArgs.Identifier investigationName]
            processCommand config ArcAPI.init initArgs

            let newIdentifier = "BestInvestigation"
            let newTitle = "newTitle"
            let newSubmissionDate = "newSubmissionDate"
            let investigationArgs = [InvestigationUpdateArgs.Identifier newIdentifier;InvestigationUpdateArgs.Title newTitle; InvestigationUpdateArgs.SubmissionDate newSubmissionDate]

            processCommand config InvestigationAPI.update investigationArgs

            let arc = ARC.load(config)
            let isa = Expect.wantSome arc.ISA "ISA was not created"

            Expect.equal isa.Identifier newIdentifier "Identifier was not set correctly"

            let title = Expect.wantSome isa.Title "Title was not set correctly"
            Expect.equal title newTitle "Title was not set correctly"

            let submissionDate = Expect.wantSome isa.SubmissionDate "SubmissionDate was not set correctly"
            Expect.equal submissionDate newSubmissionDate "SubmissionDate was not set correctly"
        )
        testCase "UpdateReplaceWithEmpty" (fun () -> 
            let config = createConfigFromDir testListName "UpdateStandard"

            let investigationName = "TestInvestigation"
            let initArgs = [ArcInitArgs.Identifier investigationName]
            processCommand config ArcAPI.init initArgs

            let newIdentifier = "BestInvestigation"
            let newTitle = "newTitle"
            let newSubmissionDate = "newSubmissionDate"
            let investigationArgs = [InvestigationUpdateArgs.Identifier newIdentifier;InvestigationUpdateArgs.Title newTitle; InvestigationUpdateArgs.SubmissionDate newSubmissionDate]

            processCommand config InvestigationAPI.update investigationArgs

            let newNewTitle = "Even more Title"

            let investigationArgs = [InvestigationUpdateArgs.Title newNewTitle; InvestigationUpdateArgs.ReplaceWithEmptyValues]

            processCommand config InvestigationAPI.update investigationArgs

            let arc = ARC.load(config)
            let isa = Expect.wantSome arc.ISA "ISA was not created"

            Expect.equal isa.Identifier newIdentifier "Identifier should not be overwritten by empty identifier, even if \"overwrite by empty\" is set."

            let title = Expect.wantSome isa.Title "Title should be overwritten a second time"
            Expect.equal title newNewTitle "Title was not set correctly"

            Expect.isNone isa.SubmissionDate "SubmissionDate should be overwritten by empty."
        )
        |> testSequenced
    ]

let testInvestigationContacts = 


    let testListName = "InvestigationContactTests"
    testList testListName [
        testCase "RegisterSimple" (fun () -> 

            let configuration = createConfigFromDir testListName "Register"
            setupArc configuration

            let personFirstName = "Testy"
            let personLastName = "McTestface"
            let personOrcid = "0000-0000-0000-0000"

            let personRegisterArgs : InvestigationContacts.PersonRegisterArgs list = [
                InvestigationContacts.PersonRegisterArgs.FirstName personFirstName
                InvestigationContacts.PersonRegisterArgs.LastName personLastName
                InvestigationContacts.PersonRegisterArgs.ORCID personOrcid
            ]
            let testPerson = Person.create(ORCID = personOrcid, FirstName = personFirstName, LastName = personLastName)

            processCommand configuration InvestigationAPI.Contacts.register personRegisterArgs

            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"

            Expect.equal isa.Contacts.Length 1 "Person was not added to assay"
            Expect.equal isa.Contacts.[0] testPerson "Person was not correctly added to assay"
        )

        testCase "RegisterSecond" (fun () ->
            
            let configuration = createConfigFromDir testListName "RegisterSecond"
            setupArc configuration

            let personFirstName = "Testy"
            let personLastName = "McTestface"
            let personOrcid = "0000-0000-0000-0000"

            let secondPersonFirstName = "Testy2"
            let secondPersonLastName = "McTestface2"
            let secondPersonOrcid = "0000-0000-0000-0001"

            let personRegisterArgs : InvestigationContacts.PersonRegisterArgs list = [
                InvestigationContacts.PersonRegisterArgs.FirstName personFirstName
                InvestigationContacts.PersonRegisterArgs.LastName personLastName
                InvestigationContacts.PersonRegisterArgs.ORCID personOrcid
            ]
            let secondPersonRegisterArgs : InvestigationContacts.PersonRegisterArgs list = [
                InvestigationContacts.PersonRegisterArgs.FirstName secondPersonFirstName
                InvestigationContacts.PersonRegisterArgs.LastName secondPersonLastName
                InvestigationContacts.PersonRegisterArgs.ORCID secondPersonOrcid                
            ]

            let testPerson = Person.create(ORCID = secondPersonOrcid, FirstName = secondPersonFirstName, LastName = secondPersonLastName)

            processCommand configuration InvestigationAPI.Contacts.register personRegisterArgs
            processCommand configuration InvestigationAPI.Contacts.register secondPersonRegisterArgs

            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"

            Expect.equal isa.Contacts.Length 2 "Person was not added to assay"
            Expect.equal isa.Contacts.[1] testPerson "Person was not correctly added to assay"       
        
        )
        testCase "DontRegisterEqual" (fun () ->
            
            let configuration = createConfigFromDir "AssayPerformerTests" "DontRegisterEqual"
            setupArc configuration

            let personFirstName = "Testy"
            let personLastName = "McTestface"
            let personOrcid = "0000-0000-0000-0000"

            let secondPersonFirstName = "Testy2"
            let secondPersonLastName = "McTestface2"
            let secondPersonOrcid = "0000-0000-0000-0001"

            let personRegisterArgs : InvestigationContacts.PersonRegisterArgs list = [
                InvestigationContacts.PersonRegisterArgs.FirstName personFirstName
                InvestigationContacts.PersonRegisterArgs.LastName personLastName
                InvestigationContacts.PersonRegisterArgs.ORCID personOrcid
            ]
            let secondPersonRegisterArgs : InvestigationContacts.PersonRegisterArgs list = [
                InvestigationContacts.PersonRegisterArgs.FirstName secondPersonFirstName
                InvestigationContacts.PersonRegisterArgs.LastName secondPersonLastName
                InvestigationContacts.PersonRegisterArgs.ORCID secondPersonOrcid                
            ]

            let testPerson = Person.create(ORCID = secondPersonOrcid, FirstName = secondPersonFirstName, LastName = secondPersonLastName)

            processCommand configuration InvestigationAPI.Contacts.register personRegisterArgs
            processCommand configuration InvestigationAPI.Contacts.register secondPersonRegisterArgs
            processCommand configuration InvestigationAPI.Contacts.register secondPersonRegisterArgs


            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"

            Expect.equal isa.Contacts.Length 2 "Identical person was added to assay"
            Expect.equal isa.Contacts.[1] testPerson "Person was modified"       
        
        )
        testCase "Update" (fun () ->
            
            let configuration = createConfigFromDir "AssayPerformerTests" "Update"
            setupArc configuration

            let personFirstName = "Testy"
            let personLastName = "McTestface"
            let personOrcid = "0000-0000-0000-0000"

            let secondPersonFirstName = "Testy2"
            let secondPersonLastName = "McTestface2"
            let secondPersonOrcid = "0000-0000-0000-0001"

            let secondPersonNewOrcid = "0000-0000-0000-0002"
            let secondPersonNewEmail = "testy2@testmail.com"

            let personRegisterArgs : InvestigationContacts.PersonRegisterArgs list = [
                InvestigationContacts.PersonRegisterArgs.FirstName personFirstName
                InvestigationContacts.PersonRegisterArgs.LastName personLastName
                InvestigationContacts.PersonRegisterArgs.ORCID personOrcid
            ]
            let secondPersonRegisterArgs : InvestigationContacts.PersonRegisterArgs list = [
                InvestigationContacts.PersonRegisterArgs.FirstName secondPersonFirstName
                InvestigationContacts.PersonRegisterArgs.LastName secondPersonLastName
                InvestigationContacts.PersonRegisterArgs.ORCID secondPersonOrcid                
            ]

            let secondPersonUpdateArgs : InvestigationContacts.PersonUpdateArgs list = [
                InvestigationContacts.PersonUpdateArgs.FirstName secondPersonFirstName
                InvestigationContacts.PersonUpdateArgs.LastName secondPersonLastName
                InvestigationContacts.PersonUpdateArgs.ORCID secondPersonNewOrcid
                InvestigationContacts.PersonUpdateArgs.Email secondPersonNewEmail
            ]

            let testPerson1 = Person.create(ORCID = personOrcid, FirstName = personFirstName, LastName = personLastName)
            let testPerson2 = Person.create(ORCID = secondPersonNewOrcid, FirstName = secondPersonFirstName, LastName = secondPersonLastName, Email = secondPersonNewEmail)

            processCommand configuration InvestigationAPI.Contacts.register personRegisterArgs
            processCommand configuration InvestigationAPI.Contacts.register secondPersonRegisterArgs
            processCommand configuration InvestigationAPI.Contacts.register secondPersonRegisterArgs
            processCommand configuration InvestigationAPI.Contacts.update secondPersonUpdateArgs

            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"

            Expect.equal isa.Contacts.Length 2 "Identical person was added to assay"
            Expect.equal isa.Contacts.[0] testPerson1 "Person was modified"       
            Expect.equal isa.Contacts.[1] testPerson2 "Person was not correctly updated"   
        )

    ]
    |> testSequenced

// currently not valid anymore since Expecto does not trigger NLog's console loggings. Did not find any workarounds
//[<Tests>]
//let testInvestigationShow =
    
//    let setupArc (arcConfiguration : ArcConfiguration) = processCommandWoArgs arcConfiguration ArcAPI.init (Map [])
    
//    let createConfigFromDir testListName testCaseName =
//        let dir = Path.Combine(__SOURCE_DIRECTORY__, "TestResult", testListName, testCaseName)
//        ArcConfiguration.GetDefault()
//        |> ArcConfiguration.ofIniData
//        |> fun c -> {c with General = (Map.ofList ["workdir", dir; "verbosity", "2"]) }

//    testList "InvestigationShowTests" [
//        testCase "ShowsCorrectly" (fun () -> 
   
//            let configuration = createConfigFromDir "InvestigationShowTests" "ShowsCorrectly"
//            setupArc configuration

//            let investigationName = "TestInvestigation"
//            let submissionDate = "FirstOctillember"
//            let investigationArgs = [InvestigationCreateArgs.Identifier investigationName; InvestigationCreateArgs.SubmissionDate submissionDate]
//            processCommand configuration InvestigationAPI.create investigationArgs

//            let writer = new StringWriter()
//            let stdout = Console.Out // standard Console output
//            Console.SetOut(writer) // reads everything that the console prints

//            processCommandWoArgs configuration InvestigationAPI.show

//            let consoleOutput = writer.ToString().Replace("\013","") // get rid of stupid carriage return char

//            Console.SetOut(stdout) // reset Console output to stdout

//            let expectedOutput = 
//                "Start Investigation Show\nInvestigation Identifier:TestInvestigation\nInvestigation Title:\nInvestigation Description:\nInvestigation Submission Date:FirstOctillember\nInvestigation Public Release Date:\n"

//            Expect.equal consoleOutput expectedOutput "The showed output differed from the expected output"

//        )

//    ]
//    |> testSequenced


[<Tests>]
let investigationTests = 
    testList "Investigation" [
        testInvestigationUpdate
        testInvestigationContacts
    ]
