namespace ArcCommander.APIs

open ArcCommander
open ArcCommander.ArgumentProcessing

open System
open System.IO
open ARCtrl.ISA
open ARCtrl.NET
open ArcCommander.CLIArguments
open ARCtrl
open ARCtrl.ISA.Spreadsheet

/// ArcCommander Study API functions that get executed by the study focused subcommand verbs.
module StudyAPI =    
    
    type ArcStudy with
        member this.UpdateTopLevelInfo(other : ArcStudy, replaceWithEmptyValues : bool) =
            if other.Title.IsSome || replaceWithEmptyValues then this.Title <- other.Title
            if other.Description.IsSome || replaceWithEmptyValues then this.Description <- other.Description
            if other.SubmissionDate.IsSome || replaceWithEmptyValues then this.SubmissionDate <- other.SubmissionDate
            if other.PublicReleaseDate.IsSome || replaceWithEmptyValues then this.PublicReleaseDate <- other.PublicReleaseDate

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
            
    /// Initializes a new empty study file in the ARC.
    let init (arcConfiguration : ArcConfiguration) (studyArgs : ArcParseResults<StudyInitArgs>) = 
            
        let log = Logging.createLogger "StudyInitLog"
        
        log.Info("Start Study Init")

        let identifier = studyArgs.GetFieldValue StudyInitArgs.Identifier

        let study = 
            ArcStudy.create(
                identifier,
                ?title = studyArgs.TryGetFieldValue StudyInitArgs.Title,
                ?description = studyArgs.TryGetFieldValue StudyInitArgs.Description,
                ?submissionDate = studyArgs.TryGetFieldValue StudyInitArgs.SubmissionDate,
                ?publicReleaseDate = studyArgs.TryGetFieldValue StudyInitArgs.PublicReleaseDate
                )

        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        if isa.StudyIdentifiers |> Seq.contains identifier then
            log.Error($"Study with identifier {identifier} already exists.")
        
        isa.AddStudy(study)
        arc.ISA <- Some isa
        arc.Write(arcConfiguration)

    /// Updates an existing study info in the ARC with the given study metadata contained in cliArgs.
    let update (arcConfiguration : ArcConfiguration) (studyArgs : ArcParseResults<StudyUpdateArgs>) =
    
        let log = Logging.createLogger "StudyUpdateLog"
        
        log.Info("Start Study Update")

        let replaceWithEmptyValues = studyArgs.ContainsFlag StudyUpdateArgs.ReplaceWithEmptyValues |> not
        let addIfMissing = studyArgs.ContainsFlag StudyUpdateArgs.AddIfMissing

        let identifier = studyArgs.GetFieldValue StudyUpdateArgs.Identifier

        let study = 
            ArcStudy.create(
                identifier,
                ?title = studyArgs.TryGetFieldValue StudyUpdateArgs.Title,
                ?description = studyArgs.TryGetFieldValue StudyUpdateArgs.Description,
                ?submissionDate = studyArgs.TryGetFieldValue StudyUpdateArgs.SubmissionDate,
                ?publicReleaseDate = studyArgs.TryGetFieldValue StudyUpdateArgs.PublicReleaseDate
                )

        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        let msg = $"Study with the identifier {identifier} does not exist."
        match isa.TryGetStudy identifier with
        | Some s ->
            
            s.UpdateTopLevelInfo(study,replaceWithEmptyValues)        
        | None when addIfMissing ->
            log.Warn($"{msg}")
            log.Info("Registering study as AddIfMissing Flag was set.")
            isa.AddStudy(study)
        | None -> 
            log.Error($"{msg}")
            log.Trace("AddIfMissing argument can be used to register study with the update command if it is missing.")

        arc.ISA <- Some isa
        arc.Write(arcConfiguration)        

    // /// Opens an existing study file in the ARC with the text editor set in globalArgs, additionally setting the given study metadata contained in cliArgs.
    /// Opens the existing study info in the ARC with the text editor set in globalArgs.
    let edit (arcConfiguration : ArcConfiguration) (studyArgs : ArcParseResults<StudyEditArgs>) = 

        let log = Logging.createLogger "StudyEditLog"
        
        log.Info("Start Study Edit")

        let editor = GeneralConfiguration.getEditor arcConfiguration

        let studyIdentifier = studyArgs.GetFieldValue StudyEditArgs.Identifier
        
        let getNewStudy oldStudy =
            ArgumentProcessing.Prompt.createIsaItemQuery 
                editor 
                (ISA.Spreadsheet.Studies.StudyInfo.toRows) 
                (ISA.Spreadsheet.Studies.fromRows 1 >> fun (_,_,_,items) -> items.Value |> fst) 
                oldStudy

        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        match isa.TryGetStudy studyIdentifier with 
        | Some study ->
            let newStudy = getNewStudy study
            study.UpdateTopLevelInfo(newStudy,true)      
        | None ->
            log.Error($"Study with the identifier {studyIdentifier} does not exist.")

        arc.ISA <- Some isa
        arc.Write(arcConfiguration)

    /// Registers an existing study in the ARC's investigation file with the given study metadata contained in cliArgs.
    let register (arcConfiguration : ArcConfiguration) (studyArgs : ArcParseResults<StudyRegisterArgs>) =

        let log = Logging.createLogger "StudyRegisterLog"
        
        log.Info("Start Study Register")

        let identifier = studyArgs.GetFieldValue StudyRegisterArgs.Identifier

        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        if isa.RegisteredStudyIdentifiers |> Seq.contains identifier then
            log.Error($"Study with identifier {identifier} is already registered.")
        else
        isa.RegisterStudy(identifier)
        arc.ISA <- Some isa
        arc.Write(arcConfiguration)

    /// Creates a new study file in the ARC and registers it in the ARC's investigation file with the given study metadata contained in cliArgs.
    let add (arcConfiguration : ArcConfiguration) (studyArgs : ArcParseResults<StudyAddArgs>) = 
        init arcConfiguration (studyArgs.Cast<StudyInitArgs>())
        register arcConfiguration (studyArgs.Cast<StudyRegisterArgs>())

    /// Deletes a study's folder and underlying file structure from the ARC.
    let delete (arcConfiguration : ArcConfiguration) (studyArgs : ArcParseResults<StudyDeleteArgs>) = 
    
        let log = Logging.createLogger "StudyDeleteLog"
        
        log.Info("Start Study Delete")

        let isForced = studyArgs.ContainsFlag StudyDeleteArgs.Force

        let identifier = studyArgs.GetFieldValue StudyDeleteArgs.Identifier

        let studyFolderPath = StudyConfiguration.getFolderPath identifier arcConfiguration

        /// Standard files that should always be present in a study.
        let standard = [|
            IsaModelConfiguration.getStudyFilePath identifier arcConfiguration 
            |> Path.truncateFolderPath identifier
            yield!
                StudyConfiguration.getFilePaths identifier arcConfiguration 
                |> Array.map (Path.truncateFolderPath identifier)
            yield!
                StudyConfiguration.getSubFolderPaths identifier arcConfiguration
                |> Array.map (
                    fun p -> Path.Combine(p, ".gitkeep")
                    >> Path.truncateFolderPath identifier
                )
        |]

        /// Actual files found.
        let allFiles =
            Directory.GetFiles(studyFolderPath, "*", SearchOption.AllDirectories)
            |> Array.map (Path.truncateFolderPath identifier)
        
        /// A check if there are no files in the folder that are not standard.
        let isStandard = Array.forall (fun t -> Array.contains t standard) allFiles

        match isForced, isStandard with
        | true, _
        | false, true ->
            try Directory.Delete(studyFolderPath, true) with
            | err -> log.Error($"Cannot delete study:\n {err.ToString()}")
        | _ ->
            log.Error "Study contains user-specific files. Deletion aborted."
            log.Info "Run the command with `--force` to force deletion."
            

    /// Unregisters an existing study from the ARC's investigation file.
    let unregister (arcConfiguration : ArcConfiguration) (studyArgs : ArcParseResults<StudyUnregisterArgs>) =

        let log = Logging.createLogger "StudyUnregisterLog"
        
        log.Info("Start Study Unregister")

        let identifier = studyArgs.GetFieldValue StudyUnregisterArgs.Identifier

        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        if isa.RegisteredStudyIdentifiers |> Seq.contains identifier then
            isa.DeregisterStudy(identifier) |> ignore
            arc.ISA <- Some isa
            arc.Write(arcConfiguration)
        else
            log.Error($"Study with identifier {identifier} is not registered.")
        

    /// Removes a study file from the ARC and unregisters it from the investigation file.
    let remove (arcConfiguration : ArcConfiguration) (studyArgs : ArcParseResults<StudyRemoveArgs>) = 
        delete arcConfiguration (studyArgs.Cast<StudyDeleteArgs>())
        unregister arcConfiguration (studyArgs.Cast<StudyUnregisterArgs>())

    /// Lists all study identifiers registered in this ARC's investigation file.
    let show (arcConfiguration : ArcConfiguration) (studyArgs : ArcParseResults<StudyShowArgs>) =

        let log = Logging.createLogger "StudyShowLog"
        
        log.Info("Start Study Show")
        
        let identifier = studyArgs.GetFieldValue StudyShowArgs.Identifier

        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        match isa.TryGetStudy identifier with
        | Some study -> 
            study
            |> Prompt.serializeXSLXWriterOutput ISA.Spreadsheet.Studies.StudyInfo.toRows
            |> log.Debug       
        | None -> 
            log.Error($"Study with identifier {identifier} does not exist.")

    /// Lists all study identifiers registered in this ARC's investigation file.
    let list (arcConfiguration : ArcConfiguration) =
        
        let log = Logging.createLogger "StudyListLog"
        
        let arc = ARC.load(arcConfiguration)
        let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

        let registered = 
            isa.RegisteredStudyIdentifiers
            
        let unregistered = isa.StudyIdentifiers |> Seq.except registered |> Seq.toList

        registered
        |> Seq.iter (fun (studyIdentifier) ->
            log.Debug($"Study: {studyIdentifier}")           
        )
        if not unregistered.IsEmpty then
            log.Debug($"Unregistered")
            unregistered 
            |> Seq.iter (fun studyIdentifier -> log.Debug(sprintf "Study: %s" studyIdentifier))


    /// Functions for altering investigation contacts
    module Contacts =

        open ArcCommander.CLIArguments.StudyContacts

        /// Updates an existing person in this study with the given person metadata contained in cliArgs.
        let update (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonUpdateArgs>) =

            let log = Logging.createLogger "StudyContactsUpdateLog"

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

            let studyIdentifier = personArgs.GetFieldValue PersonUpdateArgs.StudyIdentifier

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsStudy studyIdentifier then
                let a = isa.GetStudy studyIdentifier
                let newPersons = 
                    if Person.existsByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Contacts then
                        Person.updateByFullName updateOption person a.Contacts                   
                    else
                        let msg = $"Person with the name {firstName} {midInitials} {lastName} does not exist in the study with the identifier {studyIdentifier}."
                        if personArgs.ContainsFlag PersonUpdateArgs.AddIfMissing then
                            log.Warn($"{msg}")
                            log.Info("Registering person as AddIfMissing Flag was set.")
                            Array.append a.Contacts [|person|]
                        else 
                            log.Error(msg)
                            a.Contacts
                a.Contacts <- newPersons               
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")

            arc.ISA <- Some isa
            arc.Write(arcConfiguration)


        /// Opens an existing person by fullname (lastName, firstName, MidInitials) in the study investigation sheet with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonEditArgs>) =

            let log = Logging.createLogger "StudyContactsEditLog"
            
            log.Info("Start Person Edit")

            let editor  = GeneralConfiguration.getEditor arcConfiguration

            let lastName    = personArgs.GetFieldValue PersonEditArgs.LastName
            let firstName   = personArgs.GetFieldValue PersonEditArgs.FirstName
            let midInitials = personArgs.TryGetFieldValue PersonEditArgs.MidInitials


            let studyIdentifier = personArgs.GetFieldValue PersonEditArgs.StudyIdentifier

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsStudy studyIdentifier then
                let a = isa.GetStudy studyIdentifier

                match Person.tryGetByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Contacts with
                | Some person ->
                    ArgumentProcessing.Prompt.createIsaItemQuery editor
                        (List.singleton >> Contacts.toRows None) 
                        (Contacts.fromRows None 1 >> fun (_,_,_,items) -> items.Head)
                        person
                    |> fun p -> 
                        let newPersons = Person.updateByFullName Aux.Update.UpdateAll p a.Contacts
                        a.Contacts <- newPersons     
                | None ->
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the study with the identifier {studyIdentifier}.")      
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")
            
            arc.ISA <- Some isa
            arc.Write(arcConfiguration)

        /// Registers a person in this study with the given person metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonRegisterArgs>) =

            let log = Logging.createLogger "StudyContactsRegisterLog"
            
            log.Info("Start Person Register")

            let studyIdentifier = personArgs.GetFieldValue PersonRegisterArgs.StudyIdentifier

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

            if isa.ContainsStudy studyIdentifier then
                let a = isa.GetStudy studyIdentifier
                if Person.existsByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Contacts then
                    log.Error $"Person with the name {firstName} {midInitials} {lastName} does already exist in the study with the identifier {studyIdentifier}."
                else
                    let newPersons = Array.append a.Contacts [|person|]
                    a.Contacts <- newPersons               
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")

            arc.ISA <- Some isa
            arc.Write(arcConfiguration)


        /// Removes an existing person by fullname (lastName, firstName, MidInitials) from this study with the text editor set in globalArgs.
        let unregister (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonUnregisterArgs>) =

            let log = Logging.createLogger "StudyContactsUnregisterLog"
            
            log.Info("Start Person Unregister")

            let studyIdentifier = personArgs.GetFieldValue PersonUnregisterArgs.StudyIdentifier

            let lastName    = personArgs.GetFieldValue PersonUnregisterArgs.LastName
            let firstName   = personArgs.GetFieldValue PersonUnregisterArgs.FirstName
            let midInitials = personArgs.TryGetFieldValue PersonUnregisterArgs.MidInitials

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsStudy studyIdentifier then
                let a = isa.GetStudy studyIdentifier
                match Person.tryGetByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Contacts with
                | Some person ->               
                    let newPersons = Person.removeByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Contacts
                    a.Contacts <- newPersons     
                | None ->
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the study with the identifier {studyIdentifier}.")      
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")

            arc.ISA <- Some isa
            arc.Write(arcConfiguration)

        /// Gets an existing person by fullname (lastName, firstName, MidInitials) and prints their metadata.
        let show (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonShowArgs>) =
  
            let log = Logging.createLogger "StudyContactsShowLog"
            
            log.Info("Start Person Get")
 
            let studyIdentifier = personArgs.GetFieldValue PersonShowArgs.StudyIdentifier

            let lastName    = personArgs.GetFieldValue PersonShowArgs.LastName
            let firstName   = personArgs.GetFieldValue PersonShowArgs.FirstName
            let midInitials = personArgs.TryGetFieldValue PersonShowArgs.MidInitials

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsStudy studyIdentifier then
                let a = isa.GetStudy studyIdentifier
                match Person.tryGetByFullName firstName (midInitials |> Option.defaultValue "") lastName a.Contacts with
                | Some person ->
                    [person]
                    |> Prompt.serializeXSLXWriterOutput (Contacts.toRows None)
                    |> log.Debug
                | None ->
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the study with the identifier {studyIdentifier}.")      
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")


        /// Lists the full names of all persons included in this study's investigation sheet.
        let list (arcConfiguration : ArcConfiguration) = 

            let log = Logging.createLogger "StudyContactsListLog"
            
            log.Info("Start Person List")

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            isa.Studies
            |> Seq.iter (fun a ->
                log.Debug($"Study: {a.Identifier}")
                a.Contacts
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

    /// Functions for altering investigation Publications.
    module Publications =

        open ArcCommander.CLIArguments.StudyPublications

        module Publication =
            let updateByDOI updateOption publication publications =
                Publication.updateByDOI updateOption publication (publications |> Array.toList)
                |> List.toArray

            let existsByDOI doi publications =               
                Publication.existsByDoi doi (publications |> Array.toList)

            let tryGetByDOI doi publications =
                Publication.tryGetByDoi doi (publications |> Array.toList)

            let removeByDOI doi publications =
                Publication.removeByDoi doi (publications |> Array.toList)
                |> List.toArray

        /// Updates an existing publication in the ARC investigation study with the given publication metadata contained in cliArgs.
        let update (arcConfiguration : ArcConfiguration) (publicationArgs : ArcParseResults<PublicationUpdateArgs>) =

            let log = Logging.createLogger "StudyPublicationsUpdateLog"
            
            log.Info("Start Publication update")

            let updateOption = if publicationArgs.ContainsFlag PublicationUpdateArgs.ReplaceWithEmptyValues then Aux.Update.UpdateAll else Aux.Update.UpdateByExisting

            let doi = publicationArgs.GetFieldValue PublicationUpdateArgs.DOI

            let publication =
                 Publications.fromString
                     (publicationArgs.TryGetFieldValue PublicationUpdateArgs.PubMedID)
                     (Some doi)
                     (publicationArgs.TryGetFieldValue PublicationUpdateArgs.AuthorList)
                     (publicationArgs.TryGetFieldValue PublicationUpdateArgs.Title)
                     (publicationArgs.TryGetFieldValue PublicationUpdateArgs.Status)
                     (publicationArgs.TryGetFieldValue PublicationUpdateArgs.StatusTermSourceREF)
                     (publicationArgs.TryGetFieldValue PublicationUpdateArgs.StatusTermAccessionNumber)
                     [||]

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            let studyIdentifier = publicationArgs.GetFieldValue PublicationUpdateArgs.StudyIdentifier

            if isa.ContainsStudy studyIdentifier then
                let a = isa.GetStudy studyIdentifier
                let newPublications = 
                    if Publication.existsByDOI doi a.Publications then
                        Publication.updateByDOI updateOption publication a.Publications           
                    else
                        let msg = $"Publication with the doi {doi} does not exist in the study with the identifier {studyIdentifier}."
                        if publicationArgs.ContainsFlag PublicationUpdateArgs.AddIfMissing then
                            log.Warn($"{msg}")
                            log.Info("Registering publciation as AddIfMissing Flag was set.")
                            Array.append a.Publications [|publication|]
                        else 
                            log.Error(msg)
                            a.Publications
                a.Publications <- newPublications               
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")

            arc.ISA <- Some isa
            arc.Write(arcConfiguration)
        
        /// Opens an existing publication by DOI in the ARC investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (publicationArgs : ArcParseResults<PublicationEditArgs>) =

            let log = Logging.createLogger "StudyPublicationsEditLog"
            
            log.Info("Start Publication Edit")

            let editor = GeneralConfiguration.getEditor arcConfiguration

            let doi = publicationArgs.GetFieldValue PublicationEditArgs.DOI

            let studyIdentifier = publicationArgs.GetFieldValue PublicationEditArgs.StudyIdentifier

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsStudy studyIdentifier then
                let s = isa.GetStudy studyIdentifier

                match Publication.tryGetByDOI doi s.Publications with
                | Some publication ->
                    ArgumentProcessing.Prompt.createIsaItemQuery editor
                        (List.singleton >> Publications.toRows None) 
                        (Publications.fromRows None 1 >> fun (_,_,_,items) -> items.Head)
                        publication
                    |> fun p -> 
                        let newPublications = Publication.updateByDOI Aux.Update.UpdateAll p s.Publications
                        s.Publications <- newPublications 
                | None ->
                    log.Error($"Publication with the doi {doi} does not exist in the study with the identifier {studyIdentifier}.")      
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")
            
            arc.ISA <- Some isa
            arc.Write(arcConfiguration)

        /// Registers a publication in the ARC investigation study with the given publication metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (publicationArgs : ArcParseResults<PublicationRegisterArgs>) =

            let log = Logging.createLogger "StudyPublicationsRegisterLog"
            
            log.Info("Start Publication Register")

            let doi = publicationArgs.GetFieldValue PublicationRegisterArgs.DOI

            let publication =
                 Publications.fromString
                     (publicationArgs.TryGetFieldValue PublicationRegisterArgs.PubMedID)
                     (Some doi)
                     (publicationArgs.TryGetFieldValue PublicationRegisterArgs.AuthorList)
                     (publicationArgs.TryGetFieldValue PublicationRegisterArgs.Title)
                     (publicationArgs.TryGetFieldValue PublicationRegisterArgs.Status)
                     (publicationArgs.TryGetFieldValue PublicationRegisterArgs.StatusTermSourceREF)
                     (publicationArgs.TryGetFieldValue PublicationRegisterArgs.StatusTermAccessionNumber)
                     [||]

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            let studyIdentifier = publicationArgs.GetFieldValue PublicationRegisterArgs.StudyIdentifier

            if isa.ContainsStudy studyIdentifier then
                let s = isa.GetStudy studyIdentifier
                let newPublications = 
                    if Publication.existsByDOI doi s.Publications then
                        let msg = $"Publication with the doi {doi} already exists in the study with the identifier {studyIdentifier}."                       
                        log.Error(msg)
                        s.Publications
                    else
                        Array.append s.Publications [|publication|]
                s.Publications <- newPublications               
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")

            arc.ISA <- Some isa
            arc.Write(arcConfiguration)

        /// Opens an existing publication by DOI in the ARC investigation study with the text editor set in globalArgs.
        let unregister (arcConfiguration : ArcConfiguration) (publicationArgs : ArcParseResults<PublicationUnregisterArgs>) =

            let log = Logging.createLogger "StudyPublicationsUnregisterLog"
            
            log.Info("Start Publication Unregister")
            
            let doi = publicationArgs.GetFieldValue PublicationUnregisterArgs.DOI

            let studyIdentifier = publicationArgs.GetFieldValue PublicationUnregisterArgs.StudyIdentifier

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsStudy studyIdentifier then
                let s = isa.GetStudy studyIdentifier
                if Publication.existsByDOI doi s.Publications then
                    let newPublications = Publication.removeByDOI doi s.Publications
                    s.Publications <- newPublications 
                else
                    log.Error($"Publication with the doi {doi} does not exist in the study with the identifier {studyIdentifier}.")      
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")


        /// Gets an existing publication by DOI from the ARC investigation study and prints its metadata.
        let show (arcConfiguration : ArcConfiguration) (publicationArgs : ArcParseResults<PublicationShowArgs>) =

            let log = Logging.createLogger "StudyPublicationsShow"
            
            log.Info("Start Publication Show")

            let doi = publicationArgs.GetFieldValue PublicationShowArgs.DOI

            let studyIdentifier = publicationArgs.GetFieldValue PublicationShowArgs.StudyIdentifier

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsStudy studyIdentifier then
                let s = isa.GetStudy studyIdentifier
                match Publication.tryGetByDOI doi s.Publications with
                | Some publication ->
                    [publication]
                    |> Prompt.serializeXSLXWriterOutput (Publications.toRows None)
                    |> log.Debug
                | None ->
                    log.Error($"Publication with the doi {doi} does not exist in the study with the identifier {studyIdentifier}.")      
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")

        /// Lists the DOIs of all publications included in the investigation study.
        let list (arcConfiguration : ArcConfiguration) = 

            let log = Logging.createLogger "StudyPublicationsListLog"
            
            log.Info("Start Publication List")

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            isa.Studies
            |> Seq.iter (fun study ->
                match study.Publications with
                | [||] -> 
                    ()
                | publications -> 
                    log.Debug(sprintf "Study: %s" study.Identifier)
                    publications
                    |> Seq.iter (fun publication -> log.Debug(sprintf "--Publication DOI: %s" (Option.defaultValue "" publication.DOI)))
            )

    /// Functions for altering investigation Designs.
    module Designs =

        open CLIArguments.StudyDesignDescriptors

        module OntologyAnnotation = 

            let updateByName updateOption publication publications =
                OntologyAnnotation.updateByName updateOption publication (publications |> Array.toList)
                |> List.toArray

            let existsByName name publications =               
                OntologyAnnotation.existsByName (AnnotationValue.Text name) (publications |> Array.toList)

            let tryGetByName name publications =
                OntologyAnnotation.tryGetByName (AnnotationValue.Text name) (publications |> Array.toList)

            let removeByName name publications =
                OntologyAnnotation.removeByName (AnnotationValue.Text name) (publications |> Array.toList)
                |> List.toArray

        /// Updates an existing design in the ARC investigation study with the given design metadata contained in cliArgs.
        let update (arcConfiguration : ArcConfiguration) (designArgs : ArcParseResults<DesignUpdateArgs>) =

            let log = Logging.createLogger "StudyDesugnsUpdateLog"
            
            log.Info("Start Design Update")

            let updateOption = if designArgs.ContainsFlag DesignUpdateArgs.ReplaceWithEmptyValues then Aux.Update.UpdateAll else Aux.Update.UpdateByExisting

            let name = designArgs.GetFieldValue DesignUpdateArgs.DesignType

            let design = 
                 OntologyAnnotation.fromString(
                     name,
                     ?tan = (designArgs.TryGetFieldValue DesignUpdateArgs.TypeTermAccessionNumber),
                     ?tsr = (designArgs.TryGetFieldValue DesignUpdateArgs.TypeTermSourceREF)
                    )

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            let studyIdentifier = designArgs.GetFieldValue DesignUpdateArgs.StudyIdentifier

            if isa.ContainsStudy studyIdentifier then
                let s = isa.GetStudy studyIdentifier
                let newDesigns = 
                    if OntologyAnnotation.existsByName name s.StudyDesignDescriptors then
                        OntologyAnnotation.updateByName updateOption design s.StudyDesignDescriptors
                    else
                        let msg = $"Design with the name {name} does not exist in the study with the identifier {studyIdentifier}."
                        if designArgs.ContainsFlag DesignUpdateArgs.AddIfMissing then
                            log.Warn($"{msg}")
                            log.Info("Registering design as AddIfMissing Flag was set.")
                            Array.append s.StudyDesignDescriptors [|design|]
                        else 
                            log.Error($"{msg}")
                            s.StudyDesignDescriptors
                s.StudyDesignDescriptors <- newDesigns
                arc.ISA <- Some isa
                arc.Write(arcConfiguration)
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")
        
        /// Opens an existing design by design type in the ARC investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (designArgs : ArcParseResults<DesignEditArgs>) =

            let log = Logging.createLogger "StudyDesignsEdit"
            
            log.Info("Start Design Edit")

            let editor = GeneralConfiguration.getEditor arcConfiguration

            let name = designArgs.GetFieldValue DesignEditArgs.DesignType

            let studyIdentifier = designArgs.GetFieldValue DesignEditArgs.StudyIdentifier

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsStudy studyIdentifier then
                let s = isa.GetStudy studyIdentifier
                match OntologyAnnotation.tryGetByName name s.StudyDesignDescriptors with
                | Some design ->
                    ArgumentProcessing.Prompt.createIsaItemQuery editor
                        (List.singleton >> DesignDescriptors.toRows None) 
                        (DesignDescriptors.fromRows None 1 >> fun (_,_,_,items) -> items.Head) 
                        design
                    |> fun d -> OntologyAnnotation.updateByName (Aux.Update.UpdateAll) d s.StudyDesignDescriptors
                    |> fun newDesigns -> s.StudyDesignDescriptors <- newDesigns
                    arc.ISA <- Some isa
                    arc.Write(arcConfiguration)
                | None ->
                    log.Error($"Design with the name {name} does not exist in the study with the identifier {studyIdentifier}.")
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")

        /// Registers a design in the ARC investigation study with the given publication metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (designArgs : ArcParseResults<DesignRegisterArgs>) =

            let log = Logging.createLogger "StudyDesignsRegisterLog"
            
            log.Info("Start Design Register")

            let name = designArgs.GetFieldValue DesignRegisterArgs.DesignType

            let studyIdentifier = designArgs.GetFieldValue DesignRegisterArgs.StudyIdentifier

            let design = 
                 OntologyAnnotation.fromString(
                     name,
                     ?tan = (designArgs.TryGetFieldValue DesignRegisterArgs.TypeTermAccessionNumber),
                     ?tsr = (designArgs.TryGetFieldValue DesignRegisterArgs.TypeTermSourceREF)
                    )

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsStudy studyIdentifier then
                let s = isa.GetStudy studyIdentifier
                let newDesigns = 
                    if OntologyAnnotation.existsByName name s.StudyDesignDescriptors then
                        let msg = $"Design with the name {name} already exists in the study with the identifier {studyIdentifier}."                        
                        log.Error($"{msg}")
                        s.StudyDesignDescriptors
                    else
                        Array.append s.StudyDesignDescriptors [|design|]
                s.StudyDesignDescriptors <- newDesigns
                arc.ISA <- Some isa
                arc.Write(arcConfiguration)
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")

        /// Opens an existing design by design type in the ARC investigation study with the text editor set in globalArgs.
        let unregister (arcConfiguration : ArcConfiguration) (designArgs : ArcParseResults<DesignUnregisterArgs>) =
            
            let log = Logging.createLogger "StudyDesignsUnregisterLog"
            
            log.Info("Start Design Unregister")

            let name = designArgs.GetFieldValue DesignUnregisterArgs.DesignType

            let studyIdentifier = designArgs.GetFieldValue DesignUnregisterArgs.StudyIdentifier

            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            if isa.ContainsStudy studyIdentifier then
                let s = isa.GetStudy studyIdentifier
                match OntologyAnnotation.tryGetByName name s.StudyDesignDescriptors with
                | Some design ->
                    let newDesigns = OntologyAnnotation.removeByName name s.StudyDesignDescriptors
                    s.StudyDesignDescriptors <- newDesigns
                    arc.ISA <- Some isa
                    arc.Write(arcConfiguration)
                | None ->
                    log.Error($"Design with the name {name} does not exist in the study with the identifier {studyIdentifier}.")
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")

        /// Gets an existing design by design type from the ARC investigation study and prints its metadata.
        let show (arcConfiguration : ArcConfiguration) (designArgs : ArcParseResults<DesignShowArgs>) =
            let log = Logging.createLogger "StudyDesignsShowLog"
            
            log.Info("Start Design Show")
            let name = designArgs.GetFieldValue DesignShowArgs.DesignType
            let studyIdentifier = designArgs.GetFieldValue DesignShowArgs.StudyIdentifier
            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))
            if isa.ContainsStudy studyIdentifier then
                let s = isa.GetStudy studyIdentifier
                match OntologyAnnotation.tryGetByName name s.StudyDesignDescriptors with
                | Some design ->
                    [design]
                    |> Prompt.serializeXSLXWriterOutput (DesignDescriptors.toRows None)
                    |> log.Debug
                | None ->
                    log.Error($"Design with the name {name} does not exist in the study with the identifier {studyIdentifier}.")
            else 
                log.Error($"Study with identifier {studyIdentifier} does not exist in the arc")
        
        
        /// Lists the designs included in the investigation study.
        let list (arcConfiguration : ArcConfiguration) =

            let log = Logging.createLogger "StudyDesignsListLog"
            
            log.Info("Start Design List")
            let arc = ARC.load(arcConfiguration)
            let isa = arc.ISA |> Option.defaultValue (ArcInvestigation(Identifier.createMissingIdentifier()))

            isa.Studies
            |> Seq.iter (fun study ->
                match study.StudyDesignDescriptors with
                | [||] -> 
                    ()
                | designs -> 
                    log.Debug(sprintf "Study: %s" study.Identifier)
                    designs
                    |> Seq.iter (fun design -> log.Debug(sprintf "--Design Descriptor: %s" design.NameText))
            )

    ///// Functions for altering investigation factors.
    //module Factors =

    //    /// Updates an existing factor in the ARC investigation study with the given factor metadata contained in cliArgs.
    //    let update (arcConfiguration : ArcConfiguration) (factorArgs : Map<string,Argument>) =

    //        let log = Logging.createLogger "StudyFactorsUpdateLog"
            
    //        log.Info("Start Factor Update")

    //        let updateOption = if containsFlag "ReplaceWithEmptyValues" factorArgs then API.Update.UpdateAll else API.Update.UpdateByExisting            

    //        let name = getFieldValueByName "Name" factorArgs

    //        let factor = 
    //             Factors.fromString
    //                name
    //                (getFieldValueByName  "FactorType"                 factorArgs)
    //                (getFieldValueByName  "TypeTermAccessionNumber"    factorArgs)
    //                (getFieldValueByName  "TypeTermSourceREF"          factorArgs)
    //                 []

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath

    //        let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

    //        match investigation.Studies with
    //        | Some studies -> 
    //            match API.Study.tryGetByIdentifier studyIdentifier studies with
    //            | Some study -> 
    //                match study.Factors with
    //                | Some factors -> 
    //                    if API.Factor.existsByName name factors then
    //                        API.Factor.updateByName updateOption factor factors
    //                        |> API.Study.setFactors study
    //                    else
    //                        let msg = $"Factor with the name {name} does not exist in the study with the identifier {studyIdentifier}."
    //                        if containsFlag "AddIfMissing" factorArgs then
    //                            log.Warn($"{msg}")
    //                            log.Info("Registering factor as AddIfMissing Flag was set.")
    //                            API.Factor.add factors factor
    //                            |> API.Study.setFactors study
    //                        else 
    //                            log.Error($"{msg}")
    //                            log.Trace("AddIfMissing argument can be used to register factor with the update command if it is missing.")
    //                            study
    //                | None -> 
    //                    let msg = $"The study with the identifier {studyIdentifier} does not contain any factors."
    //                    if containsFlag "AddIfMissing" factorArgs then
    //                        log.Warn($"{msg}")
    //                        log.Info("Registering factor as AddIfMissing Flag was set.")
    //                        [factor]
    //                        |> API.Study.setFactors study
    //                    else 
    //                        log.Error($"{msg}")
    //                        log.Trace("AddIfMissing argument can be used to register factor with the update command if it is missing.")
    //                        study
    //                |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
    //                |> API.Investigation.setStudies investigation
    //            | None -> 
    //                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
    //                investigation
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")
    //            investigation
    //        |> Investigation.toFile investigationFilePath
        
    //    /// Opens an existing factor by name in the ARC investigation study with the text editor set in globalArgs.
    //    let edit (arcConfiguration : ArcConfiguration) (factorArgs : Map<string,Argument>) =
            
    //        let log = Logging.createLogger "StudyFactorsEditLog"
            
    //        log.Info("Start Factor Edit")

    //        let editor = GeneralConfiguration.getEditor arcConfiguration

    //        let name = getFieldValueByName "Name" factorArgs

    //        let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath
            
    //        match investigation.Studies with
    //        | Some studies -> 
    //            match API.Study.tryGetByIdentifier studyIdentifier studies with
    //            | Some study -> 
    //                match study.Factors with
    //                | Some factors -> 
    //                    match API.Factor.tryGetByName name factors with
    //                    | Some factor ->                    
    //                        ArgumentProcessing.Prompt.createIsaItemQuery editor
    //                            (List.singleton >> Factors.toRows None) 
    //                            (Factors.fromRows None 1 >> fun (_,_,_,items) -> items.Head) 
    //                            factor
    //                        |> fun f -> API.Factor.updateBy ((=) factor) API.Update.UpdateAll f factors
    //                        |> API.Study.setFactors study
    //                        |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
    //                        |> API.Investigation.setStudies investigation
    //                    | None ->
    //                        log.Error($"Factor with the name {name} does not exist in the study with the identifier {studyIdentifier}.")
    //                        investigation
    //                | None -> 
    //                    log.Error($"The study with the identifier {studyIdentifier} does not contain any factors.")
    //                    investigation
    //            | None -> 
    //                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
    //                investigation
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")
    //            investigation
    //        |> Investigation.toFile investigationFilePath


    //    /// Registers a factor in the ARC investigation study with the given factor metadata contained in personArgs.
    //    let register (arcConfiguration : ArcConfiguration) (factorArgs : Map<string,Argument>) =

    //        let log = Logging.createLogger "StudyFactorsRegisterLog"
            
    //        log.Info("Start Factor Register")
            
    //        let name = getFieldValueByName  "Name" factorArgs

    //        let factor = 
    //             Factors.fromString
    //                name
    //                (getFieldValueByName  "FactorType"                 factorArgs)
    //                (getFieldValueByName  "TypeTermAccessionNumber"    factorArgs)
    //                (getFieldValueByName  "TypeTermSourceREF"          factorArgs)
    //                 []
            
    //        let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath

    //        match investigation.Studies with
    //        | Some studies -> 
    //            match API.Study.tryGetByIdentifier studyIdentifier studies with
    //            | Some study -> 
    //                match study.Factors with
    //                | Some factors -> 
    //                    if API.Factor.existsByName name factors then
    //                        log.Error($"Factor with the name {name} already exists in the study with the identifier {studyIdentifier}.")
    //                        factors
    //                    else
    //                        API.Factor.add factors factor
    //                | None -> [factor]
    //                |> API.Study.setFactors study
    //                |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
    //                |> API.Investigation.setStudies investigation
    //            | None ->
    //                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
    //                investigation
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")
    //            investigation
    //        |> Investigation.toFile investigationFilePath

    //    /// Opens an existing factor by name in the ARC investigation study with the text editor set in globalArgs.
    //    let unregister (arcConfiguration : ArcConfiguration) (factorArgs : Map<string,Argument>) =

    //        let log = Logging.createLogger "StudyFactorsUnregisterLog"
            
    //        log.Info("Start Factor Unregister")
            
    //        let name = getFieldValueByName  "Name" factorArgs

    //        let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath

    //        match investigation.Studies with
    //        | Some studies -> 
    //            match API.Study.tryGetByIdentifier studyIdentifier studies with
    //            | Some study -> 
    //                match study.Factors with
    //                | Some factors -> 
    //                    if API.Factor.existsByName name factors then           
    //                        API.Factor.removeByName name factors
    //                        |> API.Study.setFactors study
    //                        |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
    //                        |> API.Investigation.setStudies investigation
    //                    else
    //                        log.Error($"Factor with the name {name} does not exist in the study with the identifier {studyIdentifier}.")
    //                        investigation
    //                | None -> 
    //                    log.Error($"The study with the identifier {studyIdentifier} does not contain any factors.")
    //                    investigation
    //            | None ->
    //                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
    //                investigation
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")
    //            investigation
    //        |> Investigation.toFile investigationFilePath

    //    /// Gets an existing factor by name from the ARC investigation study and prints its metadata.
    //    let show (arcConfiguration : ArcConfiguration) (factorArgs : Map<string,Argument>) =

    //        let log = Logging.createLogger "StudyFactorsShowLog"
            
    //        log.Info("Start Factor Show")

    //        let name = getFieldValueByName  "Name" factorArgs

    //        let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath

    //        match investigation.Studies with
    //        | Some studies -> 
    //            match API.Study.tryGetByIdentifier studyIdentifier studies with
    //            | Some study -> 
    //                match study.Factors with
    //                | Some factors -> 
    //                    match API.Factor.tryGetByName name factors with
    //                    | Some factor ->
    //                        [factor]
    //                        |> Prompt.serializeXSLXWriterOutput (Factors.toRows None)
    //                        |> log.Debug
    //                    | None -> 
    //                        log.Error($"Factor with the DOI {name} does not exist in the study with the identifier {studyIdentifier}.")
    //                | None -> 
    //                    log.Error($"The study with the identifier {studyIdentifier} does not contain any factors.")
    //            | None -> 
    //                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")

    //    /// Lists the factors included in the investigation study.
    //    let list (arcConfiguration : ArcConfiguration) = 
            
    //        let log = Logging.createLogger "StudyFactorsListLog"
            
    //        log.Warn("Start Factor List")

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath

    //        match investigation.Studies with
    //        | Some studies -> 
    //            studies
    //            |> Seq.iter (fun study ->
    //                match study.Factors with
    //                | Some factors -> 
    //                    log.Debug(sprintf "Study: %s" (Option.defaultValue "" study.Identifier))
    //                    factors
    //                    |> Seq.iter (fun factor -> log.Debug(sprintf "--Factor Name: %s" (Option.defaultValue "" factor.Name)))
    //                | None -> ()
    //            )
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")

    /// Functions for altering investigation protocols.
    //module Protocols =
        
    //    open CLIArguments.StudyProtocols

    //    module Protocol =
            

    //    /// Updates an existing protocol in the ARC investigation study with the given protocol metadata contained in cliArgs.
    //    let update (arcConfiguration : ArcConfiguration) (protocolArgs : Map<string,Argument>) =

    //        let log = Logging.createLogger "StudyProtocolsUpdateLog"
            
    //        log.Info("Start Protocol Update")

    //        let updateOption = if containsFlag "ReplaceWithEmptyValues" protocolArgs then API.Update.UpdateAll else API.Update.UpdateByExisting

    //        let name = getFieldValueByName "Name" protocolArgs

    //        let protocol = 
    //             Protocols.fromString
    //                name
    //                (getFieldValueByName "ProtocolType"                         protocolArgs)
    //                (getFieldValueByName "TypeTermAccessionNumber"              protocolArgs)
    //                (getFieldValueByName "TypeTermSourceREF"                    protocolArgs)
    //                (getFieldValueByName "Description"                          protocolArgs)
    //                (getFieldValueByName "URI"                                  protocolArgs)
    //                (getFieldValueByName "Version"                              protocolArgs)
    //                (getFieldValueByName "ParametersName"                       protocolArgs)
    //                (getFieldValueByName "ParametersTermAccessionNumber"        protocolArgs)
    //                (getFieldValueByName "ParametersTermSourceREF"              protocolArgs)
    //                (getFieldValueByName "ComponentsName"                       protocolArgs)
    //                (getFieldValueByName "ComponentsType"                       protocolArgs)
    //                (getFieldValueByName "ComponentsTypeTermAccessionNumber"    protocolArgs)
    //                (getFieldValueByName "ComponentsTypeTermSourceREF"          protocolArgs)
    //                []

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath

    //        let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

    //        match investigation.Studies with
    //        | Some studies -> 
    //            match API.Study.tryGetByIdentifier studyIdentifier studies with
    //            | Some study -> 
    //                match study.Protocols with
    //                | Some protocols -> 
    //                    if API.Protocol.existsByName name protocols then
    //                        API.Protocol.updateByName updateOption protocol protocols
    //                        |> API.Study.setProtocols study
    //                    else
    //                        let msg = $"Protocol with the name {name} does not exist in the study with the identifier {studyIdentifier}."
    //                        if containsFlag "AddIfMissing" protocolArgs then
    //                            log.Warn($"{msg}")
    //                            log.Info("Registering protocol as AddIfMissing Flag was set.")
    //                            API.Protocol.add protocols protocol
    //                            |> API.Study.setProtocols study
    //                        else 
    //                            log.Error($"{msg}")
    //                            log.Trace("AddIfMissing argument can be used to register protocol with the update command if it is missing.")
    //                            study
    //                | None -> 
    //                    let msg = $"The study with the identifier {studyIdentifier} does not contain any protocols."
    //                    if containsFlag "AddIfMissing" protocolArgs then
    //                        log.Warn($"{msg}")
    //                        log.Info("Registering protocol as AddIfMissing Flag was set.")
    //                        [protocol]
    //                        |> API.Study.setProtocols study
    //                    else 
    //                        log.Error($"{msg}")
    //                        log.Trace("AddIfMissing argument can be used to register protocol with the update command if it is missin.g")
    //                        study
    //                |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
    //                |> API.Investigation.setStudies investigation
    //            | None -> 
    //                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
    //                investigation
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")
    //            investigation
    //        |> Investigation.toFile investigationFilePath
        
    //    /// Opens an existing protocol by name in the ARC investigation study with the text editor set in globalArgs.
    //    let edit (arcConfiguration : ArcConfiguration) (protocolArgs : Map<string,Argument>) =

    //        let log = Logging.createLogger "StudyProtocolsEditLog"
            
    //        log.Info("Start Protocol Edit")

    //        let editor = GeneralConfiguration.getEditor arcConfiguration

    //        let name = (getFieldValueByName  "Name" protocolArgs)

    //        let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath
            
    //        match investigation.Studies with
    //        | Some studies -> 
    //            match API.Study.tryGetByIdentifier studyIdentifier studies with
    //            | Some study -> 
    //                match study.Protocols with
    //                | Some protocols -> 
    //                    match API.Protocol.tryGetByName name protocols with
    //                    | Some protocol ->
    //                        ArgumentProcessing.Prompt.createIsaItemQuery editor
    //                            (List.singleton >> Protocols.toRows None) 
    //                            (Protocols.fromRows None 1 >> fun (_,_,_,items) -> items.Head) 
    //                            protocol
    //                        |> fun f -> API.Protocol.updateBy ((=) protocol) API.Update.UpdateAll f protocols
    //                        |> API.Study.setProtocols study
    //                        |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
    //                        |> API.Investigation.setStudies investigation
    //                    | None ->
    //                        log.Error($"Protocol with the name {name} does not exist in the study with the identifier {studyIdentifier}.")
    //                        investigation
    //                | None -> 
    //                    log.Error($"The study with the identifier {studyIdentifier} does not contain any protocols.")
    //                    investigation
    //            | None -> 
    //                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
    //                investigation
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")
    //            investigation
    //        |> Investigation.toFile investigationFilePath


    //    /// Registers a protocol in the ARC investigation study with the given protocol metadata contained in personArgs.
    //    let register (arcConfiguration : ArcConfiguration) (protocolArgs : Map<string,Argument>) =
           
    //        let log = Logging.createLogger "StudyProtocolsRegisterLog"
            
    //        log.Info("Start Protocol Register")
            
    //        let name = getFieldValueByName "Name" protocolArgs

    //        let protocol = 
    //             Protocols.fromString
    //                name
    //                (getFieldValueByName "ProtocolType"                         protocolArgs)
    //                (getFieldValueByName "TypeTermAccessionNumber"              protocolArgs)
    //                (getFieldValueByName "TypeTermSourceREF"                    protocolArgs)
    //                (getFieldValueByName "Description"                          protocolArgs)
    //                (getFieldValueByName "URI"                                  protocolArgs)
    //                (getFieldValueByName "Version"                              protocolArgs)
    //                (getFieldValueByName "ParametersName"                       protocolArgs)
    //                (getFieldValueByName "ParametersTermAccessionNumber"        protocolArgs)
    //                (getFieldValueByName "ParametersTermSourceREF"              protocolArgs)
    //                (getFieldValueByName "ComponentsName"                       protocolArgs)
    //                (getFieldValueByName "ComponentsType"                       protocolArgs)
    //                (getFieldValueByName "ComponentsTypeTermAccessionNumber"    protocolArgs)
    //                (getFieldValueByName "ComponentsTypeTermSourceREF"          protocolArgs)
    //                []
            
    //        let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath

    //        match investigation.Studies with
    //        | Some studies -> 
    //            match API.Study.tryGetByIdentifier studyIdentifier studies with
    //            | Some study -> 
    //                match study.Protocols with
    //                | Some protocols -> 
    //                    if API.Protocol.existsByName name protocols then
    //                        log.Error($"Protocol with the name {name} already exists in the study with the identifier {studyIdentifier}.")
    //                        protocols
    //                    else
    //                        API.Protocol.add protocols protocol
    //                | None -> [protocol]
    //                |> API.Study.setProtocols study
    //                |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
    //                |> API.Investigation.setStudies investigation
    //            | None ->
    //                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
    //                investigation
    //        | None -> 
    //            log.Error($"The investigation does not contain any studies.")
    //            investigation
    //        |> Investigation.toFile investigationFilePath

    //    /// Opens an existing protocol by name in the ARC investigation study with the text editor set in globalArgs.
    //    let unregister (arcConfiguration : ArcConfiguration) (protocolArgs : Map<string,Argument>) =

    //        let log = Logging.createLogger "StudyProtocolsUnregisterLog"
            
    //        log.Info("Start Protocol Unregister")

    //        let name = getFieldValueByName "Name" protocolArgs

    //        let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath

    //        match investigation.Studies with
    //        | Some studies -> 
    //            match API.Study.tryGetByIdentifier studyIdentifier studies with
    //            | Some study -> 
    //                match study.Protocols with
    //                | Some protocols -> 
    //                    if API.Protocol.existsByName name protocols then
    //                        API.Protocol.removeByName name protocols
    //                        |> API.Study.setProtocols study
    //                        |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
    //                        |> API.Investigation.setStudies investigation
    //                    else
    //                        log.Error($"Protocol with the name {name} does not exist in the study with the identifier {studyIdentifier}.")
    //                        investigation
    //                | None -> 
    //                    log.Error($"The study with the identifier {studyIdentifier} does not contain any protocols.")
    //                    investigation
    //            | None ->
    //                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
    //                investigation
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")
    //            investigation
    //        |> Investigation.toFile investigationFilePath

    //    /// Loads a protocol or process file from a given filepath and adds it to the study.
    //    let load (arcConfiguration : ArcConfiguration) (protocolArgs : Map<string,Argument>) =

    //        let log = Logging.createLogger "StudyProtocolsLoadLog"
            
    //        log.Info("Start Protocol Load")

    //        let editor = GeneralConfiguration.getEditor arcConfiguration

    //        let path = getFieldValueByName "InputPath" protocolArgs

    //        let protocol =
    //            if containsFlag "IsProcessFile" protocolArgs then
    //                let isaProcess = Json.Process.fromFile path
    //                isaProcess.ExecutesProtocol
    //            else
    //                Json.Protocol.fromFile path |> Some
    //            |> Option.map (fun p -> 
    //                if p.Name.IsNone then
    //                    log.Error("Given protocol does not contain a name, please add it in the editor.")
    //                    ArgumentProcessing.Prompt.createIsaItemQuery editor
    //                        (List.singleton >> Protocols.toRows None) 
    //                        (Protocols.fromRows None 1 >> fun (_,_,_,items) -> items.Head) 
    //                        p
    //                else p
    //            )

    //        let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath
           
    //        match investigation.Studies with
    //        | Some studies -> 
    //            match protocol with 
    //            | Some protocol ->
    //                match API.Study.tryGetByIdentifier studyIdentifier studies with
    //                | Some study -> 
    //                    let name = protocol.Name.Value
    //                    match study.Protocols with
    //                    | Some protocols ->
    //                        if API.Protocol.existsByName name protocols then
    //                            let msg = $"Protocol with the name {name} already exists in the study with the identifier {studyIdentifier}."
    //                            if containsFlag "UpdateExisting" protocolArgs then
    //                                log.Warn($"{msg}")
    //                                log.Info("Updating protocol as \"UpdateExisting\" flag was given.")
    //                                API.Protocol.updateByName API.Update.UpdateAll protocol protocols
    //                            else
    //                                log.Error($"{msg}")
    //                                log.Info("Not updating protocol as \"UpdateExisting\" flag was not given.")
    //                                protocols
    //                        else
    //                            log.Trace($"Protocol with the name {name} does not exist in the study with the identifier {studyIdentifier}.")
    //                            API.Protocol.add protocols protocol
    //                    | None -> [protocol]
    //                    |> API.Study.setProtocols study
    //                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
    //                    |> API.Investigation.setStudies investigation
    //                | None ->
    //                    log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
    //                    investigation
    //            | None ->
    //                log.Error("The process file did not contain a protocol.")
    //                investigation
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")
    //            investigation
    //        |> Investigation.toFile investigationFilePath

    //    /// Gets an existing protocol by name from the ARC investigation study and prints its metadata.
    //    let show (arcConfiguration : ArcConfiguration) (protocolArgs : Map<string,Argument>) =
         
    //        let log = Logging.createLogger "StudyProtocolsShowLog"
            
    //        log.Info("Start Protocol Show")

    //        let name = getFieldValueByName "Name" protocolArgs

    //        let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath

    //        match investigation.Studies with
    //        | Some studies -> 
    //            match API.Study.tryGetByIdentifier studyIdentifier studies with
    //            | Some study -> 
    //                match study.Protocols with
    //                | Some protocols -> 
    //                    match API.Protocol.tryGetByName name protocols with
    //                    | Some protocol ->
    //                        [protocol]
    //                        |> Prompt.serializeXSLXWriterOutput (Protocols.toRows None)
    //                        |> log.Debug
    //                    | None -> 
    //                        log.Error($"Protocol with the DOI {name} does not exist in the study with the identifier {studyIdentifier}.")
    //                | None -> 
    //                    log.Error($"The study with the identifier {studyIdentifier} does not contain any protocols.")
    //            | None -> 
    //                log.Error($"Study with the identifier {studyIdentifier} does not exist in the investigation file.")
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")
                

    //    /// Lists the protocols included in the investigation study.
    //    let list (arcConfiguration : ArcConfiguration) = 

    //        let log = Logging.createLogger "StudyProtocolsListLog"
            
    //        log.Info"Start Protocol List"

    //        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
    //        let investigation = Investigation.fromFile investigationFilePath

    //        match investigation.Studies with
    //        | Some studies -> 
    //            studies
    //            |> Seq.iter (fun study ->
    //                match study.Protocols with
    //                | Some protocols -> 
    //                    log.Debug(sprintf "Study: %s" (Option.defaultValue "" study.Identifier))
    //                    protocols
    //                    |> Seq.iter (fun factor -> log.Debug(sprintf "--Protocol Name: %s" (Option.defaultValue "" factor.Name)))
    //                | None -> ()
    //            )
    //        | None -> 
    //            log.Error("The investigation does not contain any studies.")