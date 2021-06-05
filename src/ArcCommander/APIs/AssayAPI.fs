namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISADotNet
open ISADotNet.XLSX



/// ArcCommander Assay API functions that get executed by the assay focused subcommand verbs
module AssayAPI =        


    module AssayFolder =
        
        let exists (arcConfiguration:ArcConfiguration) (identifier : string) =
            AssayConfiguration.getFolderPath identifier arcConfiguration
            |> System.IO.Directory.Exists

    module AssayFile =
        
        let exists (arcConfiguration:ArcConfiguration) (identifier : string) =
            IsaModelConfiguration.getAssayFilePath identifier arcConfiguration
            |> System.IO.File.Exists
        
        let create (arcConfiguration:ArcConfiguration) (identifier : string) =
            IsaModelConfiguration.getAssayFilePath identifier arcConfiguration
            |> ISADotNet.XLSX.AssayFile.AssayFile.init "Investigation" identifier

    /// Initializes a new empty assay file and associated folder structure in the arc.
    let init (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Assay Init"

        let name = getFieldValueByName "AssayIdentifier" assayArgs

        if AssayFolder.exists arcConfiguration name then
            if verbosity >= 1 then printfn "Assay folder with identifier %s already exists" name
        else
            AssayConfiguration.getSubFolderPaths name arcConfiguration
            |> Array.iter (System.IO.Directory.CreateDirectory >> ignore)

            AssayFile.create arcConfiguration name 

            AssayConfiguration.getFilePaths name arcConfiguration
            |> Array.iter (System.IO.File.Create >> ignore)


    /// Updates an existing assay file in the arc with the given assay metadata contained in cliArgs.
    let update (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =
        
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
        

    /// Opens an existing assay file in the arc with the text editor set in globalArgs, additionally setting the given assay metadata contained in assayArgs.
    let edit (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =
        
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

        if verbosity >= 1 then printfn "Start Assay Edit"

        let editor = GeneralConfiguration.getEditor arcConfiguration
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
                            (List.singleton >> Assays.writeAssays None) 
                            (Assays.readAssays None 1 >> fun (_,_,_,items) -> items.Head) 
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


    /// Registers an existing assay in the arc's investigation file with the given assay metadata contained in assayArgs.
    let register (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

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
                if verbosity >= 1 then printfn "Study with the identifier %s does not yet exist, creating it now" studyIdentifier
                if StudyAPI.StudyFile.exists arcConfiguration studyIdentifier |> not then
                    StudyAPI.StudyFile.create arcConfiguration studyIdentifier
                let info = Study.StudyInfo.create studyIdentifier "" "" "" "" "" []
                Study.fromParts info [] [] [] [assay] [] []
                |> API.Study.add studies
        | None ->
            if verbosity >= 1 then printfn "Study with the identifier %s does not yet exist, creating it now" studyIdentifier
            if StudyAPI.StudyFile.exists arcConfiguration studyIdentifier |> not then
                StudyAPI.StudyFile.create arcConfiguration studyIdentifier
            let info = Study.StudyInfo.create studyIdentifier "" "" "" "" "" []
            [Study.fromParts info [] [] [] [assay] [] []]
        |> API.Investigation.setStudies investigation
        |> Investigation.toFile investigationFilePath
    
    /// Creates a new assay file and associated folder structure in the arc and registers it in the arc's investigation file with the given assay metadata contained in assayArgs.
    let add (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        init arcConfiguration assayArgs
        register arcConfiguration assayArgs

    /// Unregisters an assay file from the arc's investigation file assay register
    let unregister (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

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
    
    /// Deletes assay folder and underlying file structure of given assay
    let delete (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Assay Delete"

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFolder = 
            AssayConfiguration.tryGetFolderPath assayIdentifier arcConfiguration
            |> Option.get

        if System.IO.Directory.Exists(assayFolder) then
            System.IO.Directory.Delete(assayFolder,true)

    /// Remove an assay from the arc by both unregistering it from the investigation file and removing its folder with the underlying file structure
    let remove (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =
        unregister arcConfiguration assayArgs
        delete arcConfiguration assayArgs

    /// Moves an assay file from one study group to another (provided by assayArgs)
    let move (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

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

    /// Moves an assay file from one study group to another (provided by assayArgs)
    let get (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =
     
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
                        |> Prompt.serializeXSLXWriterOutput (Assays.writeAssays None)
                        |> printfn "%s"
                    | None -> 
                        if verbosity >= 1 then printfn "Assay with the identifier %s does not exist in the study with the identifier %s" assayIdentifier studyIdentifier
                | None -> 
                    if verbosity >= 1 then printfn "The study with the identifier %s does not contain any assays" studyIdentifier                   
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"    



    /// Lists all assay identifiers registered in this investigation
    let list (arcConfiguration:ArcConfiguration) =

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
