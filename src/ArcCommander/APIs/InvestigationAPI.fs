namespace ArcCommander.APIs

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISADotNet
open ISADotNet.XLSX
open arcIO.NET

/// ArcCommander Investigation API functions that get executed by the investigation focused subcommand verbs.
module InvestigationAPI =

    module InvestigationFile =

        let exists (arcConfiguration : ArcConfiguration) =
            IsaModelConfiguration.getInvestigationFilePath arcConfiguration
            |> System.IO.File.Exists

    /// Creates an investigation file in the ARC from the given investigation metadata contained in cliArgs that contains no studies or assays.
    let create (arcConfiguration : ArcConfiguration) (investigationArgs : Map<string,Argument>) =
           
        let log = Logging.createLogger "InvestigationCreateLog"
        
        log.Info("Start Investigation Create")

        if InvestigationFile.exists arcConfiguration then
            log.Error("Investigation file does already exist.")

        else 
            let investigation = 
                let info =
                    Investigation.InvestigationInfo.create
                        (getFieldValueByName "Identifier" investigationArgs)
                        (getFieldValueByName "Title" investigationArgs)
                        (getFieldValueByName "Description" investigationArgs)
                        (getFieldValueByName "SubmissionDate" investigationArgs)
                        (getFieldValueByName "PublicReleaseDate" investigationArgs)
                        []
                Investigation.fromParts info [] [] [] [] [] 

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
                          
            Investigation.toFile investigationFilePath investigation

    /// Updates the existing investigation file in the ARC with the given investigation metadata contained in cliArgs.
    let update (arcConfiguration : ArcConfiguration) (investigationArgs : Map<string,Argument>) = 

        let log = Logging.createLogger "InvestigationUpdateLog"
        
        log.Info("Start Investigation Update")

        let updateOption = if containsFlag "ReplaceWithEmptyValues" investigationArgs then API.Update.UpdateAllAppendLists else API.Update.UpdateByExisting

        let investigation = 
            let info =
                Investigation.InvestigationInfo.create
                    (getFieldValueByName "Identifier" investigationArgs)
                    (getFieldValueByName "Title" investigationArgs)
                    (getFieldValueByName "Description" investigationArgs)
                    (getFieldValueByName "SubmissionDate" investigationArgs)
                    (getFieldValueByName "PublicReleaseDate" investigationArgs)
                    []
            Investigation.fromParts info [] [] [] [] [] 

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let originalInvestigation = Investigation.fromFile investigationFilePath

        API.Investigation.updateBy updateOption originalInvestigation investigation
        |> Investigation.toFile investigationFilePath

    /// Opens the existing investigation info in the ARC with the text editor set in globalArgs.
    let edit (arcConfiguration : ArcConfiguration) =
       
        let log = Logging.createLogger "InvestigationEditLog"
        
        log.Info("Start Investigation Edit")

        let editor = GeneralConfiguration.getEditor arcConfiguration

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = Investigation.fromFile investigationFilePath
               
        let editedInvestigation =
            ArgumentProcessing.Prompt.createIsaItemQuery editor Investigation.InvestigationInfo.toRows
                (Investigation.InvestigationInfo.fromRows 1 >> fun (_,_,_,item) -> Investigation.fromParts item [] [] [] [] []) 
                investigation
               
        API.Investigation.updateBy API.Update.UpdateAllAppendLists investigation editedInvestigation
        |> Investigation.toFile investigationFilePath

    /// Deletes the existing investigation file in the ARC if the given identifier matches the identifier set in the investigation file.
    let delete (arcConfiguration : ArcConfiguration) (investigationArgs : Map<string,Argument>) = 

        let log = Logging.createLogger "InvestigationDeleteLog"
        
        log.Info("Start Investigation Delete")

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = Investigation.fromFile investigationFilePath

        let identifier = getFieldValueByName "Identifier" investigationArgs
        
        if Some identifier = investigation.Identifier then
            System.IO.File.Delete investigationFilePath

    /// Lists the data of the investigation in this ARC.
    let show (arcConfiguration : ArcConfiguration) = 

        let log = Logging.createLogger "InvestigationShowLog"
        
        log.Info("Start Investigation Show")

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get  

        let investigation = Investigation.fromFile investigationFilePath
        
        Prompt.serializeXSLXWriterOutput Investigation.InvestigationInfo.toRows investigation
        |> log.Debug

    /// Functions for altering investigation contacts.
    module Contacts =

        /// Updates an existing assay file in the ARC with the given assay metadata contained in cliArgs.
        let update (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let log = Logging.createLogger "InvestigationContactsUpdateLog"

            log.Info("Start Person Update")

            let updateOption = if containsFlag "ReplaceWithEmptyValues" personArgs then API.Update.UpdateAll else API.Update.UpdateByExisting

            let lastName    = getFieldValueByName "LastName"    personArgs
            let firstName   = getFieldValueByName "FirstName"   personArgs
            let midInitials = getFieldValueByName "MidInitials" personArgs

            let comments = 
                match tryGetFieldValueByName "ORCID" personArgs with
                | Some orcid -> [Comment.fromString "Investigation Person ORCID" orcid]
                | None -> []

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

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Contacts with
            | Some persons ->
                if API.Person.existsByFullName firstName midInitials lastName persons then
                    API.Person.updateByFullName updateOption person persons
                    |> API.Investigation.setContacts investigation
                else
                    let msg = $"Person with the name {firstName} {midInitials} {lastName} does not exist in the investigation."
                    if containsFlag "AddIfMissing" personArgs then
                        log.Warn($"{msg}")
                        log.Info("Registering person as AddIfMissing Flag was set.")
                        API.Person.add persons person
                        |> API.Investigation.setContacts investigation
                    else 
                        log.Error($"{msg}")
                        log.Trace("AddIfMissing argument can be used to register person with the update command if it is missing.")
                        investigation
            | None -> 
                let msg = "The investigation does not contain any persons."
                if containsFlag "AddIfMissing" personArgs then 
                    log.Warn($"{msg}")
                    log.Info("Registering person as AddIfMissing Flag was set.")
                    [person]
                    |> API.Investigation.setContacts investigation
                else 
                    log.Error($"{msg}")
                    log.Trace("AddIfMissing argument can be used to register person with the update command if it is missing.")
                    investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let log = Logging.createLogger "InvestigationContactsEditLog"

            log.Info("Start Person Edit")

            let editor = GeneralConfiguration.getEditor arcConfiguration

            let lastName    = (getFieldValueByName  "LastName"      personArgs)
            let firstName   = (getFieldValueByName  "FirstName"     personArgs)
            let midInitials = (getFieldValueByName  "MidInitials"   personArgs)

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            match investigation.Contacts with
            | Some persons ->
                match API.Person.tryGetByFullName firstName midInitials lastName persons with
                | Some person -> 
                    ArgumentProcessing.Prompt.createIsaItemQuery editor
                        (List.singleton >> Contacts.toRows None) 
                        (Contacts.fromRows None 1 >> fun (_,_,_,items) -> items.Head) 
                        person
                    |> fun p -> API.Person.updateBy ((=) person) API.Update.UpdateAll p persons
                    |> API.Investigation.setContacts investigation
                | None ->
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the investigation.") 
                    investigation
            | None -> 
                log.Error("The investigation does not contain any persons.")
                investigation
            |> Investigation.toFile investigationFilePath


        /// Registers a person in the ARC's investigation file with the given person metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let log = Logging.createLogger "InvestigationContactsRegisterLog"

            log.Info("Start Person Register")

            let lastName    = getFieldValueByName "LastName"    personArgs                   
            let firstName   = getFieldValueByName "FirstName"   personArgs
            let midInitials = getFieldValueByName "MidInitials" personArgs

            let comments = 
                match tryGetFieldValueByName "ORCID" personArgs with
                | Some orcid -> [Comment.fromString "Investigation Person ORCID" orcid]
                | None -> []

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
            
            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Contacts with
            | Some persons ->
                if API.Person.existsByFullName firstName midInitials lastName persons then
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} already exists in the investigation file.")
                    persons
                else
                    API.Person.add persons person
            | None -> [person]
            |> API.Investigation.setContacts investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing person by fullname (LastName, FirstName, MidInitials) in the ARC with the text editor set in globalArgs.
        let unregister (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let log = Logging.createLogger "InvestigationContactsUnregisterLog"

            log.Info("Start Person Unregister")

            let lastName    = getFieldValueByName "LastName"    personArgs
            let firstName   = getFieldValueByName "FirstName"   personArgs
            let midInitials = getFieldValueByName "MidInitials" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            match investigation.Contacts with
            | Some persons ->
                if API.Person.existsByFullName firstName midInitials lastName persons then
                    API.Person.removeByFullName firstName midInitials lastName persons
                    |> API.Investigation.setContacts investigation
                else
                    log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the investigation file")
                    investigation
            | None -> 
                log.Error("The investigation does not contain any persons.")
                investigation
            |> Investigation.toFile investigationFilePath

        /// Gets an existing person by fullname (LastName, FirstName, MidInitials) and prints their metadata.
        let show (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =
           
            let log = Logging.createLogger "InvestigationContactsShowLog"

            log.Info("Start Person Show")

            let lastName    = getFieldValueByName "LastName"    personArgs                   
            let firstName   = getFieldValueByName "FirstName"   personArgs
            let midInitials = getFieldValueByName "MidInitials" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            match investigation.Contacts with
            | Some persons ->
                match API.Person.tryGetByFullName firstName midInitials lastName persons with
                | Some person ->
                    [person]
                    |> Prompt.serializeXSLXWriterOutput (Contacts.toRows None)
                    |> log.Debug
                | None -> log.Error($"Person with the name {firstName} {midInitials} {lastName} does not exist in the investigation.")
            | None -> 
                log.Error("The investigation does not contain any persons.")
               

        /// Lists the full names of all persons included in the investigation.
        let list (arcConfiguration : ArcConfiguration) = 

            let log = Logging.createLogger "InvestigationContactsListLog"

            log.Info("Start Person List")

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Contacts with
            | Some persons ->
                persons
                |> Seq.iter (fun person ->
                    let firstName = Option.defaultValue "" person.FirstName
                    let midInitials = Option.defaultValue "" person.MidInitials
                    let lastName = Option.defaultValue "" person.LastName
                    if midInitials = "" then
                        log.Debug($"--Person: {firstName} {lastName}")
                    else
                        log.Debug($"--Person: {firstName} {midInitials} {lastName}")
                )
            | None -> 
                log.Error("The investigation does not contain any persons.")

    /// Functions for altering investigation publications.
    module Publications =

        /// Updates an existing assay file in the ARC with the given assay metadata contained in cliArgs.
        let update (arcConfiguration : ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let log = Logging.createLogger "InvestigationPublicationsUpdateLog"
            
            log.Info("Start Publication Update")

            let updateOption = if containsFlag "ReplaceWithEmptyValues" publicationArgs then API.Update.UpdateAll else API.Update.UpdateByExisting

            let doi = getFieldValueByName  "DOI"                        publicationArgs

            let publication =
                 Publications.fromString
                     (getFieldValueByName  "PubMedID"                   publicationArgs)
                     doi
                     (getFieldValueByName  "AuthorList"                 publicationArgs)
                     (getFieldValueByName  "Title"                      publicationArgs)
                     (getFieldValueByName  "Status"                     publicationArgs)
                     (getFieldValueByName  "StatusTermAccessionNumber"  publicationArgs)
                     (getFieldValueByName  "StatusTermSourceREF"        publicationArgs)
                     []

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Publications with
            | Some publications ->
                if API.Publication.existsByDoi doi publications then
                    API.Publication.updateByDOI updateOption publication publications
                    |> API.Investigation.setPublications investigation
                else
                    let msg = $"Publication with the DOI {doi} does not exist in the investigation."
                    if containsFlag "AddIfMissing" publicationArgs then
                        log.Warn($"{msg}")
                        log.Info("Registering publication as AddIfMissing Flag was set.")
                        API.Publication.add publications publication
                        |> API.Investigation.setPublications investigation
                    else 
                        log.Error($"{msg}")
                        log.Trace("AddIfMissing argument can be used to register publication with the update command if it is missing.")
                        investigation
            | None -> 
                let msg = "The investigation does not contain any publications."
                if containsFlag "AddIfMissing" publicationArgs then
                    log.Warn($"{msg}")
                    log.Info("Registering publication as AddIfMissing Flag was set.")
                    [publication]
                    |> API.Investigation.setPublications investigation
                else 
                    log.Error($"{msg}")
                    log.Trace("AddIfMissing argument can be used to register publication with the update command if it is missing.")
                    investigation
            |> Investigation.toFile investigationFilePath
        
        /// Opens an existing person by fullname (LastName, FirstName, MidInitials) in the ARC with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let log = Logging.createLogger "InvestigationPublicationsEditLog"
            
            log.Info("Start Publication Edit")

            let editor = GeneralConfiguration.getEditor arcConfiguration

            let doi = getFieldValueByName "DOI" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            match investigation.Publications with
            | Some publications ->
                match API.Publication.tryGetByDoi doi publications with
                | Some publication ->
                    ArgumentProcessing.Prompt.createIsaItemQuery editor
                        (List.singleton >> Publications.toRows None) 
                        (Publications.fromRows None 1 >> fun (_,_,_,items) -> items.Head) 
                        publication
                    |> fun p -> API.Publication.updateBy ((=) publication) API.Update.UpdateAll p publications
                    |> API.Investigation.setPublications investigation
                | None ->
                    log.Error($"Publication with the DOI {doi} does not exist in the investigation.")
                    investigation
            | None -> 
                log.Error("The investigation does not contain any publications.")
                investigation  
            |> Investigation.toFile investigationFilePath


        /// Registers a person in the ARC's investigation file with the given person metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let log = Logging.createLogger "InvestigationPublicationsRegisterLog"
            
            log.Info("Start Publication Register")

            let doi = getFieldValueByName  "DOI"                        publicationArgs

            let publication = 
                 Publications.fromString
                     (getFieldValueByName  "PubMedID"                   publicationArgs)
                     doi
                     (getFieldValueByName  "AuthorList"                 publicationArgs)
                     (getFieldValueByName  "Title"                      publicationArgs)
                     (getFieldValueByName  "Status"                     publicationArgs)
                     (getFieldValueByName  "StatusTermAccessionNumber"  publicationArgs)
                     (getFieldValueByName  "StatusTermSourceREF"        publicationArgs)
                     []
            
            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Publications with
            | Some publications ->
                if API.Publication.existsByDoi doi publications then
                    log.Error($"Publication with the DOI {doi} already exists in the investigation.")
                    publications
                else
                    API.Publication.add publications publication
            | None -> [publication]
            |> API.Investigation.setPublications investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing person by fullname (LastName, FirstName, MidInitials) in the ARC with the text editor set in globalArgs.
        let unregister (arcConfiguration : ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let log = Logging.createLogger "InvestigationPublicationsUnregisterLog"
            
            log.Info("Start Publication Unregister")

            let doi = getFieldValueByName  "DOI" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Publications with
            | Some publications ->
                if API.Publication.existsByDoi doi publications then
                    API.Publication.removeByDoi doi publications
                    |> API.Investigation.setPublications investigation
                else
                    log.Error($"Publication with the DOI {doi} does not exist in the investigation.")
                    investigation
            | None -> 
                log.Error("The investigation does not contain any publications.")
                investigation
            |> Investigation.toFile investigationFilePath

        /// Gets an existing publication by its doi and prints its metadata.
        let show (arcConfiguration : ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let log = Logging.createLogger "InvestigationPublicationsShowLog"
            
            log.Info("Start Publication Show")

            let doi = getFieldValueByName  "DOI" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            match investigation.Publications with
            | Some publications ->
                match API.Publication.tryGetByDoi doi publications with
                | Some publication ->
                    [publication]
                    |> Prompt.serializeXSLXWriterOutput (Publications.toRows None)
                    |> log.Debug
                | None -> log.Error($"Publication with the DOI {doi} does not exist in the investigation.")
            | None -> log.Error("The investigation does not contain any publications.")

        /// Lists the full names of all persons included in the investigation.
        let list (arcConfiguration : ArcConfiguration) = 

            let log = Logging.createLogger "InvestigationPublicationsListLog"
            
            log.Info("Start Publication List")

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Publications with
            | Some publications ->
                publications
                |> Seq.iter (fun publication ->
                    log.Debug(sprintf "Publication (DOI): %s" (Option.defaultValue "" publication.DOI))
                )
            | None -> 
                log.Error("The investigation does not contain any publications.")