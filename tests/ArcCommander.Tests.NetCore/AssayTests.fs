module AssayTests

open System.IO
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

type ArcInvestigation with

    member this.ContainsStudy(studyIdentifier : string) =
        this.StudyIdentifiers |> Seq.contains studyIdentifier

    member this.TryGetStudy(studyIdentifier : string) =
        if this.ContainsStudy studyIdentifier then 
            Some (this.GetStudy studyIdentifier)
        else
            None

    member this.DeregisterStudy(studyIdentifier : string) =
        this.RegisteredStudyIdentifiers.Remove(studyIdentifier)

type ArcStudy with
    member this.TryGetRegisteredAssayAt(index : int) = 
        this.RegisteredAssays
        |> Seq.tryItem index

    member this.TryGetRegisteredAssay(assayIdentifier : string) =
        this.RegisteredAssayIdentifiers 
        |> Seq.tryFindIndex((=) assayIdentifier)
        |> Option.bind this.TryGetRegisteredAssayAt

let standardISAArgs = 
    Map.ofList 
        [
            "investigationfilename" , "isa.investigation.xlsx"
            "studiesfilename"       , "isa.study.xlsx"
            "assayfilename"         , "isa.assay.xlsx"
        ]

let processCommand (arcConfiguration : ArcConfiguration) commandF (r : 'T list when 'T :> IArgParserTemplate) =

    let g = groupArguments r
    Prompt.deannotateArguments g 
    |> commandF arcConfiguration

let setupArc (arcConfiguration : ArcConfiguration) =
    let arcArgs : ArcInitArgs list = [ArcInitArgs.Identifier "TestInvestigation"] 

    processCommand arcConfiguration ArcAPI.init             arcArgs

let createConfigFromDir testListName testCaseName =
    let dir = Path.Combine(__SOURCE_DIRECTORY__, "TestResult", testListName, testCaseName)
    ArcConfiguration.GetDefault()
    |> ArcConfiguration.ofIniData
    |> fun c -> {c with General = (Map.ofList ["workdir", dir; "verbosity", "2"]) }


[<Tests>]
let testAssayTestFunction = 

    let testDirectory = Path.Combine(__SOURCE_DIRECTORY__, "TestFiles")

    testList "AssayTestFunctionTests" [
        testCase "MatchesAssayValues" (fun () ->
            let assayIdentifier = "a_proteome"
            let mt = OntologyAnnotation.fromString("protein expression profiling","OBI","http://purl.obolibrary.org/obo/OBI_0000615")
            let tt = OntologyAnnotation.fromString("mass spectrometry","OBI")
            let tp = OntologyAnnotation.fromString "iTRAQ"

            let testAssay = ArcAssay.create(assayIdentifier,mt,tt,tp)

            let arc = ARC.load(testDirectory)
            let investigation = arc.ISA.Value
            // Positive control
            Expect.equal investigation.Studies.[0].RegisteredAssays.[0] testAssay "The assay in the file should match the one created per hand but did not"
            // Negative control
            Expect.notEqual investigation.Studies.[0].RegisteredAssays.[1] testAssay "The assay in the file did not match the one created per hand and still returned true"
        )
        testCase "ListsCorrectAssaysInCorrectStudies" (fun () -> 
            let testAssays = [
                "BII-S-1",["a_proteome";"a_metabolome"; "a_transcriptome"]
                "BII-S-2",["a_microarray"]
            ]

            let arc = ARC.load(testDirectory)
            let investigation = arc.ISA.Value

            testAssays
            |> List.iter (fun (studyIdentifier,assayIdentifiers) ->
                match investigation.TryGetStudy studyIdentifier with
                | Some study ->
                    Expect.sequenceEqual study.RegisteredAssayIdentifiers assayIdentifiers "Assay Filenames did not match the expected ones"
                | None -> failwith "Study %s could not be taken from the ivnestigation even though it should be there"                    
            )
        )

    ]
    |> testSequenced


[<Tests>]
let testAssayRegister = 

    testList "AssayRegisterTests" [

        testCase "AddToExistingStudy" (fun () -> 

            let configuration = createConfigFromDir "AssayRegisterTests" "AddToExistingStudy"
            setupArc configuration

            let assayIdentifier = "TestAssay"
            let measurementType = "TestMeasurementType"
            let studyIdentifier = "TestStudy"
            
            let studyArgs : StudyRegisterArgs list = [StudyRegisterArgs.Identifier studyIdentifier]
            let assayInitArgs : AssayInitArgs list = [
                AssayInitArgs.AssayIdentifier assayIdentifier
                AssayInitArgs.MeasurementType measurementType
            ]
            let assayRegisterArgs : AssayRegisterArgs list = [
                AssayRegisterArgs.StudyIdentifier studyIdentifier
                AssayRegisterArgs.AssayIdentifier assayIdentifier
            ]
            let testAssay = ArcAssay.create (assayIdentifier,measurementType = OntologyAnnotation.fromString(measurementType))
            
            processCommand configuration StudyAPI.register studyArgs
            processCommand configuration AssayAPI.init     assayInitArgs
            processCommand configuration AssayAPI.register assayRegisterArgs
            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"

            match isa.TryGetStudy studyIdentifier with
            | Some study ->
                let assay = study.GetRegisteredAssay assayIdentifier
                Expect.equal assay testAssay "Assay is missing values"
            | None ->
                failwith "Study was not added to the investigation"
        )
        testCase "DoNothingIfAssayExisting" (fun () -> 

            let configuration = createConfigFromDir "AssayRegisterTests" "DoNothingIfAssayExisting"
            setupArc configuration

            let assayIdentifier = "TestAssay"
            let measurementType = "TestMeasurementType"
            let studyIdentifier = "TestStudy"

            let studyArgs : StudyRegisterArgs list = [StudyRegisterArgs.Identifier studyIdentifier]

            processCommand configuration StudyAPI.register studyArgs
            
            let assay1Args : AssayAddArgs list = [
                AssayAddArgs.StudyIdentifier studyIdentifier
                AssayAddArgs.AssayIdentifier assayIdentifier
                AssayAddArgs.MeasurementType measurementType
            ]
            let assay2Args : AssayAddArgs list = [
                AssayAddArgs.StudyIdentifier studyIdentifier
                AssayAddArgs.AssayIdentifier assayIdentifier
                AssayAddArgs.MeasurementType "failedTestMeasurementType"
            ]
            let testMT = OntologyAnnotation.fromString(measurementType)
            let testAssay = ArcAssay.create (assayIdentifier,measurementType = testMT)
            
            processCommand configuration AssayAPI.add assay1Args
            processCommand configuration AssayAPI.add assay2Args // <- trying to create a second, nearly identical assay which shall NOT work
            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"

            match isa.TryGetStudy studyIdentifier with
            | Some study ->
                let assay = study.GetRegisteredAssay assayIdentifier
                Expect.equal assay testAssay "Assay values were removed"
                Expect.equal study.RegisteredAssayCount 1 "Assay was added even though it should't have been as an assay with the same identifier was already present"
                
                Expect.equal assay.MeasurementType.Value testMT "Assay was overwritten with second assay."
            | None ->
                failwith "Study was removed from the investigation"
        )
        testCase "AddSecondAssayToExistingStudy" (fun () -> 

            let configuration = createConfigFromDir "AssayRegisterTests" "AddSecondAssayToExistingStudy"
            setupArc configuration

            let oldAssayIdentifier = "TestAssay"
            let oldMeasurementType = "TestMeasurementType"
            let studyIdentifier = "TestStudy"

            let assayAddArgs = [
                AssayAddArgs.StudyIdentifier studyIdentifier
                AssayAddArgs.AssayIdentifier oldAssayIdentifier
                AssayAddArgs.MeasurementType oldMeasurementType
            ]
            
            processCommand configuration AssayAPI.add assayAddArgs

            let newAssayIdentifier = "SecondTestAssay"
            let newMeasurementType = "OtherMeasurementType"

            let assayInitArgs = [
                AssayInitArgs.AssayIdentifier newAssayIdentifier
                AssayInitArgs.MeasurementType newMeasurementType
            ]

            let assayRegisterArgs = [
                    AssayRegisterArgs.StudyIdentifier studyIdentifier
                    AssayRegisterArgs.AssayIdentifier newAssayIdentifier
                ]

            let testMT = OntologyAnnotation.fromString(newMeasurementType)
            let testAssay = ArcAssay.create (newAssayIdentifier,measurementType = testMT)
            
            
            processCommand configuration AssayAPI.init      assayInitArgs
            processCommand configuration AssayAPI.register  assayRegisterArgs
            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"

            match isa.TryGetStudy studyIdentifier with
            | Some study ->

                let assay = study.GetRegisteredAssay newAssayIdentifier
                Expect.equal assay testAssay "New Assay is missing values"
                Expect.sequenceEqual (study.RegisteredAssayIdentifiers) [oldAssayIdentifier; newAssayIdentifier] "Either an assay is missing or they're in an incorrect order"
            | None ->
                failwith "Study was removed from the investigation"
        )
        testCase "CreateStudyIfNotExisting" (fun () -> 

            let configuration = createConfigFromDir "AssayRegisterTests" "CreateStudyIfNotExisting"
            setupArc configuration

            let assayIdentifier = "TestAssay"
            let measurementType = "MeasurementTypeOfStudyCreatedByAssayRegister"
            let studyIdentifier = "StudyCreatedByAssayRegister"
            
            let assayArgs : AssayAddArgs list = [
                AssayAddArgs.StudyIdentifier studyIdentifier
                AssayAddArgs.AssayIdentifier assayIdentifier
                MeasurementType "MeasurementTypeOfStudyCreatedByAssayRegister"
            ]
            let testMT = OntologyAnnotation.fromString(measurementType)
            let testAssay = ArcAssay.create (assayIdentifier,measurementType = testMT)
            
            processCommand configuration AssayAPI.add assayArgs
            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"

            let studyOption = isa.TryGetStudy studyIdentifier
            Expect.isSome studyOption "Study should have been created in order for assay to be placed in it"     
            
            let assay = studyOption.Value.GetRegisteredAssay assayIdentifier
            Expect.equal assay testAssay "Assay is missing values"
        )
        testCase "StudyNameNotGivenUseAssayName" (fun () -> 

            let configuration = createConfigFromDir "AssayRegisterTests" "StudyNameNotGivenUseAssayName"
            setupArc configuration

            let assayIdentifier = "TestAssayWithoutStudyName"
            let technologyType = "InferName"
            
            let assayArgs = [
                AssayAddArgs.AssayIdentifier assayIdentifier
                TechnologyType technologyType
            ]
            let tt = OntologyAnnotation.fromString(technologyType)
            let testAssay = ArcAssay(assayIdentifier,technologyType = tt)
            
            processCommand configuration AssayAPI.add assayArgs
            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"

            let studyOption = isa.TryGetStudy assayIdentifier
            Expect.isSome studyOption "Study should have been created with the name of the assay but was not"     
            
            let assay = studyOption.Value.GetRegisteredAssay assayIdentifier
            Expect.equal assay testAssay "Assay is missing values"
        )
        // This case checks if no duplicate study gets created when the user adds an assay with an identifier (and no given study identifier) that equals a previously added study's identifier.
        testCase "StudyNameNotGivenUseAssayNameNoDuplicateStudy" (fun () -> 

            let configuration = createConfigFromDir "AssayRegisterTests" "StudyNameNotGivenUseAssayNameNoDuplicateStudy"
            setupArc configuration

            let studyIdentifier = "TestAssayWithoutStudyName"
            processCommand configuration StudyAPI.add [StudyAddArgs.Identifier studyIdentifier]

            let assayIdentifier = studyIdentifier
            processCommand configuration AssayAPI.add [AssayAddArgs.AssayIdentifier assayIdentifier]
            
            let testAssay = ArcAssay.create (assayIdentifier)

            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"
            
            let studiesWithIdentifiers = isa.StudyIdentifiers |> Seq.toList |> List.filter ((=) assayIdentifier)
            
            Expect.equal studiesWithIdentifiers.Length 1 "Duplicate study was added"

            let study = isa.GetStudy assayIdentifier

            let assay = study.GetRegisteredAssay assayIdentifier
            Expect.equal assay testAssay "New Assay is missing values"
            Expect.equal study.RegisteredAssayCount 1 "Assay missing"
        )
        testCase "StudyNameNotGivenUseAssayNameNoDuplicateAssay" (fun () ->

            let configuration = createConfigFromDir "AssayRegisterTests" "StudyNameNotGivenUseAssayNameNoDuplicateAssay"
            setupArc configuration

            let assayIdentifier = "StudyCreatedByAssayRegister"

            let assayArgs = [AssayAddArgs.AssayIdentifier assayIdentifier]
            
            processCommand configuration AssayAPI.add assayArgs
            processCommand configuration AssayAPI.add assayArgs // <- trying to create a second, nearly identical assay which shall NOT work
            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"
            
            let studiesWithIdentifiers = isa.StudyIdentifiers |> Seq.toList |> List.filter ((=) assayIdentifier)
            
            Expect.equal studiesWithIdentifiers.Length 1 "Duplicate study was added"

            let study = isa.GetStudy assayIdentifier 
            
            let assay = study.GetRegisteredAssay assayIdentifier
            Expect.equal study.RegisteredAssayCount 1 "Duplicate Assay added"
        )
    ]
    |> testSequenced

[<Tests>]
let testAssayUpdate = 

    testList "AssayUpdateTests" [

        testCase "UpdateStandard" (fun () -> 

            let configuration = createConfigFromDir "AssayUpdateTests" "UpdateStandard"
            setupArc configuration

            let studyIdentifier = "Study1"
            let assayIdentifier = "Assay2"

            let assay1Args = [
                AssayAddArgs.StudyIdentifier studyIdentifier
                AssayAddArgs.AssayIdentifier "Assay1"
                AssayAddArgs.MeasurementType "Assay1Method"
            ]
            let assay2Args = [
                AssayAddArgs.StudyIdentifier studyIdentifier
                AssayAddArgs.AssayIdentifier assayIdentifier
                AssayAddArgs.MeasurementType "Assay2Method"
                AssayAddArgs.TechnologyType "Assay2Tech"
            ]
            let assay3Args = [
                AssayAddArgs.StudyIdentifier "Study2"
                AssayAddArgs.AssayIdentifier "Assay3"
                AssayAddArgs.TechnologyType "Assay3Tech"
            ]
            
            processCommand configuration AssayAPI.add assay1Args
            processCommand configuration AssayAPI.add assay2Args
            processCommand configuration AssayAPI.add assay3Args

            let measurementType = "NewMeasurementType"
            let mt = OntologyAnnotation.fromString(measurementType)
            let tt = OntologyAnnotation.fromString("Assay2Tech")
            let testAssay = ArcAssay.create(assayIdentifier,mt,tt)

            let assayUpdateArgs : AssayUpdateArgs list = [
                AssayUpdateArgs.AssayIdentifier assayIdentifier
                AssayUpdateArgs.MeasurementType measurementType
            ]

            let arcBeforeUpdate = ARC.load(configuration)
            let isaBeforeUpdate = Expect.wantSome arcBeforeUpdate.ISA "Investigation was not created"


            processCommand configuration AssayAPI.update assayUpdateArgs
            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"
            
            Expect.equal (isa.GetStudyAt(1).RegisteredAssays[1]) (isaBeforeUpdate.GetStudyAt(1).RegisteredAssays[1]) "Only assay in first study was supposed to be updated, but study 2 is also different"
            
            let study = isa.GetStudyAt 0
            let studyBeforeUpdate = isaBeforeUpdate.GetStudyAt 0

            Expect.equal (study.RegisteredAssays[0]) (studyBeforeUpdate.RegisteredAssays[0]) "Only assay number 1 in first study was supposed to be updated, but first assay is also different"

            let assay = study.RegisteredAssays[1]

            Expect.equal assay.Identifier testAssay.Identifier "Assay Filename has changed even though it shouldn't"
            Expect.equal assay.TechnologyType testAssay.TechnologyType "Assay technology type has changed, even though no value was given and the \"ReplaceWithEmptyValues\" flag was not set"
            Expect.equal assay.MeasurementType testAssay.MeasurementType "Assay Measurement type was not updated correctly"

        )
        testCase "UpdateReplaceWithEmpty" (fun () -> 

            let configuration = createConfigFromDir "AssayUpdateTests" "UpdateReplaceWithEmpty"
            setupArc configuration

            let studyIdentifier1 = "Study1"
            let assayIdentifier1 = "Assay1"

            let assay1AddArgs = [
                AssayAddArgs.StudyIdentifier studyIdentifier1
                AssayAddArgs.AssayIdentifier assayIdentifier1
            ]

            processCommand configuration AssayAPI.add assay1AddArgs // add first assay with study that shall not be touched by anything next
           
            let studyIdentifier2 = "Study2"
            let assayIdentifier2 = "Assay2"
            let oldMeasurementType = "OldMeasurementType"

            let assay2AddArgs = [
                AssayAddArgs.StudyIdentifier studyIdentifier2
                AssayAddArgs.AssayIdentifier assayIdentifier2
                AssayAddArgs.MeasurementType oldMeasurementType
            ]

            processCommand configuration AssayAPI.add assay2AddArgs

            let newMeasurementType = "NewMeasurementType"
            let mt = OntologyAnnotation.fromString(newMeasurementType)
            let testAssay = ArcAssay.create(assayIdentifier2,mt)

            let assayUpdateArgs : AssayUpdateArgs list = [
                AssayUpdateArgs.ReplaceWithEmptyValues
                AssayUpdateArgs.AssayIdentifier assayIdentifier2
                AssayUpdateArgs.MeasurementType newMeasurementType
            ]           

            let arcBeforeUpdate = ARC.load(configuration)
            let isaBeforeUpdate = Expect.wantSome arcBeforeUpdate.ISA "Investigation was not created"

            processCommand configuration AssayAPI.update assayUpdateArgs
            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"          

            Expect.equal (isa.GetStudyAt(0)) (isaBeforeUpdate.GetStudyAt(0))  "Only assay in second study was supposed to be updated, but study 1 is also different"
            
            let study = isa.RegisteredStudies[1]

            let assay = study.RegisteredAssays[0]

            Expect.equal assay.Identifier testAssay.Identifier "Assay Filename has changed even though it shouldnt"
            Expect.isNone assay.TechnologyType "Assay technology type has not been removed, even though no value was given and the \"ReplaceWithEmptyValues\" flag was set"
            Expect.equal assay.MeasurementType testAssay.MeasurementType "Assay Measurement type was not updated correctly"
        )
    ]
    |> testSequenced

[<Tests>]
let testAssayUnregister = 

    testList "AssayUnregisterTests" [

        testCase "AssayExists" (fun () -> 

            let configuration = createConfigFromDir "AssayUnregisterTests" "AssayExists"
            setupArc configuration

            let assay1Args = [
                AssayAddArgs.StudyIdentifier "Study1"
                AssayAddArgs.AssayIdentifier "Assay1"
                AssayAddArgs.MeasurementType "Assay1Method"
            ]
            let assay2Args = [
                AssayAddArgs.StudyIdentifier "Study1"
                AssayAddArgs.AssayIdentifier "Assay2"
                AssayAddArgs.MeasurementType "Assay2Method"
                AssayAddArgs.TechnologyType "Assay2Tech"
            ]
            let assay3Args = [
                AssayAddArgs.StudyIdentifier "Study2"
                AssayAddArgs.AssayIdentifier "Assay3"
                AssayAddArgs.TechnologyType "Assay3Tech"
            ]

            let studyIdentifier = "Study1"
            let assayIdentifier = "Assay2"

            let assayArgs : AssayUnregisterArgs list = [
                AssayUnregisterArgs.StudyIdentifier studyIdentifier
                AssayUnregisterArgs.AssayIdentifier assayIdentifier
            ]
            
            processCommand configuration AssayAPI.add assay1Args
            processCommand configuration AssayAPI.add assay2Args
            processCommand configuration AssayAPI.add assay3Args

            let arcBeforeUpdate = ARC.load(configuration)
            let isaBeforeUpdate = Expect.wantSome arcBeforeUpdate.ISA "Investigation was not created"

            processCommand configuration AssayAPI.unregister assayArgs
            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"              

            Expect.equal (isa.RegisteredStudies[1]) (isaBeforeUpdate.RegisteredStudies[1])  "Only assay in first study was supposed to be unregistered, but study 2 is also different"
            
            let study = isa.RegisteredStudies[0]
            let studyBeforeUpdate = isaBeforeUpdate.RegisteredStudies[0]

            Expect.notEqual study.RegisteredAssayCount 0 "Only assay number 2 in first study was supposed to be unregistered, but both assays were removed"
            Expect.equal study.RegisteredAssays[0] studyBeforeUpdate.RegisteredAssays[0] "Only assay number 2 in first study was supposed to be unregistered, but first assay is also different"
            Expect.equal study.RegisteredAssayCount 1 "Only first assay was supposed to be left in the study after removing the second but both are still present"
        )
        testCase "AssayDoesNotExist" (fun () -> 

            let configuration = createConfigFromDir "AssayUnregisterTests" "AssayDoesNotExist"
            setupArc configuration
           
            let studyIdentifier = "Study2"
            let assayIdentifier = "FakeAssayName"

            let assayArgs : AssayUnregisterArgs list = [AssayUnregisterArgs.StudyIdentifier studyIdentifier;AssayUnregisterArgs.AssayIdentifier assayIdentifier]

            let arcBeforeUpdate = ARC.load(configuration)
            let isaBeforeUpdate = Expect.wantSome arcBeforeUpdate.ISA "Investigation was not created"

            processCommand configuration AssayAPI.unregister assayArgs
            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"           

            Expect.equal isa isaBeforeUpdate "Investigation values did change even though the given assay does not exist and none should have been removed"
            
        )
        testCase "StudyDoesNotExist" (fun () -> 

            let configuration = createConfigFromDir "AssayUnregisterTests" "StudyDoesNotExist"
            setupArc configuration

            let studyIdentifier = "FakeStudyName"
            let assayIdentifier = "Assay2"

            let assayUnrArgs : AssayUnregisterArgs list = [
                AssayUnregisterArgs.StudyIdentifier studyIdentifier
                AssayUnregisterArgs.AssayIdentifier assayIdentifier
            ]

            let arcBeforeUpdate = ARC.load(configuration)
            let isaBeforeUpdate = Expect.wantSome arcBeforeUpdate.ISA "Investigation was not created"

            processCommand configuration AssayAPI.unregister assayUnrArgs
            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"              

            Expect.equal isa isaBeforeUpdate "Investigation values did change even though the given study does not exist and none should have been removed"
            
        )
    ]
    |> testSequenced

[<Tests>]
let testAssayMove = 

    testList "AssayMoveTests" [

        testCase "ToExistingStudy" (fun () -> 

            let configuration = createConfigFromDir "AssayMoveTests" "ToExistingStudy"
            setupArc configuration

            let studyIdentifier = "Study1"
            let targetStudyIdentfier = "Study2"
            let assayIdentifier = "Assay2"

            let assay1Args = [
                AssayAddArgs.StudyIdentifier studyIdentifier
                AssayAddArgs.AssayIdentifier "Assay1"
                AssayAddArgs.MeasurementType "Assay1Method"
            ]
            let assay2Args = [
                AssayAddArgs.StudyIdentifier studyIdentifier
                AssayAddArgs.AssayIdentifier assayIdentifier
                AssayAddArgs.MeasurementType "Assay2Method"
                AssayAddArgs.TechnologyType "Assay2Tech"
            ]
            let assay3Args = [
                AssayAddArgs.StudyIdentifier targetStudyIdentfier
                AssayAddArgs.AssayIdentifier "Assay3"
                AssayAddArgs.TechnologyType "Assay3Tech"
            ]

            let assayMovArgs : AssayMoveArgs list = [
                AssayMoveArgs.AssayIdentifier assayIdentifier
                AssayMoveArgs.StudyIdentifier studyIdentifier
                AssayMoveArgs.TargetStudyIdentifier targetStudyIdentfier
            ]
            
            processCommand configuration AssayAPI.add assay1Args
            processCommand configuration AssayAPI.add assay2Args
            processCommand configuration AssayAPI.add assay3Args


            let arcBeforeUpdate = ARC.load(configuration)
            let isaBeforeUpdate = Expect.wantSome arcBeforeUpdate.ISA "Investigation was not created"
            let testAssay = isaBeforeUpdate.GetStudyAt(0).RegisteredAssays[1]

            processCommand configuration AssayAPI.move assayMovArgs

            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"  

            Expect.equal (isa.GetStudyAt(0).RegisteredAssayCount) (isaBeforeUpdate.GetStudyAt(0).RegisteredAssayCount - 1) "Assay was not removed from source study"

            let assay = isa.GetStudyAt(1).GetRegisteredAssay assayIdentifier

            //Expect.isSome assay "Assay was not added to target study"

            Expect.equal assay testAssay "Assay was moved but some values are not correct"
            
        )
        testCase "ToNewStudy" (fun () -> 

            let configuration = createConfigFromDir "AssayMoveTests" "ToNewStudy"
            setupArc configuration

            let studyIdentifier = "Study1"
            let targetStudyIdentfier = "NewStudy"
            let assayIdentifier = "Assay1"

            let assayAddArgs = [
                AssayAddArgs.StudyIdentifier studyIdentifier
                AssayAddArgs.AssayIdentifier assayIdentifier
                AssayAddArgs.MeasurementType "Assay1Method"
            ]

            processCommand configuration AssayAPI.add assayAddArgs

            let assayMovArgs : AssayMoveArgs list = [
                AssayMoveArgs.TargetStudyIdentifier targetStudyIdentfier
                AssayMoveArgs.StudyIdentifier studyIdentifier
                AssayMoveArgs.AssayIdentifier assayIdentifier
            ]
 


            let arcBeforeUpdate = ARC.load(configuration)
            let isaBeforeUpdate = Expect.wantSome arcBeforeUpdate.ISA "Investigation was not created"
            let testAssay = isaBeforeUpdate.GetStudyAt(0).RegisteredAssays[0]

            processCommand configuration AssayAPI.move assayMovArgs

            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"  

            Expect.isNone (isa.GetStudyAt(0).TryGetRegisteredAssay assayIdentifier) "Assay was not removed from source study"

            let study = isa.TryGetStudy "NewStudy"

            Expect.isSome study "New Study was not created"

            let assay = study.Value.TryGetRegisteredAssay assayIdentifier

            Expect.isSome assay "Assay was not added to target study"

            Expect.equal assay.Value testAssay "Assay was moved but some values are not correct"
            
        )
        testCase "AssayDoesNotExist" (fun () -> 

            let configuration = createConfigFromDir "AssayMoveTests" "AssayDoesNotExist"
            setupArc configuration
           
            let studyIdentifier = "Study2"
            let targetStudyIdentifier = "Study1"
            let assayIdentifier = "FakeAssayName"

            let assayArgs : AssayMoveArgs list = [AssayMoveArgs.StudyIdentifier studyIdentifier;AssayMoveArgs.TargetStudyIdentifier targetStudyIdentifier;AssayMoveArgs.AssayIdentifier assayIdentifier]


            let arcBeforeUpdate = ARC.load(configuration)
            let isaBeforeUpdate = Expect.wantSome arcBeforeUpdate.ISA "Investigation was not created"

            processCommand configuration AssayAPI.move assayArgs

            
            let arc = ARC.load(configuration)
            let isa = Expect.wantSome arc.ISA "Investigation was not created"           

            Expect.equal isa isaBeforeUpdate "Investigation values did change even though the given assay does not exist and none should have been moved"
            
        )
    ]
    |> testSequenced