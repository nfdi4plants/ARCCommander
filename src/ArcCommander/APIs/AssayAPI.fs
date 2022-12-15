namespace ArcCommander.APIs

open System
open System.IO

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISADotNet
open ISADotNet.XLSX
open ISADotNet.XLSX.AssayFile

open FSharpSpreadsheetML

/// ArcCommander Assay API functions that get executed by the assay focused subcommand verbs
module AssayAPI =

    /// API for working with assay folders.
    module AssayFolder =
        
        /// Checks if an assay folder exists in the ARC.
        let exists (arcConfiguration : ArcConfiguration) (identifier : string) =
            AssayConfiguration.getFolderPath identifier arcConfiguration
            |> System.IO.Directory.Exists

    /// API for working with assay files.
    module AssayFile =
        
        /// Checks if an assay file exists in the ARC.
        let exists (arcConfiguration : ArcConfiguration) (identifier : string) =
            IsaModelConfiguration.getAssayFilePath identifier arcConfiguration
            |> System.IO.File.Exists
        
        /// Creates an assay file from the given assay in the ARC.
        let create (arcConfiguration : ArcConfiguration) assay (identifier : string) =
            IsaModelConfiguration.getAssayFilePath identifier arcConfiguration
            |> ISADotNet.XLSX.AssayFile.Assay.init (Some assay) None identifier

    /// Initializes a new empty assay file and associated folder structure in the ARC.
    let init (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let log = Logging.createLogger "AssayInitLog"
        
        log.Info("Start Assay Init")

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
            log.Error($"Assay folder with identifier {name} already exists.")
        else
            AssayConfiguration.getSubFolderPaths name arcConfiguration
            |> Array.iter (
                Directory.CreateDirectory 
                >> fun dir -> File.Create(Path.Combine(dir.FullName, ".gitkeep")) |> ignore 
            )

            AssayFile.create arcConfiguration assay name 

            AssayConfiguration.getFilePaths name arcConfiguration
            |> Array.iter (File.Create >> ignore)


    /// Updates an existing assay file in the ARC with the given assay metadata contained in cliArgs.
    let update (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =
        
        let log = Logging.createLogger "AssayUpdateLog"

        log.Info("Start Assay Update")

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
                log.Trace("No Study Identifier given, use Assay Identifier instead.")
                s

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath
        
        let assayFilepath = IsaModelConfiguration.tryGetAssayFilePath assayIdentifier arcConfiguration |> Option.get

        let doc = Spreadsheet.fromFile assayFilepath true

        // part that writes assay metadata into the assay file
        try 
            MetaData.overwriteWithAssayInfo "Assay" assay doc
            
        finally
            Spreadsheet.close doc

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
                        let msg = $"Assay with the identifier {assayIdentifier} does not exist in the study with the identifier {studyIdentifier}."
                        if containsFlag "AddIfMissing" assayArgs then 
                            log.Warn($"{msg}")
                            log.Info("Registering assay as AddIfMissing Flag was set.")
                            API.Assay.add assays assay
                            |> API.Study.setAssays study
                        else 
                            log.Error($"{msg}")
                            log.Trace("AddIfMissing argument can be used to register assay with the update command if it is missing.")
                            study
                | None -> 
                    let msg = $"The study with the identifier {studyIdentifier} does not contain any assays."
                    if containsFlag "AddIfMissing" assayArgs then
                        log.Warn($"{msg}")
                        log.Info("Registering assay as AddIfMissing Flag was set.")
                        [assay]
                        |> API.Study.setAssays study
                    else 
                        log.Error($"{msg}")
                        log.Trace("AddIfMissing argument can be used to register assay with the update command if it is missing.")
                        study
                |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                |> API.Investigation.setStudies investigation
            | None -> 
                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
                investigation
        | None -> 
            log.Error("The investigation does not contain any studies.")
            investigation
        |> Investigation.toFile investigationFilePath

    /// Opens an existing assay file in the ARC with the text editor set in globalArgs, additionally setting the given assay metadata contained in assayArgs.
    let edit (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =
        
        let log = Logging.createLogger "AssayEditLog"

        log.Info("Start Assay Edit")

        let editor = GeneralConfiguration.getEditor arcConfiguration

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        
        let assayFileName = 
            IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration
            |> Option.get

        let studyIdentifier = 
            match getFieldValueByName "StudyIdentifier" assayArgs with
            | "" -> assayIdentifier
            | s -> 
                log.Trace("No Study Identifier given, use assayIdentifier instead.")
                s

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath

        let assayFilepath = IsaModelConfiguration.tryGetAssayFilePath assayIdentifier arcConfiguration |> Option.get

        let compareAssayMetadata (assay1 : Assay) (assay2 : Assay) =
            assay1.FileName             = assay2.FileName           &&
            assay1.MeasurementType      = assay2.MeasurementType    &&
            assay1.TechnologyType       = assay2.TechnologyType     &&
            assay1.TechnologyPlatform   = assay2.TechnologyPlatform &&
            assay1.Comments             = assay2.Comments

        // read assay metadata information from assay file
        let _, oldAssayAssayFile = AssayFile.Assay.fromFile assayFilepath

        let getNewAssay oldAssay =
            ArgumentProcessing.Prompt.createIsaItemQuery 
                editor 
                (List.singleton >> Assays.toRows None) 
                (Assays.fromRows None 1 >> fun (_,_,_,items) -> items.Head) 
                oldAssay
        
        // read assay metadata information from investigation file, check with assay metadata from assay file, 
        // update assay metadata in investigation file and return new assay
        let newAssay = 
            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Assays with
                    | Some assays -> 
                        match API.Assay.tryGetByFileName assayFileName assays with
                        | Some oldAssayInvestigationFile -> 
                            // check if assay metadata from assay file and investigation file differ
                            if compareAssayMetadata oldAssayInvestigationFile oldAssayAssayFile |> not then 
                                log.Warn("The assay metadata in the investigation file differs from that in the assay file.")
                            getNewAssay oldAssayAssayFile
                            // update assay metadata in investigation file
                            |> fun a -> 
                                API.Assay.updateBy ((=) oldAssayInvestigationFile) API.Update.UpdateAll a assays
                                |> API.Study.setAssays study
                                |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                                |> API.Investigation.setStudies investigation
                                |> Investigation.toFile investigationFilePath
                                a
                        | None -> 
                            log.Error($"Assay with the identifier {assayIdentifier} does not exist in the study with the identifier {studyIdentifier}. It is advised to register the assay in the investigation file via \"arc a register\".")
                            getNewAssay oldAssayAssayFile
                    | None -> 
                        log.Error($"The study with the identifier {studyIdentifier} does not contain any assays. It is advised to register the assay with the identifier {assayIdentifier} in the investigation file via \"arc a register\".")
                        getNewAssay oldAssayAssayFile
                | None -> 
                    log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file. It is advised to register the assay with the identifier {assayIdentifier} in the investigation file via \"arc a register\".")
                    getNewAssay oldAssayAssayFile
            | None -> 
                log.Error($"The investigation does not contain any studies. It is advised to register the assay with the identifier {assayIdentifier} in the investigation file via \"arc a register\".")
                getNewAssay oldAssayAssayFile

        // part that writes assay metadata into the assay file
        let doc = Spreadsheet.fromFile assayFilepath true
        
        try 
            MetaData.overwriteWithAssayInfo "Investigation" newAssay doc
                    
        finally
            Spreadsheet.close doc


    /// Registers an existing assay in the ARC's investigation file with the given assay metadata contained in the assay file's investigation sheet.
    let register (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let log = Logging.createLogger "AssayRegisterLog"
        
        log.Info("Start Assay Register")

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        
        let assayFileName = IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration |> Option.get
        
        let assayFilePath = IsaModelConfiguration.getAssayFilePath assayIdentifier arcConfiguration

        let _, assay = Assay.fromFile assayFilePath

        let studyIdentifier = 
            match getFieldValueByName "StudyIdentifier" assayArgs with
            | "" -> 
                log.Trace("No Study Identifier given, use assayIdentifier instead.")
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
                    | Some _ ->
                        log.Error($"Assay with the identifier {assayIdentifier} already exists in the investigation file.")
                        assays
                    | None ->
                        API.Assay.add assays assay
                | None ->
                    [assay]
                |> API.Study.setAssays study
                |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
            | None ->
                log.Info($"Study with the identifier {studyIdentifier} does not exist yet, creating it now.")
                if StudyAPI.StudyFile.exists arcConfiguration studyIdentifier |> not then
                    StudyAPI.StudyFile.create arcConfiguration (Study.create(Identifier = studyIdentifier)) studyIdentifier
                let info = Study.StudyInfo.create studyIdentifier "" "" "" "" (IsaModelConfiguration.getStudyFileName studyIdentifier arcConfiguration) []
                Study.fromParts info [] [] [] [assay] [] []
                |> API.Study.add studies
        | None ->
            log.Info($"Study with the identifier {studyIdentifier} does not exist yet, creating it now.")
            if StudyAPI.StudyFile.exists arcConfiguration studyIdentifier |> not then
                StudyAPI.StudyFile.create arcConfiguration (Study.create(Identifier = studyIdentifier)) studyIdentifier
            let info = Study.StudyInfo.create studyIdentifier "" "" "" "" (IsaModelConfiguration.getStudyFileName studyIdentifier arcConfiguration) []
            [Study.fromParts info [] [] [] [assay] [] []]
        |> API.Investigation.setStudies investigation
        |> Investigation.toFile investigationFilePath
    
    /// Creates a new assay file and associated folder structure in the ARC and registers it in the ARC's investigation file with the given assay metadata contained in assayArgs.
    let add (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        init arcConfiguration assayArgs
        register arcConfiguration assayArgs

    /// Unregisters an assay file from the ARC's investigation file.
    let unregister (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let log = Logging.createLogger "AssayUnregisterLog"
        
        log.Info("Start Assay Unregister")

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFileName = IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration |> Option.get

        let studyIdentifier = 
            match getFieldValueByName "StudyIdentifier" assayArgs with
            | "" -> assayIdentifier
            | s -> 
                log.Trace("No Study Identifier given, use assayIdentifier instead.")
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
                    | Some _ ->
                        API.Assay.removeByFileName assayFileName assays
                        |> API.Study.setAssays study
                        |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                        |> API.Investigation.setStudies investigation
                    | None ->
                        log.Error($"Assay with the identifier {assayIdentifier} does not exist in the study with the identifier {studyIdentifier}.")
                        investigation
                | None -> 
                    log.Error($"The study with the identifier {studyIdentifier} does not contain any assays.")
                    investigation
            | None -> 
                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
                investigation
        | None -> 
            log.Error($"The investigation does not contain any studies.")
            investigation
        |> Investigation.toFile investigationFilePath
    
    /// Deletes an assay's folder and underlying file structure from the ARC.
    let delete (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let log = Logging.createLogger "AssayDeleteLog"

        log.Info("Start Assay Delete")

        let isForced = (containsFlag "Force" assayArgs)

        let identifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFolderPath = AssayConfiguration.getFolderPath identifier arcConfiguration

        /// Standard files that should be always present in an assay.
        let standard = [|
            IsaModelConfiguration.getAssayFilePath identifier arcConfiguration
            |> Path.truncateFolderPath identifier
            yield!
                AssayConfiguration.getFilePaths identifier arcConfiguration
                |> Array.map (Path.truncateFolderPath identifier)
            yield!
                AssayConfiguration.getSubFolderPaths identifier arcConfiguration
                |> Array.map (
                    fun p -> Path.Combine(p, ".gitkeep")
                    >> Path.truncateFolderPath identifier
                )
        |]

        /// Actual files found.
        let allFiles =
            Directory.GetFiles(assayFolderPath, "*", SearchOption.AllDirectories)
            |> Array.map (Path.truncateFolderPath identifier)

        /// A check if there are no files in the folder that are not standard.
        let isStandard = Array.forall (fun t -> Array.contains t standard) allFiles

        match isForced, isStandard with
        | true, _
        | false, true ->
            try Directory.Delete(assayFolderPath, true) with
            | err -> log.Error($"Cannot delete assay:\n {err.ToString()}")
        | _ ->
            log.Error "Assay contains user-specific files. Deletion aborted."
            log.Info "Run the command with `--force` to force deletion."


    /// Remove an assay from the ARC by both unregistering it from the investigation file and removing its folder with the underlying file structure.
    let remove (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =
        unregister arcConfiguration assayArgs
        delete arcConfiguration assayArgs

    /// Moves an assay file from one study group to another (provided by assayArgs).
    let move (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let log = Logging.createLogger "AssayMoveLog"
        
        log.Info("Start Assay Move")

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
                            log.Trace($"Target Study with the identifier {studyIdentifier} does not exist in the investigation file, creating new study to move assay to.")
                            let info = Study.StudyInfo.create targetStudyIdentifer "" "" "" "" (IsaModelConfiguration.getStudyFileName studyIdentifier arcConfiguration) []
                            Study.fromParts info [] [] [] [assay] [] []
                            |> API.Study.add studies
                            |> API.Investigation.setStudies investigation
                    | None -> 
                        log.Error($"Assay with the identifier {assayIdentifier} does not exist in the study with the identifier {studyIdentifier}.")
                        investigation
                | None -> 
                    log.Error($"The study with the identifier {studyIdentifier} does not contain any assays.")
                    investigation
            | None -> 
                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
                investigation
        | None -> 
            log.Error($"The investigation does not contain any studies.")
            investigation
        |> Investigation.toFile investigationFilePath

    /// Moves an assay file from one study group to another (provided by assayArgs).
    let show (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =
     
        let log = Logging.createLogger "AssayShowLog"
        
        log.Info("Start Assay Get")

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFileName = IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration |> Option.get

        let studyIdentifier = 
            match getFieldValueByName "StudyIdentifier" assayArgs with
            | "" -> assayIdentifier
            | s -> 
                log.Trace("No Study Identifier given, use assayIdentifier instead.")
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
                        |> log.Debug
                    | None -> 
                        log.Error($"Assay with the identifier {assayIdentifier} does not exist in the study with the identifier {studyIdentifier}.")
                | None -> 
                    log.Error($"The study with the identifier {studyIdentifier} does not contain any assays.")
            | None -> 
                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
        | None -> 
            log.Error("The investigation does not contain any studies.")



    /// Lists all assay identifiers registered in this investigation.
    let list (arcConfiguration : ArcConfiguration) =

        let log = Logging.createLogger "AssayListLog"
        
        log.Info("Start Assay List")
        
        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get

        let investigation = Investigation.fromFile investigationFilePath

        let assayFolderIdentifiers = AssayConfiguration.getAssayNames arcConfiguration |> set

        let assayIdentifiers,studies = 
            investigation.Studies
            |> Option.defaultValue []
            |> List.choose (fun s ->
                let studyIdentifier = 
                    if s.Identifier.IsNone then
                        log.Warn("Study does not have identifier.")
                        ""
                    else 
                        s.Identifier.Value
               
                match s.Assays with
                | None | Some [] -> 
                    log.Warn($"Study {studyIdentifier} does not contain assays.")
                    None
                | Some assays ->
                    (
                    studyIdentifier,
                    assays 
                    |> List.choose (fun a ->
                        match a.FileName with
                        | None | Some "" -> 
                            log.Warn("Assay does not have filename.")
                            None
                        | Some filename ->
                            match IsaModelConfiguration.tryGetAssayIdentifierOfFileName filename arcConfiguration with
                            | Some identifier -> 
                                Some identifier

                            | None -> 
                                log.Error($"Could not parse assay filename {filename} to obtain identifier. Check formatting.")
                                None
                    ))
                    |> Some
            )
            |> fun studies ->
                List.collect (fun (s,assays) -> assays) studies |> set,
                studies

        
        let onlyRegistered = Set.difference assayIdentifiers assayFolderIdentifiers
        let onlyInitialized = Set.difference assayFolderIdentifiers assayIdentifiers 
        let combined = Set.union assayIdentifiers assayFolderIdentifiers

        if not onlyRegistered.IsEmpty then
            log.Warn("The ARC contains following registered assays that have no associated folders:")
            onlyRegistered
            |> Seq.iter ((sprintf "WARN: %s") >> log.Warn) 
            log.Info($"You can init the assay folder using \"arc a init\".")

        if not onlyInitialized.IsEmpty then
            log.Warn("The ARC contains assay folders with the following identifiers not registered in the investigation:")
            onlyInitialized
            |> Seq.iter ((sprintf "WARN: %s") >> log.Warn) 
            log.Info($"You can register the assay using \"arc a register\".")

        if combined.IsEmpty then
            log.Error("The ARC does not contain any assays.")

        studies
        |> List.iter (fun (studyIdentifier,assays) ->

            log.Debug($"Study: {studyIdentifier}")
            assays 
            |> Seq.iter (fun assayIdentifier -> log.Debug(sprintf "--Assay: %s" assayIdentifier))
        )
        if not onlyInitialized.IsEmpty then
            log.Debug($"Unregistered")
            onlyInitialized 
            |> Seq.iter (fun assayIdentifier -> log.Debug(sprintf "--Assay: %s" assayIdentifier))


    /// Exports an assay to JSON.
    let exportSingleAssay (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let log = Logging.createLogger "AssayExportSingleAssayLog"
        
        log.Info("Start exporting single assay")
        
        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        
        let assayFileName = IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration |> Option.get
        
        let assayFilePath = IsaModelConfiguration.getAssayFilePath assayIdentifier arcConfiguration

        let studyIdentifier = 
            match getFieldValueByName "StudyIdentifier" assayArgs with
            | "" -> assayIdentifier
            | s -> 
                log.Trace("No Study Identifier given, use assayIdentifier instead.")
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
                            log.Error($"Assay with the identifier {assayIdentifier} does not exist in the study with the identifier {studyIdentifier}.")
                            None
                    | None -> 
                        log.Error($"The study with the identifier {studyIdentifier} does not contain any assays.")
                        None
                | None -> 
                    log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
                    None
            | None -> 
                log.Error("The investigation does not contain any studies.")
                None

        let persons,assayFromFile =

            if System.IO.File.Exists assayFilePath then
                try
                    let p, a = AssayFile.Assay.fromFile assayFilePath
                    p, Some a
                with
                | err -> 
                    log.Error(sprintf "Assay file \"%s\" could not be read:\n %s" assayFilePath (err.ToString()))
                    [], None
            else
                log.Error(sprintf "Assay file \"%s\" does not exist." assayFilePath)
                [], None
        
        let mergedAssay = 
            match assayInInvestigation,assayFromFile with
            | Some ai, Some a -> API.Update.UpdateByExisting.updateRecordType ai a
            | None, Some a -> a
            | Some ai, None -> ai
            | None, None -> log.Fatal("No assay could be retrieved."); raise (Exception(""))
          
          
        if containsFlag "ProcessSequence" assayArgs then

            let output = mergedAssay.ProcessSequence |> Option.defaultValue []

            match tryGetFieldValueByName "Output" assayArgs with
            | Some p -> ArgumentProcessing.serializeToFile p output
            | None -> ()

            log.Debug(ArgumentProcessing.serializeToString output)

        else 

            let output = Study.create(Contacts = persons,Assays = [mergedAssay])
     
            match tryGetFieldValueByName "Output" assayArgs with
            | Some p -> ISADotNet.Json.Study.toFile p output
            | None -> ()

            log.Debug(ISADotNet.Json.Study.toString output)


    /// Exports all assays to JSON.
    let exportAllAssays (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let log = Logging.createLogger "AssayExportAllAssaysLog"
        
        log.Info("Start exporting all assays")
        
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
                        log.Trace("No Study Identifier given, use assayIdentifier instead.")
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
                                    log.Error($"Assay with the identifier {assayIdentifier} does not exist in the study with the identifier {studyIdentifier}.")
                                    None
                            | None -> 
                                log.Error($"The study with the identifier {studyIdentifier} does not contain any assays.")
                                None
                        | None -> 
                            log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
                            None
                    | None -> 
                        log.Error("The investigation does not contain any studies.")
                        None

                let persons,assayFromFile =

                    if System.IO.File.Exists assayFilePath then
                        try
                            let p,a = AssayFile.Assay.fromFile assayFilePath
                            p, Some a
                        with
                        | err -> 
                            log.Error(sprintf "Assay file \"%s\" could not be read:\n %s" assayFilePath (err.ToString()))
                            [], None
                    else
                        log.Error(sprintf "Assay file \"%s\" does not exist." assayFilePath)
                        [], None
        
                let mergedAssay = 
                    match assayInInvestigation,assayFromFile with
                    | Some ai, Some a -> API.Update.UpdateByExisting.updateRecordType ai a
                    | None, Some a -> a
                    | Some ai, None -> ai
                    | None, None -> log.Fatal("No assay could be retrieved."); raise (Exception(""))
            
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
                                                          
            match tryGetFieldValueByName "Output" assayArgs with
            | Some p -> ArgumentProcessing.serializeToFile p output
            | None -> ()

            System.Console.Write(ArgumentProcessing.serializeToString output)

        else 

            match tryGetFieldValueByName "Output" assayArgs with
            | Some p -> ArgumentProcessing.serializeToFile p assays
            | None -> ()

            System.Console.Write(ArgumentProcessing.serializeToString assays)

    /// Exports one or several assay(s) to JSON.
    let export (arcConfiguration : ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let log = Logging.createLogger "AssayExportLog"
        
        log.Info("Start Assay export")

        match tryGetFieldValueByName "AssayIdentifier" assayArgs with
        | Some _ -> exportSingleAssay arcConfiguration assayArgs
        | None -> exportAllAssays arcConfiguration assayArgs


    /// Functions for altering investigation contacts
    module Contacts =

        /// Updates an existing person in this assay with the given person metadata contained in cliArgs.
        let update (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let log = Logging.createLogger "AssayContactsUpdateLog"

            log.Info("Start Person Update")

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
                    let msg = $"Person with the name {firstName} {midInitials} {lastName} does not exist in the assay with the identifier {assayIdentifier}."
                    if containsFlag "AddIfMissing" personArgs then
                        log.Warn($"{msg}")
                        log.Info("Registering person as AddIfMissing Flag was set.")
                        let newPersons = API.Person.add persons person
                        MetaData.overwriteWithPersons "Investigation" newPersons doc
                    else log.Error($"{msg}")

            finally
                Spreadsheet.close doc


        /// Opens an existing person by fullname (lastName, firstName, MidInitials) in the assay investigation sheet with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let log = Logging.createLogger "AssayContactsEditLog"
            
            log.Info("Start Person Edit")

            let editor  = GeneralConfiguration.getEditor arcConfiguration

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
                    ArgumentProcessing.Prompt.createIsaItemQuery editor
                        (List.singleton >> Contacts.toRows None) 
                        (Contacts.fromRows None 1 >> fun (_,_,_,items) -> items.Head)
                        person
                    |> fun p -> 
                        let newPersons = API.Person.updateBy ((=) person) API.Update.UpdateAll p persons
                        MetaData.overwriteWithPersons "Investigation" newPersons doc
                | None ->
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the assay with the identifier {assayIdentifier}.")

                Spreadsheet.close doc

            finally
                Spreadsheet.close doc


        /// Registers a person in this assay with the given person metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let log = Logging.createLogger "AssayContactsRegisterLog"
            
            log.Info("Start Person Register")

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

            let log = Logging.createLogger "AssayContactsUnregisterLog"
            
            log.Info("Start Person Unregister")

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
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the assay with the identifier {assayIdentifier}.")

            finally
                Spreadsheet.close doc


        /// Gets an existing person by fullname (lastName, firstName, MidInitials) and prints their metadata.
        let show (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =
  
            let log = Logging.createLogger "AssayContactsShowLog"
            
            log.Info("Start Person Get")

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
                    |> log.Debug
                | None ->
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the assay with the identifier {assayIdentifier}.")

            finally
                Spreadsheet.close doc


        /// Lists the full names of all persons included in this assay's investigation sheet.
        let list (arcConfiguration : ArcConfiguration) = 

            let log = Logging.createLogger "AssayContactsListLog"
            
            log.Info("Start Person List")

            let assayIdentifiers = AssayConfiguration.getAssayNames arcConfiguration

            if Array.isEmpty assayIdentifiers 
            
            then log.Debug("No assays found.")

            else
                let assayFilePaths = assayIdentifiers |> Array.map (fun ai -> IsaModelConfiguration.tryGetAssayFilePath ai arcConfiguration |> Option.get)

                let docs = assayFilePaths |> Array.map (fun afp -> Spreadsheet.fromFile afp true)

                let allPersons = docs |> Array.map (MetaData.getPersons "Investigation")

                (allPersons, assayIdentifiers)
                ||> Array.iter2 (
                    fun persons aid ->
                        log.Debug($"Assay: {aid}")
                        persons
                        |> Seq.iter (
                            fun person -> 
                                let firstName   = Option.defaultValue "" person.FirstName
                                let midInitials = Option.defaultValue "" person.MidInitials
                                let lastName    = Option.defaultValue "" person.LastName
                                if midInitials = "" 
                                then log.Debug($"--Person: {firstName} {lastName}")
                                else log.Debug($"--Person: {firstName} {midInitials} {lastName}")
                        )
                )