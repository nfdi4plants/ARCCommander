namespace ArcCommander.APIs

open ArcCommander
open ArcCommander.CLIArguments
open ArcCommander.ArgumentProcessing

open ARCtrl
open ARCtrl.Spreadsheet

/// ArcCommander Investigation API functions that get executed by the investigation focused subcommand verbs.
module InvestigationAPI =

    type ArcInvestigation with
        member this.UpdateTopLevelInfo(other : ArcInvestigation, replaceWithEmptyValues : bool) =   
            if not (Helper.Identifier.isMissingIdentifier other.Identifier) then 
                IdentifierSetters.setInvestigationIdentifier other.Identifier this
                |> ignore
            if other.Title.IsSome || replaceWithEmptyValues then this.Title <- other.Title
            if other.Description.IsSome || replaceWithEmptyValues then this.Description <- other.Description
            if other.SubmissionDate.IsSome || replaceWithEmptyValues then this.SubmissionDate <- other.SubmissionDate
            if other.PublicReleaseDate.IsSome || replaceWithEmptyValues then this.PublicReleaseDate <- other.PublicReleaseDate

    
         

    module InvestigationFile =

        let exists (arcConfiguration : ArcConfiguration) =
            IsaModelConfiguration.getInvestigationFilePath arcConfiguration
            |> System.IO.File.Exists

    /// Creates an investigation file in the ARC from the given investigation metadata contained in cliArgs that contains no studies or assays.
    let create (arcConfiguration : ArcConfiguration) (investigationArgs : ArcParseResults<InvestigationCreateArgs>) =
           
        let log = Logging.createLogger "InvestigationCreateLog"

        log.Error InvestigationCreateArgs.deprecationWarning

    /// Updates the existing investigation file in the ARC with the given investigation metadata contained in cliArgs.
    let update (arcConfiguration : ArcConfiguration) (investigationArgs : ArcParseResults<InvestigationUpdateArgs>) = 

        let log = Logging.createLogger "InvestigationUpdateLog"
        
        log.Info("Start Investigation Update")

        let replaceWithEmptyValues = investigationArgs.ContainsFlag InvestigationUpdateArgs.ReplaceWithEmptyValues
        
        let identifier = 
            investigationArgs.TryGetFieldValue InvestigationUpdateArgs.InvestigationIdentifier
            |> Option.defaultValue (Helper.Identifier.createMissingIdentifier())

        let investigation = 
            ArcInvestigation.create(
                identifier,
                ?title = investigationArgs.TryGetFieldValue InvestigationUpdateArgs.Title,
                ?description = investigationArgs.TryGetFieldValue InvestigationUpdateArgs.Description,
                ?submissionDate = investigationArgs.TryGetFieldValue InvestigationUpdateArgs.SubmissionDate,
                ?publicReleaseDate = investigationArgs.TryGetFieldValue InvestigationUpdateArgs.PublicReleaseDate
            )

        let mutable arc = ARC.load(arcConfiguration)
        let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier())) //(ArcInvestigation(Helper.Identifier.createMissingIdentifier()))

        isa.UpdateTopLevelInfo(investigation, replaceWithEmptyValues)

        arc <- isa
        arc.Update(arcConfiguration)

    /// Opens the existing investigation info in the ARC with the text editor set in globalArgs.
    let edit (arcConfiguration : ArcConfiguration) =
       
        let log = Logging.createLogger "InvestigationEditLog"
        
        log.Info("Start Investigation Edit")

        let editor = GeneralConfiguration.getEditor arcConfiguration

        let mutable arc = ARC.load(arcConfiguration)
        let isa = Some arc |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

        let editedInvestigation =
            ArgumentProcessing.Prompt.createIsaItemQuery editor ArcInvestigation.InvestigationInfo.toRows
                (ArcInvestigation.InvestigationInfo.fromRows 1 >> fun (_,_,_,item) -> ArcInvestigation.fromParts item [] [] [] [] [] []) 
                isa

        isa.UpdateTopLevelInfo(editedInvestigation, true)

        arc <- isa
        arc.Update(arcConfiguration)


    ///// Deletes the existing investigation file in the ARC if the given identifier matches the identifier set in the investigation file.
    //let delete (arcConfiguration : ArcConfiguration) (investigationArgs : Map<string,Argument>) = 

    //    let log = Logging.createLogger "InvestigationDeleteLog"
        
    //    log.Info("Start Investigation Delete")

    //    let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
    //    let investigation = Investigation.fromFile investigationFilePath

    //    let identifier = getFieldValueByName "Identifier" investigationArgs
        
    //    if Some identifier = investigation.Identifier then
    //        System.IO.File.Delete investigationFilePath


    /// Lists the data of the investigation in this ARC.
    let show (arcConfiguration : ArcConfiguration) = 

        let log = Logging.createLogger "InvestigationShowLog"
        
        log.Info("Start Investigation Show")

        let arc = ARC.load(arcConfiguration)
        let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))
        
        Prompt.serializeXSLXWriterOutput ArcInvestigation.InvestigationInfo.toRows isa
        |> log.Debug

    /// Functions for altering investigation contacts.
    module Contacts =

        open CLIArguments.InvestigationContacts

        type Person with
             
             member this.UpdateBy(p : Person, ?onlyReplaceExisting : bool,?appendSequences : bool) =
                let onlyReplaceExisting = defaultArg onlyReplaceExisting false
                let appendSequences = defaultArg appendSequences false
                let updateAlways = onlyReplaceExisting |> not
                if p.ORCID.IsSome || updateAlways then 
                    this.ORCID <- p.ORCID
                if p.LastName.IsSome || updateAlways then
                    this.LastName <- p.LastName
                if p.FirstName.IsSome || updateAlways then
                    this.FirstName <- p.FirstName
                if p.MidInitials.IsSome || updateAlways then
                    this.MidInitials <- p.MidInitials
                if p.EMail.IsSome || updateAlways then
                    this.EMail <- p.EMail
                if p.Phone.IsSome || updateAlways then
                    this.Phone <- p.Phone
                if p.Fax.IsSome || updateAlways then
                    this.Fax <- p.Fax
                if p.Address.IsSome || updateAlways then
                    this.Address <- p.Address
                if p.Affiliation.IsSome || updateAlways then
                    this.Affiliation <- p.Affiliation
                if p.Roles.Count <> 0 || updateAlways then
                    let r = ArcTypesAux.updateAppendResizeArray appendSequences this.Roles p.Roles
                    this.Roles <- r
                if p.Comments.Count <> 0 || updateAlways then
                    let s = ArcTypesAux.updateAppendResizeArray appendSequences this.Comments p.Comments
                    this.Comments <- s

        /// Updates an existing assay file in the ARC with the given assay metadata contained in cliArgs.
        let update (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonUpdateArgs>) =

            let log = Logging.createLogger "InvestigationContactsUpdateLog"

            log.Info("Start Person Update")

            let onlyReplaceExisting = personArgs.ContainsFlag PersonUpdateArgs.ReplaceWithEmptyValues |> not                   

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
                    (ResizeArray())
            
            person.ORCID <- orcid

            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))
           
            match Person.tryGetByFullName firstName (midInitials |> Option.defaultValue "") lastName (Array.ofSeq isa.Contacts) with
            | Some p ->
                p.UpdateBy(person, onlyReplaceExisting = onlyReplaceExisting)
             | None ->
                let msg = $"Person with the name {firstName} {midInitials} {lastName} does not exist in the investigation."
                if personArgs.ContainsFlag PersonUpdateArgs.AddIfMissing then
                    log.Warn($"{msg}")
                    log.Info("Registering person as AddIfMissing Flag was set.")
                    isa.Contacts.Add person
                else 
                    log.Error(msg)

            arc.Update(arcConfiguration)

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonEditArgs>) =
            
            let log = Logging.createLogger "InvestigationContactsEditLog"
            log.Info("Start Person Edit")
            let editor = GeneralConfiguration.getEditor arcConfiguration
            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

            let lastName    = personArgs.GetFieldValue PersonEditArgs.LastName
            let firstName   = personArgs.GetFieldValue PersonEditArgs.FirstName
            let midInitials = personArgs.TryGetFieldValue PersonEditArgs.MidInitials

            match Person.tryGetByFullName firstName (midInitials |> Option.defaultValue "") lastName (Array.ofSeq isa.Contacts) with
            | Some person ->
                ArgumentProcessing.Prompt.createIsaItemQuery editor
                    (List.singleton >> Contacts.toRows None) 
                    (Contacts.fromRows None 1 >> fun (_,_,_,items) -> items.Head)
                    person
                |> fun p -> 
                    person.UpdateBy(p) 
            | None ->
                log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the investigation.")      

            arc.Update(arcConfiguration)

        /// Registers a person in the ARC's investigation file with the given person metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonRegisterArgs>) =
            
            let log = Logging.createLogger "InvestigationContactsRegisterLog"

            log.Info("Start Person Register")

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
                    (ResizeArray())
            
            person.ORCID <- orcid
                       
            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc)|> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

            if Person.existsByFullName firstName (midInitials |> Option.defaultValue "") lastName (Array.ofSeq isa.Contacts) then
                log.Error $"Person with the name {firstName} {midInitials} {lastName} does already exist in the investigation."
            else
                isa.Contacts.Add person         

            arc.Update(arcConfiguration)

        /// Opens an existing person by fullname (LastName, FirstName, MidInitials) in the ARC with the text editor set in globalArgs.
        let unregister (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonUnregisterArgs>) =

            let log = Logging.createLogger "InvestigationContactsUnregisterLog"

            log.Info("Start Person Unregister")

            let lastName    = personArgs.GetFieldValue PersonUnregisterArgs.LastName
            let firstName   = personArgs.GetFieldValue PersonUnregisterArgs.FirstName
            let midInitials = personArgs.TryGetFieldValue PersonUnregisterArgs.MidInitials

            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

            let tryGetIndex (persons : Person seq) =
                persons
                |> Seq.tryFindIndex (fun p -> 
                    p.FirstName = Some firstName 
                    && p.MidInitials = midInitials 
                    && p.LastName = Some lastName
                )

            match tryGetIndex isa.Contacts with
            | Some index ->
                isa.Contacts.RemoveAt index
            | None -> 
                log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the investigation.")

            arc.Update(arcConfiguration)

        /// Gets an existing person by fullname (LastName, FirstName, MidInitials) and prints their metadata.
        let show (arcConfiguration : ArcConfiguration) (personArgs : ArcParseResults<PersonShowArgs>) =
           
            let log = Logging.createLogger "InvestigationContactsShowLog"

            log.Info("Start Person Show")

            let lastName    = personArgs.GetFieldValue PersonShowArgs.LastName
            let firstName   = personArgs.GetFieldValue PersonShowArgs.FirstName
            let midInitials = personArgs.TryGetFieldValue PersonShowArgs.MidInitials

            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

            match Person.tryGetByFullName firstName (midInitials |> Option.defaultValue "") lastName (Array.ofSeq isa.Contacts) with
            | Some person ->
                [person]
                |> Prompt.serializeXSLXWriterOutput (Contacts.toRows None)
                |> log.Debug
            | None ->
                log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the investigation.")

        /// Lists the full names of all persons included in the investigation.
        let list (arcConfiguration : ArcConfiguration) = 

            let log = Logging.createLogger "InvestigationContactsListLog"

            log.Info("Start Person List")

            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

            isa.Contacts
            |> Seq.iter (
                fun person -> 
                    let firstName   = Option.defaultValue "" person.FirstName
                    let midInitials = Option.defaultValue "" person.MidInitials
                    let lastName    = Option.defaultValue "" person.LastName
                    if midInitials = "" 
                    then log.Debug($"--Person: {firstName} {lastName}")
                    else log.Debug($"--Person: {firstName} {midInitials} {lastName}")
            )

    /// Functions for altering investigation publications.
    module Publications =

        open ArcCommander.CLIArguments.InvestigationPublications

        type Publication with 
            
            member this.UpdateBy(p : Publication, ?onlyReplaceExisting : bool,?appendSequences : bool) =
                let onlyReplaceExisting = defaultArg onlyReplaceExisting false
                let appendSequences = defaultArg appendSequences false
                let updateAlways = onlyReplaceExisting |> not
                if p.PubMedID.IsSome || updateAlways then 
                    this.PubMedID <- p.PubMedID
                if p.DOI.IsSome || updateAlways then
                    this.DOI <- p.DOI
                if p.Authors.IsSome || updateAlways then
                    this.Authors <- p.Authors
                if p.Title.IsSome || updateAlways then
                    this.Title <- p.Title
                if p.Status.IsSome || updateAlways then
                    this.Status <- p.Status                
                if p.Comments.Count <> 0 || updateAlways then
                    let s = ArcTypesAux.updateAppendResizeArray appendSequences this.Comments p.Comments
                    this.Comments <- s

            static member existsByDOI doi (publications : Publication seq) =  
                publications
                |> Seq.exists (fun p -> p.DOI = Some doi)

            static member tryGetByDOI doi (publications : Publication seq) =
                publications
                |> Seq.tryFind (fun p -> p.DOI = Some doi)

            static member tryFindIndexByDOI doi (publications : Publication seq) =
                publications
                |> Seq.tryFindIndex (fun p -> p.DOI = Some doi)

        /// Updates an existing assay file in the ARC with the given assay metadata contained in cliArgs.
        let update (arcConfiguration : ArcConfiguration) (publicationArgs : ArcParseResults<PublicationUpdateArgs>) =

            let log = Logging.createLogger "InvestigationPublicationsUpdateLog"
            
            log.Info("Start Publication Update")

            let onlyReplaceExisting = publicationArgs.ContainsFlag PublicationUpdateArgs.ReplaceWithEmptyValues |> not

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
                     (ResizeArray())

            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

            match Publication.tryGetByDOI doi isa.Publications with
            | Some p ->
                p.UpdateBy(publication, onlyReplaceExisting = onlyReplaceExisting)
            | None ->
                let msg = $"Publication with the doi {doi} does not exist in the investigation."
                if publicationArgs.ContainsFlag PublicationUpdateArgs.AddIfMissing then
                    log.Warn($"{msg}")
                    log.Info("Registering publciation as AddIfMissing Flag was set.")
                    isa.Publications.Add publication
                else 
                    log.Error(msg)

            arc.Update(arcConfiguration)

        
        /// Opens an existing person by fullname (LastName, FirstName, MidInitials) in the ARC with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (publicationArgs : ArcParseResults<PublicationEditArgs>) =
            let log = Logging.createLogger "InvestigationPublicationsEditLog"
            
            log.Info("Start Publication Edit")

            let editor = GeneralConfiguration.getEditor arcConfiguration

            let doi = publicationArgs.GetFieldValue PublicationEditArgs.DOI

            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

            match Publication.tryGetByDOI doi isa.Publications with
            | Some publication ->
                ArgumentProcessing.Prompt.createIsaItemQuery editor
                        (List.singleton >> Publications.toRows None) 
                        (Publications.fromRows None 1 >> fun (_,_,_,items) -> items.Head)
                        publication
                |> fun p -> 
                    publication.UpdateBy(p)
            | None ->
                log.Error($"Publication with the doi {doi} does not exist in the investigation.")

            arc.Update(arcConfiguration)

        /// Registers a person in the ARC's investigation file with the given person metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (publicationArgs : ArcParseResults<PublicationRegisterArgs>) =

            let log = Logging.createLogger "InvestigationPublicationsRegisterLog"
            
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
                    (ResizeArray())


            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

            if Publication.existsByDOI doi isa.Publications then
                log.Warn($"Publication with the doi {doi} already exists in the investigation.")
            else
                isa.Publications.Add publication

            arc.Update(arcConfiguration)

        /// Opens an existing person by fullname (LastName, FirstName, MidInitials) in the ARC with the text editor set in globalArgs.
        let unregister (arcConfiguration : ArcConfiguration) (publicationArgs : ArcParseResults<PublicationUnregisterArgs>) =

            let log = Logging.createLogger "InvestigationPublicationsUnregisterLog"
            
            log.Info("Start Publication Unregister")

            let doi = publicationArgs.GetFieldValue PublicationUnregisterArgs.DOI

            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

            match Publication.tryFindIndexByDOI doi isa.Publications with
            | Some index ->
                isa.Publications.RemoveAt index
            | None ->   
                log.Warn($"Publication with the doi {doi} does not exist in the investigation.")

            arc.Update(arcConfiguration)

        /// Gets an existing publication by its doi and prints its metadata.
        let show (arcConfiguration : ArcConfiguration) (publicationArgs : ArcParseResults<PublicationShowArgs>) =

            let log = Logging.createLogger "InvestigationPublicationsShowLog"
            
            log.Info("Start Publication Show")

            let doi = publicationArgs.GetFieldValue PublicationShowArgs.DOI

            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

            match Publication.tryGetByDOI doi isa.Publications with
            | Some publication ->
                [publication]
                |> Prompt.serializeXSLXWriterOutput (Publications.toRows None)
                |> log.Debug
            | None ->
                log.Error($"Publication with the doi {doi} does not exist in the investigation.")

        /// Lists the full names of all persons included in the investigation.
        let list (arcConfiguration : ArcConfiguration) = 

            let log = Logging.createLogger "InvestigationPublicationsListLog"
            
            log.Info("Start Publication List")

            let arc = ARC.load(arcConfiguration)
            let isa = (Some arc) |> Option.defaultValue (ARC(Helper.Identifier.createMissingIdentifier()))

            match isa.Publications with
            | publications when Seq.isEmpty publications ->
                log.Warn("The investigation does not contain any publications.")
            | publications ->
               publications
                |> Seq.iter (fun publication ->
                    log.Debug(sprintf "Publication (DOI): %s" (Option.defaultValue "" publication.DOI))
                )
