namespace ArcCommander.APIs

open System.IO

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISADotNet
open ISADotNet.XLSX
open ISADotNet.XLSX.AssayFile
open ISADotNet.XLSX.AssayFile.MetaData

open FSharpSpreadsheetML
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet

/// ArcCommander Assay API functions that get executed by the assay focused subcommand verbs
module AssayAPI =        


    module AssayFolder =
        
        let exists (arcConfiguration : ArcConfiguration) (identifier : string) =
            AssayConfiguration.getFolderPath identifier arcConfiguration
            |> System.IO.Directory.Exists

    module AssayFile =
        
        let exists (arcConfiguration : ArcConfiguration) (identifier : string) =
            IsaModelConfiguration.getAssayFilePath identifier arcConfiguration
            |> System.IO.File.Exists
        
        let create (arcConfiguration : ArcConfiguration) (assay) (identifier : string) =
            IsaModelConfiguration.getAssayFilePath identifier arcConfiguration
            |> ISADotNet.XLSX.AssayFile.Assay.init "Investigation" (Some assay) None identifier

    /// Initializes a new empty assay file and associated folder structure in the arc.
    let init (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Assay Init"

        let name = getFieldValueByName "AssayIdentifier" assayArgs

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        
        let assayFileName = 
            IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration
            |> Option.get

        let assay = 
            Assays.fromString
                (getFieldValueByName  "MeasurementType"                     assayArgs)
                (getFieldValueByName  "MeasurementTypeTermAccessionNumber"  assayArgs)
                (getFieldValueByName  "MeasurementTypeTermSourceREF"        assayArgs)
                (getFieldValueByName  "TechnologyType"                      assayArgs)
                (getFieldValueByName  "TechnologyTypeTermAccessionNumber"   assayArgs)
                (getFieldValueByName  "TechnologyTypeTermSourceREF"         assayArgs)
                (getFieldValueByName  "TechnologyPlatform"                  assayArgs)
                assayFileName
                []

        if AssayFolder.exists arcConfiguration name then
            if verbosity >= 1 then printfn "Assay folder with identifier %s already exists" name
        else
            AssayConfiguration.getSubFolderPaths name arcConfiguration
            |> Array.iter (Directory.CreateDirectory >> ignore)

            AssayFile.create arcConfiguration assay name 

            AssayConfiguration.getFilePaths name arcConfiguration
            |> Array.iter (File.Create >> ignore)


    /// Updates an existing assay file in the ARC with the given assay metadata contained in cliArgs.
    let update (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =
        
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

        if verbosity >= 1 then printfn "Start Assay Update"

        let updateOption = if containsFlag "ReplaceWithEmptyValues" assayArgs then API.Update.UpdateAll else API.Update.UpdateByExisting            

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFileName = 
            IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration
            |> Option.get

        let assay = 
            Assays.fromString
                (getFieldValueByName  "MeasurementType" assayArgs)
                (getFieldValueByName  "MeasurementTypeTermAccessionNumber" assayArgs)
                (getFieldValueByName  "MeasurementTypeTermSourceREF" assayArgs)
                (getFieldValueByName  "TechnologyType" assayArgs)
                (getFieldValueByName  "TechnologyTypeTermAccessionNumber" assayArgs)
                (getFieldValueByName  "TechnologyTypeTermSourceREF" assayArgs)
                (getFieldValueByName  "TechnologyPlatform" assayArgs)
                assayFileName
                []

        let studyIdentifier = 
            match getFieldValueByName "StudyIdentifier" assayArgs with
            | "" -> assayIdentifier
            | s -> 
                if verbosity >= 2 then printfn "No Study Identifier given, use assayIdentifier instead"
                s

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath

        // part that writes assay metadata into the investigation file
        match investigation.Studies with
        | Some studies -> 
            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                match study.Assays with
                | Some assays -> 
                    if API.Assay.existsByFileName assayFileName assays then
                        API.Assay.updateByFileName updateOption assay assays
                        |> API.Study.setAssays study
                    else
                        if verbosity >= 1 then printfn "Assay with the identifier %s does not exist in the study with the identifier %s" assayIdentifier studyIdentifier
                        if containsFlag "AddIfMissing" assayArgs then
                            if verbosity >= 1 then printfn "Registering assay as AddIfMissing Flag was set" 
                            API.Assay.add assays assay
                            |> API.Study.setAssays study
                        else 
                            if verbosity >= 2 then printfn "AddIfMissing argument can be used to register assay with the update command if it is missing" 
                            study
                | None -> 
                    if verbosity >= 1 then printfn "The study with the identifier %s does not contain any assays" studyIdentifier
                    if containsFlag "AddIfMissing" assayArgs then
                        if verbosity >= 1 then printfn "Registering assay as AddIfMissing Flag was set" 
                        [assay]
                        |> API.Study.setAssays study
                    else 
                        if verbosity >= 2 then printfn "AddIfMissing argument can be used to register assay with the update command if it is missing" 
                        study
                |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                |> API.Investigation.setStudies investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier              
                investigation
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"  
            investigation
        |> Investigation.toFile investigationFilePath
        
        let assayFilepath = IsaModelConfiguration.tryGetAssayFilePath assayIdentifier arcConfiguration |> Option.get
        
        printfn "DEBUG: got assayFilepath: %s" assayFilepath

        let doc = Spreadsheet.fromFile assayFilepath true

        printfn "DEBUG: got doc"

        // part that writes assay metadata into the assay file
        try 
            //let persons = MetaData.getPersons "Investigation" doc
            
            //match API.Person.tryGetByFullName firstName midInitials lastName persons with
            //| Some person ->
            //    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir
            //        (List.singleton >> Contacts.toRows None) 
            //        (Contacts.fromRows None 1 >> fun (_,_,_,items) -> items.Head)
            //        person
            //    |> fun p -> 
            //        let newPersons = API.Person.updateBy ((=) person) API.Update.UpdateAll p persons
            //        MetaData.overwriteWithPersons "Investigation" newPersons doc
            //| None ->
            //    if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the assay with the identifier %s." firstName midInitials lastName assayIdentifier

            // takes persons from before => persons don't get touched by `arc a update`, use `arc a person update` instead
            //let persons = MetaData.getPersons "Investigation" doc

            printfn "AssayData is %A" assay

            MetaData.overwriteWithAssayInfo "Investigation" assay doc

            printfn "DEBUG: overwroteWithAssayInfo"
            //printfn "DEBUG: did nothing"

            // check if persons get deleted. if yes -> uncomment
            //MetaData.overwriteWithPersons "Investigation" persons doc
            
        finally
            Spreadsheet.close doc

    /// Opens an existing assay file in the ARC with the text editor set in globalArgs, additionally setting the given assay metadata contained in assayArgs.
    let edit (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =
        
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

        if verbosity >= 1 then printfn "Start Assay Edit"

        let editor  = GeneralConfiguration.getEditor        arcConfiguration
        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        
        let assayFileName = 
            IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration
            |> Option.get

        let studyIdentifier = 
            match getFieldValueByName "StudyIdentifier" assayArgs with
            | "" -> assayIdentifier
            | s -> 
                if verbosity >= 2 then printfn "No Study Identifier given, use assayIdentifier instead"
                s

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath

        
        match investigation.Studies with
        | Some studies -> 
            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                match study.Assays with
                | Some assays -> 
                    match API.Assay.tryGetByFileName assayFileName assays with
                    | Some assay ->
                
                        ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                            (List.singleton >> Assays.toRows None) 
                            (Assays.fromRows None 1 >> fun (_,_,_,items) -> items.Head) 
                            assay
                        |> fun a -> API.Assay.updateBy ((=) assay) API.Update.UpdateAll a assays
                        |> API.Study.setAssays study
                        |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                        |> API.Investigation.setStudies investigation

                    | None ->
                        if verbosity >= 1 then printfn "Assay with the identifier %s does not exist in the study with the identifier %s" assayIdentifier studyIdentifier
                        investigation
                | None -> 
                    if verbosity >= 1 then printfn "The study with the identifier %s does not contain any assays" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"  
            investigation
        |> Investigation.toFile investigationFilePath


    /// Registers an existing assay in the ARC's investigation file with the given assay metadata contained in assayArgs.
    let register (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Assay Register"

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        
        let assayFileName = 
            IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration
            |> Option.get
        
        let assay = 
            Assays.fromString
                (getFieldValueByName  "MeasurementType" assayArgs)
                (getFieldValueByName  "MeasurementTypeTermAccessionNumber" assayArgs)
                (getFieldValueByName  "MeasurementTypeTermSourceREF" assayArgs)
                (getFieldValueByName  "TechnologyType" assayArgs)
                (getFieldValueByName  "TechnologyTypeTermAccessionNumber" assayArgs)
                (getFieldValueByName  "TechnologyTypeTermSourceREF" assayArgs)
                (getFieldValueByName  "TechnologyPlatform" assayArgs)
                assayFileName
                []
               
        let studyIdentifier = 
            match getFieldValueByName "StudyIdentifier" assayArgs with
            | "" -> 
                if verbosity >= 1 then printfn "No Study Identifier given, use assayIdentifier instead"
                assayIdentifier
            | s -> s

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath
                
        match investigation.Studies with
        | Some studies -> 
            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                match study.Assays with
                | Some assays -> 
                    match API.Assay.tryGetByFileName assayFileName assays with
                    | Some assay ->
                        if verbosity >= 1 then printfn "Assay with the identifier %s already exists in the investigation file" assayIdentifier
                        assays
                    | None ->                       
                        API.Assay.add assays assay                     
                | None ->
                    [assay]
                |> API.Study.setAssays study
                |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                
            | None ->
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist yet, creating it now" studyIdentifier
                if StudyAPI.StudyFile.exists arcConfiguration studyIdentifier |> not then
                    StudyAPI.StudyFile.create arcConfiguration studyIdentifier
                let info = Study.StudyInfo.create studyIdentifier "" "" "" "" "" []
                Study.fromParts info [] [] [] [assay] [] []
                |> API.Study.add studies
        | None ->
            if verbosity >= 1 then printfn "Study with the identifier %s does not exist yet, creating it now" studyIdentifier
            if StudyAPI.StudyFile.exists arcConfiguration studyIdentifier |> not then
                StudyAPI.StudyFile.create arcConfiguration studyIdentifier
            let info = Study.StudyInfo.create studyIdentifier "" "" "" "" "" []
            [Study.fromParts info [] [] [] [assay] [] []]
        |> API.Investigation.setStudies investigation
        |> Investigation.toFile investigationFilePath
    
    /// Creates a new assay file and associated folder structure in the arc and registers it in the ARC's investigation file with the given assay metadata contained in assayArgs.
    let add (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        init arcConfiguration assayArgs
        register arcConfiguration assayArgs

    /// Unregisters an assay file from the ARC's investigation file.
    let unregister (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Assay Unregister"

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFileName = 
            IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration
            |> Option.get

        let studyIdentifier = 
            match getFieldValueByName "StudyIdentifier" assayArgs with
            | "" -> assayIdentifier
            | s -> 
                if verbosity >= 2 then printfn "No Study Identifier given, use assayIdentifier instead"
                s

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath

        match investigation.Studies with
        | Some studies -> 
            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                match study.Assays with
                | Some assays -> 
                    match API.Assay.tryGetByFileName assayFileName assays with
                    | Some assay ->
                        API.Assay.removeByFileName assayFileName assays
                        |> API.Study.setAssays study
                        |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                        |> API.Investigation.setStudies investigation
                    | None ->
                        if verbosity >= 1 then printfn "Assay with the identifier %s does not exist in the study with the identifier %s" assayIdentifier studyIdentifier
                        investigation
                | None -> 
                    if verbosity >= 1 then printfn "The study with the identifier %s does not contain any assays" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"  
            investigation
        |> Investigation.toFile investigationFilePath
    
    /// Deletes assay folder and underlying file structure of given assay.
    let delete (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Assay Delete"

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFolder = 
            AssayConfiguration.tryGetFolderPath assayIdentifier arcConfiguration
            |> Option.get

        if System.IO.Directory.Exists(assayFolder) then
            System.IO.Directory.Delete(assayFolder,true)

    /// Remove an assay from the ARC by both unregistering it from the investigation file and removing its folder with the underlying file structure.
    let remove (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =
        unregister arcConfiguration assayArgs
        delete arcConfiguration assayArgs

    /// Moves an assay file from one study group to another (provided by assayArgs)
    let move (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Assay Move"

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        let assayFileName = IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration |> Option.get

        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs
        let targetStudyIdentifer = getFieldValueByName "TargetStudyIdentifier" assayArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get      
        let investigation = Investigation.fromFile investigationFilePath
        
        match investigation.Studies with
        | Some studies -> 
            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                match study.Assays with
                | Some assays -> 
                    match API.Assay.tryGetByFileName assayFileName assays with
                    | Some assay ->
                
                        let studies = 
                            // Remove Assay from old study
                            API.Study.mapAssays (API.Assay.removeByFileName assayFileName) study
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies

                        match API.Study.tryGetByIdentifier targetStudyIdentifer studies with
                        | Some targetStudy -> 
                            API.Study.mapAssays (fun assays -> API.Assay.add assays assay) targetStudy
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                            |> API.Investigation.setStudies investigation
                        | None -> 
                            if verbosity >= 2 then printfn "Target Study with the identifier %s does not exist in the investigation file, creating new study to move assay to" studyIdentifier
                            let info = Study.StudyInfo.create targetStudyIdentifer "" "" "" "" "" []
                            Study.fromParts info [] [] [] [assay] [] []
                            |> API.Study.add studies
                            |> API.Investigation.setStudies investigation
                    | None -> 
                        if verbosity >= 1 then printfn "Assay with the identifier %s does not exist in the study with the identifier %s" assayIdentifier studyIdentifier
                        investigation
                | None -> 
                    if verbosity >= 1 then printfn "The study with the identifier %s does not contain any assays" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"  
            investigation
        |> Investigation.toFile investigationFilePath

    /// Moves an assay file from one study group to another (provided by assayArgs).
    let show (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =
     
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Assay Get"

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFileName = IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration |> Option.get

        let studyIdentifier = 
            match getFieldValueByName "StudyIdentifier" assayArgs with
            | "" -> assayIdentifier
            | s -> 
                if verbosity >= 2 then printfn "No Study Identifier given, use assayIdentifier instead"
                s

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath
        
        match investigation.Studies with
        | Some studies -> 
            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                match study.Assays with
                | Some assays -> 
                    match API.Assay.tryGetByFileName assayFileName assays with
                    | Some assay ->
                        [assay]
                        |> Prompt.serializeXSLXWriterOutput (Assays.toRows None)
                        |> printfn "%s"
                    | None -> 
                        if verbosity >= 1 then printfn "Assay with the identifier %s does not exist in the study with the identifier %s" assayIdentifier studyIdentifier
                | None -> 
                    if verbosity >= 1 then printfn "The study with the identifier %s does not contain any assays" studyIdentifier                   
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"    



    /// Lists all assay identifiers registered in this investigation.
    let list (arcConfiguration : ArcConfiguration) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Assay List"
        
        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get

        let investigation = Investigation.fromFile investigationFilePath

        match investigation.Studies with
        | Some studies -> 
            studies
            |> List.iter (fun study ->
                let studyIdentifier = Option.defaultValue "" study.Identifier
                match study.Assays with
                | Some assays -> 
                    if List.isEmpty assays |> not then
                        printfn "Study: %s" studyIdentifier
                        assays 
                        |> Seq.iter (fun assay -> printfn "--Assay: %s" (Option.defaultValue "" assay.FileName))
                | None -> 
                    if verbosity >= 1 then printfn "The study with the identifier %s does not contain any assays" studyIdentifier   
            )
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"  

    /// Exports an assay to JSON.
    let exportSingleAssay (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start exporting single assay"
        
        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        
        let assayFileName = IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration |> Option.get
        
        let assayFilePath = IsaModelConfiguration.getAssayFilePath assayIdentifier arcConfiguration

        let studyIdentifier = 
            match getFieldValueByName "StudyIdentifier" assayArgs with
            | "" -> assayIdentifier
            | s -> 
                if verbosity >= 2 then printfn "No Study Identifier given, use assayIdentifier instead"
                s
        
        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
                
        let investigation = Investigation.fromFile investigationFilePath

        // Try retrieve given assay from investigation file
        let assayInInvestigation = 
            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Assays with
                    | Some assays -> 
                        match API.Assay.tryGetByFileName assayFileName assays with
                        | Some assay ->
                            Some assay                           
                        | None -> 
                            if verbosity >= 1 then printfn "Assay with the identifier %s does not exist in the study with the identifier %s" assayIdentifier studyIdentifier
                            None
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any assays" studyIdentifier                   
                        None
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    None
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"     
                None

        let persons,assayFromFile =

            if System.IO.File.Exists assayFilePath then
                try
                    let _,_,p,a = AssayFile.Assay.fromFile assayFilePath
                    p, Some a
                with
                | err -> 
                    if verbosity >= 1 then printfn "Assay file \"%s\" could not be read" assayFilePath    
                    [],None
            else
                if verbosity >= 1 then printfn "Assay file \"%s\" does not exist" assayFilePath     
                [],None
        
        let mergedAssay = 
            match assayInInvestigation,assayFromFile with
            | Some ai, Some a -> API.Update.UpdateByExisting.updateRecordType ai a
            | None, Some a -> a
            | Some ai, None -> ai
            | None, None -> failwith "No assay could be retrieved"     
          
          
        if containsFlag "ProcessSequence" assayArgs then

            let output = mergedAssay.ProcessSequence |> Option.defaultValue []

            match tryGetFieldValueByName "Path" assayArgs with
            | Some p -> ArgumentProcessing.serializeToFile p output
            | None -> ()

            System.Console.Write(ArgumentProcessing.serializeToString output)

        else 

            let output = Study.create(Contacts = persons,Assays = [mergedAssay])
     
            match tryGetFieldValueByName "Path" assayArgs with
            | Some p -> ISADotNet.Json.Study.toFile p output
            | None -> ()

            System.Console.Write(ISADotNet.Json.Study.toString output)


    /// Exports all assays to JSON.
    let exportAllAssays (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start exporting all assays"
        
        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath

        let assayIdentifiers = AssayConfiguration.getAssayNames arcConfiguration
        
        let assays =
            assayIdentifiers
            |> Array.toList
            |> List.map (fun assayIdentifier ->

                let assayFileName = IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration |> Option.get
        
                let assayFilePath = IsaModelConfiguration.getAssayFilePath assayIdentifier arcConfiguration

                let studyIdentifier = 
                    match getFieldValueByName "StudyIdentifier" assayArgs with
                    | "" -> assayIdentifier
                    | s -> 
                        if verbosity >= 2 then printfn "No Study Identifier given, use assayIdentifier instead"
                        s
              
                // Try retrieve given assay from investigation file
                let assayInInvestigation = 
                    match investigation.Studies with
                    | Some studies -> 
                        match API.Study.tryGetByIdentifier studyIdentifier studies with
                        | Some study -> 
                            match study.Assays with
                            | Some assays -> 
                                match API.Assay.tryGetByFileName assayFileName assays with
                                | Some assay ->
                                    Some assay                           
                                | None -> 
                                    if verbosity >= 1 then printfn "Assay with the identifier %s does not exist in the study with the identifier %s" assayIdentifier studyIdentifier
                                    None
                            | None -> 
                                if verbosity >= 1 then printfn "The study with the identifier %s does not contain any assays" studyIdentifier                   
                                None
                        | None -> 
                            if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                            None
                    | None -> 
                        if verbosity >= 1 then printfn "The investigation does not contain any studies"     
                        None

                let persons,assayFromFile =

                    if System.IO.File.Exists assayFilePath then
                        try
                            let _,_,p,a = AssayFile.Assay.fromFile assayFilePath
                            p, Some a
                        with
                        | err -> 
                            if verbosity >= 1 then printfn "Assay file \"%s\" could not be read" assayFilePath    
                            [],None
                    else
                        if verbosity >= 1 then printfn "Assay file \"%s\" does not exist" assayFilePath     
                        [],None
        
                let mergedAssay = 
                    match assayInInvestigation,assayFromFile with
                    | Some ai, Some a -> API.Update.UpdateByExisting.updateRecordType ai a
                    | None, Some a -> a
                    | Some ai, None -> ai
                    | None, None -> failwith "No assay could be retrieved"     
            
                Study.create(Contacts = persons, Assays = [mergedAssay])
            )
        
          
        if containsFlag "ProcessSequence" assayArgs then

            let output = 
                assays 
                |> List.collect (fun s -> 
                    s.Assays 
                    |> Option.defaultValue [] 
                    |> List.collect (fun a -> a.ProcessSequence |> Option.defaultValue [])
                )
                                                          
            match tryGetFieldValueByName "Path" assayArgs with
            | Some p -> ArgumentProcessing.serializeToFile p output
            | None -> ()

            System.Console.Write(ArgumentProcessing.serializeToString output)

        else 

            match tryGetFieldValueByName "Path" assayArgs with
            | Some p -> ArgumentProcessing.serializeToFile p assays
            | None -> ()

            System.Console.Write(ArgumentProcessing.serializeToString assays)

    /// Exports one or several assay(s) to JSON.
    let export (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Assay export"

        match tryGetFieldValueByName "AssayIdentifier" assayArgs with
        | Some _ -> exportSingleAssay arcConfiguration assayArgs
        | None -> exportAllAssays arcConfiguration assayArgs


    /// Functions for altering investigation contacts
    module Contacts =

        /// Updates an existing person in this assay with the given person metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Update"

            let updateOption = if containsFlag "ReplaceWithEmptyValues" personArgs then API.Update.UpdateAll else API.Update.UpdateByExisting            

            let lastName    = getFieldValueByName "LastName"    personArgs
            let firstName   = getFieldValueByName "FirstName"   personArgs
            let midInitials = getFieldValueByName "MidInitials" personArgs

            let comments = 
                match tryGetFieldValueByName "ORCID" personArgs with
                | Some orcid    -> [Comment.fromString "Investigation Person ORCID" orcid]
                | None          -> []

            let person = 
                Contacts.fromString
                    lastName
                    firstName
                    midInitials
                    (getFieldValueByName  "Email"                       personArgs)
                    (getFieldValueByName  "Phone"                       personArgs)
                    (getFieldValueByName  "Fax"                         personArgs)
                    (getFieldValueByName  "Address"                     personArgs)
                    (getFieldValueByName  "Affiliation"                 personArgs)
                    (getFieldValueByName  "Roles"                       personArgs)
                    (getFieldValueByName  "RolesTermAccessionNumber"    personArgs)
                    (getFieldValueByName  "RolesTermSourceREF"          personArgs)
                    comments

            let assayIdentifier = getFieldValueByName "AssayIdentifier" personArgs

            let assayFilePath = IsaModelConfiguration.tryGetAssayFilePath assayIdentifier arcConfiguration |> Option.get

            let doc = Spreadsheet.fromFile assayFilePath true
            
            try 
                let persons = MetaData.getPersons "Investigation" doc

                if API.Person.existsByFullName firstName midInitials lastName persons then
                    let newPersons = API.Person.updateByFullName updateOption person persons
                    MetaData.overwriteWithPersons "Investigation" newPersons doc
                else
                    if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the assay with the identifier %s." firstName midInitials lastName assayIdentifier
                    if containsFlag "AddIfMissing" personArgs then
                        if verbosity >= 1 then printfn "Registering person as AddIfMissing Flag was set." 
                        let newPersons = API.Person.add persons person
                        MetaData.overwriteWithPersons "Investigation" newPersons doc

            finally
                Spreadsheet.close doc


        /// Opens an existing person by fullname (lastName, firstName, MidInitials) in the assay investigation sheet with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Person Edit"

            let editor  = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let lastName    = (getFieldValueByName "LastName"       personArgs)
            let firstName   = (getFieldValueByName "FirstName"      personArgs)
            let midInitials = (getFieldValueByName "MidInitials"    personArgs)

            let assayIdentifier = getFieldValueByName "AssayIdentifier" personArgs

            let assayFilePath = IsaModelConfiguration.tryGetAssayFilePath assayIdentifier arcConfiguration |> Option.get

            let doc = Spreadsheet.fromFile assayFilePath true

            try
                let persons = MetaData.getPersons "Investigation" doc

                match API.Person.tryGetByFullName firstName midInitials lastName persons with
                | Some person ->
                    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir
                        (List.singleton >> Contacts.toRows None) 
                        (Contacts.fromRows None 1 >> fun (_,_,_,items) -> items.Head)
                        person
                    |> fun p -> 
                        let newPersons = API.Person.updateBy ((=) person) API.Update.UpdateAll p persons
                        MetaData.overwriteWithPersons "Investigation" newPersons doc
                | None ->
                    if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the assay with the identifier %s." firstName midInitials lastName assayIdentifier

                Spreadsheet.close doc

            finally
                Spreadsheet.close doc


        /// Registers a person in this assay with the given person metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Person Register"

            let lastName    = getFieldValueByName "LastName"    personArgs
            let firstName   = getFieldValueByName "FirstName"   personArgs
            let midInitials = getFieldValueByName "MidInitials" personArgs

            let comments = 
                match tryGetFieldValueByName "ORCID" personArgs with
                | Some orcid    -> [Comment.fromString "Investigation Person ORCID" orcid]
                | None          -> []

            let person = 
                Contacts.fromString
                    lastName
                    firstName
                    midInitials
                    (getFieldValueByName  "Email"                       personArgs)
                    (getFieldValueByName  "Phone"                       personArgs)
                    (getFieldValueByName  "Fax"                         personArgs)
                    (getFieldValueByName  "Address"                     personArgs)
                    (getFieldValueByName  "Affiliation"                 personArgs)
                    (getFieldValueByName  "Roles"                       personArgs)
                    (getFieldValueByName  "RolesTermAccessionNumber"    personArgs)
                    (getFieldValueByName  "RolesTermSourceREF"          personArgs)
                    comments
            
            let assayIdentifier = getFieldValueByName "AssayIdentifier" personArgs
            
            let assayFilePath = IsaModelConfiguration.tryGetAssayFilePath assayIdentifier arcConfiguration |> Option.get
            
            let doc = Spreadsheet.fromFile assayFilePath true

            try
                let persons = MetaData.getPersons "Investigation" doc

                let newPersons = API.Person.add persons person
                MetaData.overwriteWithPersons "Investigation" newPersons doc

            finally
                Spreadsheet.close doc


        /// Removes an existing person by fullname (lastName, firstName, MidInitials) from this assay with the text editor set in globalArgs.
        let unregister (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Person Unregister"

            let lastName    = (getFieldValueByName  "LastName"      personArgs)
            let firstName   = (getFieldValueByName  "FirstName"     personArgs)
            let midInitials = (getFieldValueByName  "MidInitials"   personArgs)

            let assayIdentifier = getFieldValueByName "AssayIdentifier" personArgs

            let assayFilePath = IsaModelConfiguration.tryGetAssayFilePath assayIdentifier arcConfiguration |> Option.get

            let doc = Spreadsheet.fromFile assayFilePath true

            try 
                let persons = MetaData.getPersons "Investigation" doc

                if API.Person.existsByFullName firstName midInitials lastName persons then
                    let newPersons = API.Person.removeByFullName firstName midInitials lastName persons
                    MetaData.overwriteWithPersons "Investigation" newPersons doc
                else
                    if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the assay with the identifier %s." firstName midInitials lastName assayIdentifier

            finally
                Spreadsheet.close doc


        /// Gets an existing person by fullname (lastName, firstName, MidInitials) and prints their metadata.
        let show (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =
  
            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Person Get"

            let lastName    = (getFieldValueByName  "LastName"      personArgs)
            let firstName   = (getFieldValueByName  "FirstName"     personArgs)
            let midInitials = (getFieldValueByName  "MidInitials"   personArgs)

            let assayIdentifier = getFieldValueByName "AssayIdentifier" personArgs
            
            let assayFilePath = IsaModelConfiguration.tryGetAssayFilePath assayIdentifier arcConfiguration |> Option.get
            
            let doc = Spreadsheet.fromFile assayFilePath true
            
            try
                let persons = MetaData.getPersons "Investigation" doc

                match API.Person.tryGetByFullName firstName midInitials lastName persons with
                | Some person ->
                    [person]
                    |> Prompt.serializeXSLXWriterOutput (Contacts.toRows None)
                    |> printfn "%s"
                | None ->
                    printfn "Person with the name %s %s %s does not exist in the assay with the identifier %s." firstName midInitials lastName assayIdentifier

            finally
                Spreadsheet.close doc


        /// Lists the full names of all persons included in this assay's investigation sheet.
        let list (arcConfiguration : ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Person List"

            let assayIdentifiers = AssayConfiguration.getAssayNames arcConfiguration

            if Array.isEmpty assayIdentifiers 
            
            then printfn "No assays found."

            else
                let assayFilePaths = assayIdentifiers |> Array.map (fun ai -> IsaModelConfiguration.tryGetAssayFilePath ai arcConfiguration |> Option.get)

                let docs = assayFilePaths |> Array.map (fun afp -> Spreadsheet.fromFile afp true)

                let allPersons = docs |> Array.map (MetaData.getPersons "Investigation")

                (allPersons, assayIdentifiers)
                ||> Array.iter2 (
                    fun persons aid ->
                        printfn "Assay: %s" aid
                        persons
                        |> Seq.iter (
                            fun person -> 
                                let firstName   = Option.defaultValue "" person.FirstName
                                let midInitials = Option.defaultValue "" person.MidInitials
                                let lastName    = Option.defaultValue "" person.LastName
                                if midInitials = "" 
                                then printfn "--Person: %s %s" firstName lastName
                                else printfn "--Person: %s %s %s" firstName midInitials lastName
                        )
                )