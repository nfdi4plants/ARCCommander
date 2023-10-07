namespace ArcCommander.APIs

open System
open System.IO

open ArcCommander
open ArcCommander.ArgumentProcessing

open ARCtrl
open ARCtrl.NET
open ARCtrl.ISA
open ARCtrl.ISA.Spreadsheet
open ArcCommander.CLIArguments


open FsSpreadsheet.ExcelIO



/// ArcCommander Assay API functions that get executed by the assay focused subcommand verbs
module AssayAPI =

    type ArcAssay with
        member this.UpdateTopLevelInfo(other : ArcAssay, replaceWithEmptyValues : bool) =
            if other.TechnologyType.IsSome || replaceWithEmptyValues then this.TechnologyType <- other.TechnologyType
            if other.TechnologyPlatform.IsSome || replaceWithEmptyValues then this.TechnologyPlatform <- other.TechnologyPlatform
            if other.MeasurementType.IsSome || replaceWithEmptyValues then this.MeasurementType <- other.MeasurementType


    type ArcInvestigation with

        member this.ContainsAssay(assayIdentifier : string) =
            this.AssayIdentifiers |> Array.contains assayIdentifier

        member this.TryGetAssay(assayIdentifier : string) =
            if this.ContainsAssay assayIdentifier then 
                Some (this.GetAssay assayIdentifier)
            else
                None

    /// Initializes a new empty assay file and associated folder structure in the ARC.
    let init (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayInitArgs>) =

        let log = Logging.createLogger "AssayInitLog"
        
        log.Info("Start Assay Init")

        let assayIdentifier = assayArgs.GetFieldValue  AssayInitArgs.AssayIdentifier
        
        let assayFileName = Identifier.Assay.fileNameFromIdentifier assayIdentifier

        let mt = 
            OntologyAnnotation.fromString(
                ?termName = (assayArgs.TryGetFieldValue AssayInitArgs.MeasurementType),
                ?tan = (assayArgs.TryGetFieldValue AssayInitArgs.MeasurementTypeTermAccessionNumber),
                ?tsr = (assayArgs.TryGetFieldValue AssayInitArgs.MeasurementTypeTermSourceREF)
                    )
            |> Aux.Option.fromValueWithDefault OntologyAnnotation.empty
        let tt = 
            OntologyAnnotation.fromString(
                ?termName = (assayArgs.TryGetFieldValue AssayInitArgs.TechnologyType),
                ?tan = (assayArgs.TryGetFieldValue AssayInitArgs.TechnologyTypeTermAccessionNumber),
                ?tsr = (assayArgs.TryGetFieldValue AssayInitArgs.TechnologyTypeTermSourceREF)
                    )
            |> Aux.Option.fromValueWithDefault OntologyAnnotation.empty
        let tp = 
            assayArgs.TryGetFieldValue AssayInitArgs.TechnologyPlatform
            |> Option.map ArcAssay.decomposeTechnologyPlatform
            
        let assay = 
            ArcAssay.create(assayIdentifier, ?measurementType = mt,?technologyType = tt, ?technologyPlatform = tp)
        
        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        if isa.AssayIdentifiers |> Array.contains assayIdentifier then
            log.Error($"Assay with identifier {assayIdentifier} already exists.")
        else
        isa.AddAssay(assay)
        arc.ISA <- Some isa
        arc.Write(arcConfiguration)

    /// Updates an existing assay file in the ARC with the given assay metadata contained in cliArgs.
    let update (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayUpdateArgs>) =
        
        let log = Logging.createLogger "AssayUpdateLog"

        log.Info("Start Assay Update")

        let replaceWithEmptyValues = assayArgs.ContainsFlag AssayUpdateArgs.ReplaceWithEmptyValues |> not
        let addIfMissing = assayArgs.ContainsFlag AssayUpdateArgs.AddIfMissing
        
        let assayIdentifier = assayArgs.GetFieldValue  AssayUpdateArgs.AssayIdentifier
        
        let assayFileName = Identifier.Assay.fileNameFromIdentifier assayIdentifier

        let assay = 
            Assays.fromString
                (assayArgs.TryGetFieldValue  AssayUpdateArgs.MeasurementType)
                (assayArgs.TryGetFieldValue  AssayUpdateArgs.MeasurementTypeTermAccessionNumber)
                (assayArgs.TryGetFieldValue  AssayUpdateArgs.MeasurementTypeTermSourceREF)
                (assayArgs.TryGetFieldValue  AssayUpdateArgs.TechnologyType)
                (assayArgs.TryGetFieldValue  AssayUpdateArgs.TechnologyTypeTermAccessionNumber)
                (assayArgs.TryGetFieldValue  AssayUpdateArgs.TechnologyTypeTermSourceREF)
                (assayArgs.TryGetFieldValue  AssayUpdateArgs.TechnologyPlatform)
                assayFileName
                [||]
   
        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        let msg = $"Assay with the identifier {assayIdentifier} does not exist."
        try 
            let a = isa.GetAssay assayIdentifier
            a.UpdateTopLevelInfo(assay,replaceWithEmptyValues)
        with 
        | _ when addIfMissing ->
            log.Warn($"{msg}")
            log.Info("Registering assay as AddIfMissing Flag was set.")
            isa.AddAssay(assay)
        | _ -> 
            log.Error($"{msg}")
            log.Trace("AddIfMissing argument can be used to register assay with the update command if it is missing.")

        arc.ISA <- Some isa
        arc.Write(arcConfiguration)

    /// Opens an existing assay file in the ARC with the text editor set in globalArgs, additionally setting the given assay metadata contained in assayArgs.
    let edit (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayEditArgs>) =
        
        let log = Logging.createLogger "AssayEditLog"

        log.Info("Start Assay Edit")

        let editor = GeneralConfiguration.getEditor arcConfiguration

        let assayIdentifier = assayArgs.GetFieldValue AssayEditArgs.AssayIdentifier
        
        let getNewAssay oldAssay =
            ArgumentProcessing.Prompt.createIsaItemQuery 
                editor 
                (List.singleton >> Assays.toRows None) 
                (Assays.fromRows None 1 >> fun (_,_,_,items) -> items.Head) 
                oldAssay

        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        try 
            let assay = isa.GetAssay assayIdentifier
            let newAssay = getNewAssay assay
            assay.UpdateTopLevelInfo(newAssay,true)
        with
        | _ ->
            log.Error($"Assay with the identifier {assayIdentifier} does not exist.")

        arc.ISA <- Some isa
        arc.Write(arcConfiguration)


    /// Registers an existing assay in the ARC's investigation file with the given assay metadata contained in the assay file's investigation sheet.
    let register (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayRegisterArgs>) =

        let log = Logging.createLogger "AssayRegisterLog"
        
        log.Info("Start Assay Register")

        let assayIdentifier = assayArgs.GetFieldValue AssayRegisterArgs.AssayIdentifier
                
        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        let studyIdentifier = 
            match assayArgs.TryGetFieldValue AssayRegisterArgs.StudyIdentifier with
            | None -> 
                log.Trace("No Study Identifier given, use assayIdentifier instead.")
                assayIdentifier
            | Some s -> s

        let s = 
            if isa.StudyIdentifiers |> Seq.contains studyIdentifier then
                isa.GetStudy studyIdentifier
            else
                log.Info($"Study with the identifier {studyIdentifier} does not exist yet, creating it now.")
                isa.AddRegisteredStudy (ArcStudy(studyIdentifier))
                isa.GetStudy studyIdentifier

        s.RegisterAssay assayIdentifier

        arc.ISA <- Some isa
        arc.Write(arcConfiguration)
    
    /// Creates a new assay file and associated folder structure in the ARC and registers it in the ARC's investigation file with the given assay metadata contained in assayArgs.
    let add (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayAddArgs>) =

        init arcConfiguration (assayArgs.Cast<AssayInitArgs>())
        register arcConfiguration (assayArgs.Cast<AssayRegisterArgs>())


    /// Unregisters an assay file from the ARC's investigation file.
    let unregister (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayUnregisterArgs>) =

        let log = Logging.createLogger "AssayUnregisterLog"
        
        log.Info("Start Assay Unregister")

        let assayIdentifier = assayArgs.GetFieldValue AssayUnregisterArgs.AssayIdentifier

        let studyIdentifier = 
            match assayArgs.TryGetFieldValue AssayUnregisterArgs.StudyIdentifier with
            | None -> 
                log.Trace("No Study Identifier given, use assayIdentifier instead.")
                assayIdentifier
            | Some s -> s

        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))


        if isa.StudyIdentifiers |> Seq.contains studyIdentifier then
            isa.GetStudy studyIdentifier
            |> fun s -> s.DeregisterAssay assayIdentifier
        else
            log.Error($"Study with the identifier {studyIdentifier} does not exist.")
        
        arc.ISA <- Some isa
        arc.Write(arcConfiguration)
    
    /// Deletes an assay's folder and underlying file structure from the ARC.
    let delete (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayDeleteArgs>) =

        let log = Logging.createLogger "AssayDeleteLog"

        log.Info("Start Assay Delete")

        let isForced = assayArgs.ContainsFlag AssayDeleteArgs.Force

        let identifier = assayArgs.GetFieldValue AssayDeleteArgs.AssayIdentifier

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
    let remove (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayRemoveArgs>) =
        unregister arcConfiguration (assayArgs.Cast<AssayUnregisterArgs>())
        delete arcConfiguration (assayArgs.Cast<AssayDeleteArgs>())

    /// Moves an assay file from one study group to another (provided by assayArgs).
    let move (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayMoveArgs>) =

        let log = Logging.createLogger "AssayMoveLog"
        
        log.Info("Start Assay Move")

        let assayIdentifier = assayArgs.GetFieldValue AssayMoveArgs.AssayIdentifier

        let studyIdentifier = assayArgs.GetFieldValue AssayMoveArgs.StudyIdentifier
        let targetStudyIdentifer = assayArgs.GetFieldValue AssayMoveArgs.TargetStudyIdentifier

        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))


        if isa.StudyIdentifiers |> Seq.contains studyIdentifier then
            let s = isa.GetStudy studyIdentifier
            s.DeregisterAssay assayIdentifier
              
        let s = 
            if isa.StudyIdentifiers |> Seq.contains targetStudyIdentifer then
                isa.GetStudy targetStudyIdentifer
            else
                log.Info($"Study with the identifier {targetStudyIdentifer} does not exist yet, creating it now.")
                isa.AddRegisteredStudy (ArcStudy(targetStudyIdentifer))
                isa.GetStudy targetStudyIdentifer

        s.RegisterAssay assayIdentifier

        arc.ISA <- Some isa
        arc.Write(arcConfiguration)

    /// Moves an assay file from one study group to another (provided by assayArgs).
    let show (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayShowArgs>) =
     
        let log = Logging.createLogger "AssayShowLog"
        
        log.Info("Start Assay Get")

        let assayIdentifier = assayArgs.GetFieldValue AssayShowArgs.AssayIdentifier

        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        try 
            isa.GetAssay assayIdentifier 
            |> List.singleton
            |> Prompt.serializeXSLXWriterOutput (Assays.toRows None)
            |> log.Debug
        with 
        | _ -> log.Error($"Assay with the identifier {assayIdentifier} does not exist in the arc.")
              

    /// Lists all assay identifiers registered in this investigation.
    let list (arcConfiguration : ArcConfiguration) =

        let log = Logging.createLogger "AssayListLog"
        
        log.Info("Start Assay List")
        
        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        let studies = 
            isa.RegisteredStudies
            |> Seq.map (fun s -> s.Identifier, s.RegisteredAssayIdentifiers)
            
        let unregistered = isa.AssayIdentifiers |> Seq.except (studies |> Seq.collect snd) |> Seq.toList

        studies
        |> Seq.iter (fun (studyIdentifier,assayIdentifiers) ->
            log.Debug($"Study: {studyIdentifier}")
            assayIdentifiers 
            |> Seq.iter (fun assayIdentifier -> log.Debug(sprintf "--Assay: %s" assayIdentifier))
        )
        if not unregistered.IsEmpty then
            log.Debug($"Unregistered")
            unregistered 
            |> Seq.iter (fun assayIdentifier -> log.Debug(sprintf "--Assay: %s" assayIdentifier))


    /// Exports an assay to JSON.
    let exportSingleAssay (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayExportArgs>) =

        let log = Logging.createLogger "AssayExportSingleAssayLog"
        
        log.Info("Start exporting single assay")
        
        let assayIdentifier = assayArgs.GetFieldValue AssayExportArgs.AssayIdentifier
        
        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        if isa.ContainsAssay assayIdentifier then
            
            let a = isa.GetAssay assayIdentifier

            let output = 

                if assayArgs.ContainsFlag AssayExportArgs.ProcessSequence then               
                    a.Tables |> Seq.collect (fun t -> t.GetProcesses())
                    |> Seq.toList
                    |> ARCtrl.ISA.Json.ProcessSequence.toJsonString
                else 
                    Study.create(Contacts = (a.Performers |> Array.toList),Assays = [a.ToAssay()])
                    |> ARCtrl.ISA.Json.Study.toJsonString

            match assayArgs.TryGetFieldValue AssayExportArgs.Output with
            | Some p -> System.IO.File.WriteAllText(p, output)
            | None -> ()

            log.Debug(output)

        else 
            log.Error($"Assay with the identifier {assayIdentifier} does not exist in the arc.")

    /// Exports all assays to JSON.
    let exportAllAssays (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayExportArgs>) =

        let log = Logging.createLogger "AssayExportAllAssaysLog"
        
        log.Info("Start exporting all assays")
        
        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        if isa.AssayCount > 0 then
            
            let ass = isa.Assays

            let output = 

                if assayArgs.ContainsFlag AssayExportArgs.ProcessSequence then               
                    ass
                    |> Seq.collect (fun a -> a.Tables |> Seq.collect (fun t -> t.GetProcesses()))
                    |> Seq.toList
                    |> ARCtrl.ISA.Json.ProcessSequence.toJsonString
                else 
                    ass
                    |> Seq.map (fun a -> 
                        Study.create(Contacts = (a.Performers |> Array.toList),Assays = [a.ToAssay()])
                        |> ARCtrl.ISA.Json.Study.encoder (ARCtrl.ISA.Json.ConverterOptions())
                        
                    )
                    |> Seq.toArray
                    |> Thoth.Json.Net.Encode.array
                    |> Thoth.Json.Net.Encode.toString 2

            match assayArgs.TryGetFieldValue AssayExportArgs.Output with
            | Some p -> System.IO.File.WriteAllText(p, output)
            | None -> ()

            log.Debug(output)

        else 
            log.Error($"Arc contains no assays.")

    /// Exports one or several assay(s) to JSON.
    let export (arcConfiguration : ArcConfiguration) (assayArgs : ArcParseResults<AssayExportArgs>) =

        let log = Logging.createLogger "AssayExportLog"
        
        log.Info("Start Assay export")

        match assayArgs.TryGetFieldValue AssayExportArgs.AssayIdentifier with
        | Some _ -> exportSingleAssay arcConfiguration assayArgs
        | None -> exportAllAssays arcConfiguration assayArgs


    /// Functions for altering investigation contacts
    module Contacts =

        open ArcCommander.CLIArguments.AssayContacts

        /// Updates an existing person in this assay with the given person metadata contained in cliArgs.
        let update (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonUpdateArgs>) =

            let log = Logging.createLogger "AssayContactsUpdateLog"

            log.Info("Start Person Update")

            let updateOption = if personArgs.ContainsFlag PersonUpdateArgs.ReplaceWithEmptyValues then Aux.Update.UpdateAll else Aux.Update.UpdateByExisting            

            let lastName    = personArgs.GetFieldValue PersonUpdateArgs.LastName
            let firstName   = personArgs.GetFieldValue PersonUpdateArgs.FirstName
            let midInitials = personArgs.TryGetFieldValue PersonUpdateArgs.MidInitials

            let orcid = personArgs.TryGetFieldValue PersonUpdateArgs.ORCID

            let person = 
                Contacts.fromString
                    (Some lastName)
                    (Some firstName)
                    midInitials
                    (personArgs.TryGetFieldValue PersonUpdateArgs.Email)
                    (personArgs.TryGetFieldValue PersonUpdateArgs.Phone)
                    (personArgs.TryGetFieldValue PersonUpdateArgs.Fax)
                    (personArgs.TryGetFieldValue PersonUpdateArgs.Address)
                    (personArgs.TryGetFieldValue PersonUpdateArgs.Affiliation)
                    (personArgs.TryGetFieldValue PersonUpdateArgs.Roles                    |> Option.defaultValue "")
                    (personArgs.TryGetFieldValue PersonUpdateArgs.RolesTermAccessionNumber |> Option.defaultValue "")
                    (personArgs.TryGetFieldValue PersonUpdateArgs.RolesTermSourceREF       |> Option.defaultValue "")
                    [||]
                |> fun c -> {c with ORCID = orcid}

            let assayIdentifier = personArgs.GetFieldValue PersonUpdateArgs.AssayIdentifier

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsAssay assayIdentifier then
                let a = isa.GetAssay assayIdentifier
                let newPersons = 
                    if Person.existsByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Performers then
                        Person.updateByFullName updateOption person a.Performers                   
                    else
                        let msg = $"Person with the name {firstName} {midInitials} {lastName} does not exist in the assay with the identifier {assayIdentifier}."
                        if personArgs.ContainsFlag PersonUpdateArgs.AddIfMissing then
                            log.Warn($"{msg}")
                            log.Info("Registering person as AddIfMissing Flag was set.")
                            Array.append a.Performers [|person|]
                        else 
                            log.Error(msg)
                            a.Performers
                a.Performers <- newPersons               
            else 
                log.Error($"Assay with identifier {assayIdentifier} does not exist in the arc")

            arc.ISA <- Some isa
            arc.Write(arcConfiguration)


        /// Opens an existing person by fullname (lastName, firstName, MidInitials) in the assay investigation sheet with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonEditArgs>) =

            let log = Logging.createLogger "AssayContactsEditLog"
            
            log.Info("Start Person Edit")

            let editor  = GeneralConfiguration.getEditor arcConfiguration

            let lastName    = personArgs.GetFieldValue PersonEditArgs.LastName
            let firstName   = personArgs.GetFieldValue PersonEditArgs.FirstName
            let midInitials = personArgs.TryGetFieldValue PersonEditArgs.MidInitials


            let assayIdentifier = personArgs.GetFieldValue PersonEditArgs.AssayIdentifier

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsAssay assayIdentifier then
                let a = isa.GetAssay assayIdentifier

                match Person.tryGetByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Performers with
                | Some person ->
                    ArgumentProcessing.Prompt.createIsaItemQuery editor
                        (List.singleton >> Contacts.toRows None) 
                        (Contacts.fromRows None 1 >> fun (_,_,_,items) -> items.Head)
                        person
                    |> fun p -> 
                        let newPersons = Person.updateByFullName Aux.Update.UpdateAll p a.Performers
                        a.Performers <- newPersons     
                | None ->
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the assay with the identifier {assayIdentifier}.")      
            else 
                log.Error($"Assay with identifier {assayIdentifier} does not exist in the arc")
            
            arc.ISA <- Some isa
            arc.Write(arcConfiguration)

        /// Registers a person in this assay with the given person metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonRegisterArgs>) =

            let log = Logging.createLogger "AssayContactsRegisterLog"
            
            log.Info("Start Person Register")

            let assayIdentifier = personArgs.GetFieldValue PersonRegisterArgs.AssayIdentifier

            let lastName    = personArgs.GetFieldValue PersonRegisterArgs.LastName   
            let firstName   = personArgs.GetFieldValue PersonRegisterArgs.FirstName  
            let midInitials = personArgs.TryGetFieldValue PersonRegisterArgs.MidInitials

            let orcid = personArgs.TryGetFieldValue PersonRegisterArgs.ORCID

            let person = 
                Contacts.fromString
                    (Some lastName)
                    (Some firstName)
                    midInitials
                    (personArgs.TryGetFieldValue PersonRegisterArgs.Email)
                    (personArgs.TryGetFieldValue PersonRegisterArgs.Phone)
                    (personArgs.TryGetFieldValue PersonRegisterArgs.Fax)
                    (personArgs.TryGetFieldValue PersonRegisterArgs.Address)
                    (personArgs.TryGetFieldValue PersonRegisterArgs.Affiliation)
                    (personArgs.TryGetFieldValue PersonRegisterArgs.Roles                    |> Option.defaultValue "")
                    (personArgs.TryGetFieldValue PersonRegisterArgs.RolesTermAccessionNumber |> Option.defaultValue "")
                    (personArgs.TryGetFieldValue PersonRegisterArgs.RolesTermSourceREF       |> Option.defaultValue "")
                    [||]
                |> fun c -> {c with ORCID = orcid}
                       
            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsAssay assayIdentifier then
                let a = isa.GetAssay assayIdentifier
                if Person.existsByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Performers then
                    log.Error $"Person with the name {firstName} {midInitials} {lastName} does already exist in the assay with the identifier {assayIdentifier}."
                else
                    let newPersons = Array.append a.Performers [|person|]
                    a.Performers <- newPersons               
            else 
                log.Error($"Assay with identifier {assayIdentifier} does not exist in the arc")

            arc.ISA <- Some isa
            arc.Write(arcConfiguration)


        /// Removes an existing person by fullname (lastName, firstName, MidInitials) from this assay with the text editor set in globalArgs.
        let unregister (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonUnregisterArgs>) =

            let log = Logging.createLogger "AssayContactsUnregisterLog"
            
            log.Info("Start Person Unregister")

            let assayIdentifier = personArgs.GetFieldValue PersonUnregisterArgs.AssayIdentifier

            let lastName    = personArgs.GetFieldValue PersonUnregisterArgs.LastName
            let firstName   = personArgs.GetFieldValue PersonUnregisterArgs.FirstName
            let midInitials = personArgs.TryGetFieldValue PersonUnregisterArgs.MidInitials

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsAssay assayIdentifier then
                let a = isa.GetAssay assayIdentifier
                match Person.tryGetByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Performers with
                | Some person ->               
                    let newPersons = Person.removeByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Performers
                    a.Performers <- newPersons     
                | None ->
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the assay with the identifier {assayIdentifier}.")      
            else 
                log.Error($"Assay with identifier {assayIdentifier} does not exist in the arc")

            arc.ISA <- Some isa
            arc.Write(arcConfiguration)

        /// Gets an existing person by fullname (lastName, firstName, MidInitials) and prints their metadata.
        let show (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonShowArgs>) =
  
            let log = Logging.createLogger "AssayContactsShowLog"
            
            log.Info("Start Person Get")
 
            let assayIdentifier = personArgs.GetFieldValue PersonShowArgs.AssayIdentifier

            let lastName    = personArgs.GetFieldValue PersonShowArgs.LastName
            let firstName   = personArgs.GetFieldValue PersonShowArgs.FirstName
            let midInitials = personArgs.TryGetFieldValue PersonShowArgs.MidInitials

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsAssay assayIdentifier then
                let a = isa.GetAssay assayIdentifier
                match Person.tryGetByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Performers with
                | Some person ->
                    [person]
                    |> Prompt.serializeXSLXWriterOutput (Contacts.toRows None)
                    |> log.Debug
                | None ->
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the assay with the identifier {assayIdentifier}.")      
            else 
                log.Error($"Assay with identifier {assayIdentifier} does not exist in the arc")


        /// Lists the full names of all persons included in this assay's investigation sheet.
        let list (arcConfiguration : ArcConfiguration) = 

            let log = Logging.createLogger "AssayContactsListLog"
            
            log.Info("Start Person List")

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            isa.Assays
            |> Seq.iter (fun a ->
                log.Debug($"Assay: {a.Identifier}")
                a.Performers
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
