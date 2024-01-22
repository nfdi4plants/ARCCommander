﻿module StudyTests

open Argu
open Expecto
open TestingUtils
open ARCtrl
open ARCtrl.ISA
open ARCtrl.ISA.Spreadsheet
open ARCtrl.NET
open ArcCommander
open ArgumentProcessing
open ArcCommander.CLIArguments
open ArcCommander.APIs

let setupArc (arcConfiguration:ArcConfiguration) =
    let arcArgs : ArcInitArgs list =  [ArcInitArgs.InvestigationIdentifier "TestInvestigation"] 

    processCommand arcConfiguration ArcAPI.init             arcArgs

let testStudyInit = 
    
    let testListName = "StudyInitTests"

    testList testListName [
        testCase "DoesNotRegister" (fun () ->
            let config = createConfigFromDir testListName "DoesNotRegister"

            setupArc config

            let studyIdentifier = "TestStudy"
            let studyArgs = [StudyInitArgs.StudyIdentifier studyIdentifier]
            processCommand config StudyAPI.init studyArgs

            let arc = ARC.load(config)
            let isa = Expect.wantSome arc.ISA "ISA was not created"
            Expect.equal isa.Studies.Count 1 "Study was not initialized in ARC"
            Expect.equal isa.RegisteredStudies.Count 0 "Study was registered to ISA, even though it should not have been"   
        )
    
    
    ]

let testStudyAdd =
    
    let testListName = "StudyAddTests"


    testList testListName [
        testCase "AddToEmptyInvestigation" (fun () -> 

            let config = createConfigFromDir testListName "AddToEmptyInvestigation"

            setupArc config

            let studyIdentifier = "TestStudy"
            let studyDescription = "TestStudyDescription"

            let studyArgs = [StudyAddArgs.StudyIdentifier studyIdentifier;StudyAddArgs.Description studyDescription]
            processCommand config StudyAPI.add studyArgs
            let arc = ARC.load(config)
            let isa = Expect.wantSome arc.ISA "ISA was not created" 
            Expect.equal isa.Studies.Count 1 "Study was not initialized in ARC"
            Expect.equal isa.RegisteredStudies.Count 1 "Study was not registetered to ISA"
            Expect.equal isa.RegisteredStudies.[0].Identifier studyIdentifier "Study was not registetered to ISA with correct identifier"
            let description = Expect.wantSome isa.RegisteredStudies.[0].Description "Study was not registetered to ISA with correct description"
            Expect.equal description studyDescription "Study was not registetered to ISA with correct description"
        )
        testCase "AddSecondStudy" (fun () -> 

            let config = createConfigFromDir testListName "AddSecondStudy"

            setupArc config

            let studyIdentifier = "TestStudy"
            let studyDescription = "TestStudyDescription"

            let studyArgs = [StudyAddArgs.StudyIdentifier studyIdentifier;StudyAddArgs.Description studyDescription]
            processCommand config StudyAPI.add studyArgs

            let studyIdentifier = "TestStudy2"
            let studyDescription = "TestStudyDescription2"

            let studyArgs = [StudyAddArgs.StudyIdentifier studyIdentifier;StudyAddArgs.Description studyDescription]
            processCommand config StudyAPI.add studyArgs
            let arc = ARC.load(config)
            let isa = Expect.wantSome arc.ISA "ISA was not created"
            Expect.equal isa.Studies.Count 2 "Second study was not added to ISA"
            Expect.equal isa.RegisteredStudies.Count 2 "Second study was not registetered to ISA"
            Expect.equal isa.RegisteredStudies.[1].Identifier studyIdentifier "Second study was not registetered to ISA with correct identifier"
            let description = Expect.wantSome isa.RegisteredStudies.[1].Description "Second study was not registetered to ISA with correct description"
            Expect.equal description studyDescription "Second study was not registetered to ISA with correct description"
        )
        testCase "DoesntAddDuplicateStudy" (fun () -> 

            let config = createConfigFromDir testListName "DoesntAddDuplicateStudy"

            setupArc config

            let studyIdentifier = "TestStudy"
            let studyDescription = "TestStudyDescription"

            let studyArgs = [StudyAddArgs.StudyIdentifier studyIdentifier;StudyAddArgs.Description studyDescription]
            processCommand config StudyAPI.add studyArgs

            let studyIdentifier = "TestStudy2"
            let studyDescription = "TestStudyDescription2"

            let studyArgs = [StudyAddArgs.StudyIdentifier studyIdentifier;StudyAddArgs.Description studyDescription]
            processCommand config StudyAPI.add studyArgs

            let studyIdentifier = "TestStudy2"
            let studyDescription = "TestStudyDescription2"

            let studyArgs = [StudyAddArgs.StudyIdentifier studyIdentifier;StudyAddArgs.Description studyDescription]
            Expect.throws (fun () -> processCommand config StudyAPI.add studyArgs) "Study was added to ISA, even though it already existed"
            let arc = ARC.load(config)
            let isa = Expect.wantSome arc.ISA "ISA was not created"
            Expect.equal isa.Studies.Count 2 "Second study was added to ISA, even though it already existed"
            Expect.equal isa.RegisteredStudies.Count 2 "Second study was added to ISA, even though it already existed"
        )
    ]
    |> testSequenced

//[<Tests>]
//let testStudyProtocolLoad = 

//    let protocolTestFile = __SOURCE_DIRECTORY__ + @"/TestFiles/ProtocolTestFile.json"
//    let processTestFile = __SOURCE_DIRECTORY__ + @"/TestFiles/ProcessTestFile.json"

//    let testDirectory = __SOURCE_DIRECTORY__ + @"/TestResult/studyProtocolLoadTest"

//    let configuration = 
//        ArcConfiguration.create 
//            (Map.ofList ["workdir", testDirectory; "verbosity", "2"; "editor", "Decoy"]) 
//            standardISAArgs
//            Map.empty Map.empty Map.empty Map.empty

    //testList "StudyProtocolLoadTests" [

    //    testCase "AddFromProtocolFile" (fun () -> 

    //        let studyIdentifier = "ProtocolStudy"
            
    //        let studyArgs = [StudyRegisterArgs.Identifier studyIdentifier]

    //        let loadArgs = [StudyProtocols.ProtocolLoadArgs.InputPath protocolTestFile; StudyProtocols.ProtocolLoadArgs.StudyIdentifier studyIdentifier]
            
    //        let testProtocolName = "peptide_digestion"
    //        let testProtocolTypeName = AnnotationValue.Text "Protein Digestion"

    //        setupArc configuration
    //        processCommand configuration StudyAPI.register studyArgs
    //        processCommand configuration StudyAPI.Protocols.load loadArgs
            
    //        let investigation = ISADotNet.XLSX.Investigation.fromFile (IsaModelConfiguration.getInvestigationFilePath configuration)
    //        match API.Study.tryGetByIdentifier studyIdentifier investigation.Studies.Value with
    //        | Some study ->
    //            let protocols = study.Protocols
    //            Expect.isSome protocols "Protocol was not added, as Protocols is still None"
    //            let protocol = API.Protocol.tryGetByName testProtocolName protocols.Value
    //            Expect.isSome protocol "Protocol could not be found, either it was not added or the name was not inserted correctly"
    //            let protocolType = protocol.Value.ProtocolType
    //            Expect.isSome protocolType "ProtocolType was not added to protocol"
    //            Expect.equal protocolType.Value.Name.Value testProtocolTypeName "ProtocolType name field was not correctly transferred from protocol file"

    //        | None -> Expect.isTrue false "Study was not registered, Protocol could not be tested"
    //    )
    //    testCase "AddFromProcessFile" (fun () -> 

    //        let studyIdentifier = "ProcessStudy"
            
    //        let studyArgs = [StudyRegisterArgs.Identifier studyIdentifier]

    //        let loadArgs = [StudyProtocols.ProtocolLoadArgs.InputPath processTestFile; StudyProtocols.ProtocolLoadArgs.StudyIdentifier studyIdentifier;StudyProtocols.ProtocolLoadArgs.IsProcessFile]
            
    //        let testProtocolName = "peptide_digestion"
    //        let testProtocolTypeName = AnnotationValue.Text "Protein Digestion"
                        
    //        processCommand configuration StudyAPI.register studyArgs
    //        processCommand configuration StudyAPI.Protocols.load loadArgs
            
    //        let investigation = ISADotNet.XLSX.Investigation.fromFile (IsaModelConfiguration.getInvestigationFilePath configuration)
    //        match API.Study.tryGetByIdentifier studyIdentifier investigation.Studies.Value with
    //        | Some study ->
    //            let protocols = study.Protocols
    //            Expect.isSome protocols "Protocol was not added, as Protocols is still None"
    //            let protocol = API.Protocol.tryGetByName testProtocolName protocols.Value
    //            Expect.isSome protocol "Protocol could not be found, either it was no added or the name was not inserted correctly"
    //            let protocolType = protocol.Value.ProtocolType
    //            Expect.isSome protocolType "ProtocolType was not added to protocol"
    //            Expect.equal protocolType.Value.Name.Value testProtocolTypeName "ProtocolType name field was not correctly transferred from protocol file"

    //        | None -> Expect.isTrue false "Study was not registered, Protocol could not be tested"       
    //    )
    //    testCase "DoesNothingIfAlreadyExisting" (fun () -> 

    //        let studyIdentifier = "AlreadyContaingProtocolStudy"                    

    //        let studyArgs = [StudyRegisterArgs.Identifier studyIdentifier]
    //        let protocolArgs = [StudyProtocols.ProtocolRegisterArgs.Name "peptide_digestion";StudyProtocols.ProtocolRegisterArgs.StudyIdentifier studyIdentifier]
    //        let loadArgs = [StudyProtocols.ProtocolLoadArgs.InputPath protocolTestFile; StudyProtocols.ProtocolLoadArgs.StudyIdentifier studyIdentifier]
                       
    //        processCommand configuration StudyAPI.register studyArgs
    //        processCommand configuration StudyAPI.Protocols.register protocolArgs

    //        let investigationBeforeLoadingProtocol = ISADotNet.XLSX.Investigation.fromFile (IsaModelConfiguration.getInvestigationFilePath configuration)

    //        processCommand configuration StudyAPI.Protocols.load loadArgs
            
    //        let investigation = ISADotNet.XLSX.Investigation.fromFile (IsaModelConfiguration.getInvestigationFilePath configuration)
           
    //        Expect.equal investigation investigationBeforeLoadingProtocol "Investigation should not have been altered in any way, as a protocol with the same name did already exist in the study, and the \"UpdateExisting\" flag was not set"
    //    )
    //    testCase "UpdateAlreadyExisting" (fun () -> 

    //        let studyIdentifier = "AlreadyContaingProtocolStudy"
                     
    //        let testProtocolName = "peptide_digestion"
    //        let testProtocolTypeName = AnnotationValue.Text "Protein Digestion"

    //        let studyArgs = [StudyRegisterArgs.Identifier studyIdentifier]
    //        let protocolArgs = [StudyProtocols.ProtocolRegisterArgs.Name "peptide_digestion";StudyProtocols.ProtocolRegisterArgs.StudyIdentifier studyIdentifier]
    //        let loadArgs = [StudyProtocols.ProtocolLoadArgs.InputPath protocolTestFile; StudyProtocols.ProtocolLoadArgs.StudyIdentifier studyIdentifier;StudyProtocols.ProtocolLoadArgs.UpdateExisting]
                       
    //        processCommand configuration StudyAPI.register studyArgs
    //        processCommand configuration StudyAPI.Protocols.register protocolArgs

    //        processCommand configuration StudyAPI.Protocols.load loadArgs
            
    //        let investigation = ISADotNet.XLSX.Investigation.fromFile (IsaModelConfiguration.getInvestigationFilePath configuration)
           
    //        match API.Study.tryGetByIdentifier studyIdentifier investigation.Studies.Value with
    //        | Some study ->
    //            let protocols = study.Protocols
    //            let protocol = API.Protocol.tryGetByName testProtocolName protocols.Value
    //            let protocolType = protocol.Value.ProtocolType
    //            Expect.isSome protocolType "ProtocolType was not added to protocol"
    //            Expect.equal protocolType.Value.Name.Value testProtocolTypeName "ProtocolType name field was not correctly transferred from protocol file"

    //        | None -> Expect.isTrue false "Study was not registered, Protocol could not be tested"               

    //    )
    //]
    //|> testSequenced


let testStudyContacts = 

    let testListName = "StudyContactTests"

    let config = createConfigFromDir testListName "Contacts"

    let investigationFileName = "isa.investigation.xlsx"
    let source = __SOURCE_DIRECTORY__
    let investigationToCopy = System.IO.Path.Combine([|source;"TestFiles";investigationFileName|])
    let investigationFilePath = System.IO.Path.Combine([|GeneralConfiguration.getWorkDirectory config;investigationFileName|])         
    let studyIdentifier = "BII-S-1"

    let studyToCopy = System.IO.Path.Combine([|source;"TestFiles";"studies"|])
    let studyFilePath = System.IO.Path.Combine([|GeneralConfiguration.getWorkDirectory config;"studies"|])      
    

    setupArc config

    //Copy testInvestigation
    System.IO.File.Copy(investigationToCopy,investigationFilePath,true)
    //Copy testStudy
    directoryCopy studyToCopy studyFilePath true

    let arc = ARC.load(config)

    let studyBeforeChangingIt = 
        arc.ISA.Value.GetStudy studyIdentifier

    testList testListName [
        testCase "Update" (fun () -> 
            let newAddress = "FunStreet"

            let firstName = "Stephen"
            let midInitials = "G"
            let lastName = "Oliver"

            let contactArgs = 
                [
                    StudyContacts.PersonUpdateArgs.StudyIdentifier studyIdentifier
                    StudyContacts.PersonUpdateArgs.FirstName firstName;
                    StudyContacts.PersonUpdateArgs.MidInitials midInitials;
                    StudyContacts.PersonUpdateArgs.LastName lastName;
                    StudyContacts.PersonUpdateArgs.Address newAddress;
                ]

            let personBeforeUpdating = 
                studyBeforeChangingIt.Contacts
                |> Person.tryGetByFullName firstName midInitials lastName
                |> Option.get 

            processCommand config StudyAPI.Contacts.update contactArgs
            
            let arc = ARC.load(config)
            let investigation = arc.ISA.Value

            let study = investigation.TryGetStudy studyIdentifier

            Expect.isSome study "Study missing after updating person"

            let person = Person.tryGetByFullName firstName midInitials lastName study.Value.Contacts

            Expect.isSome person "Person missing after updating person"

            let adress = person.Value.Address

            Expect.isSome adress "Adress missing after updating person"
            Expect.equal adress.Value newAddress "Adress was not updated with new value"
            Expect.equal person.Value {personBeforeUpdating with Address = Some newAddress} "Other values of person were changed even though only the Address should have been updated"

        )

    ]
    |> testSequenced

[<Tests>]
let studyTests = 
    testList "Study" [
        testStudyInit
        testStudyAdd
        testStudyContacts
    ]