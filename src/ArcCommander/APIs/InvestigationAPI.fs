namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISADotNet
open ISADotNet.XLSX


/// ArcCommander Investigation API functions that get executed by the investigation focused subcommand verbs.
module InvestigationAPI =

    module InvestigationFile =

        let exists (arcConfiguration : ArcConfiguration) =
            IsaModelConfiguration.getInvestigationFilePath arcConfiguration
            |> System.IO.File.Exists

    /// Creates an investigation file in the arc from the given investigation metadata contained in cliArgs that contains no studies or assays.
    let create (arcConfiguration : ArcConfiguration) (investigationArgs : Map<string,Argument>) =
           
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Investigation Create"

        if InvestigationFile.exists arcConfiguration then
            if verbosity >= 1 then printfn "Investigation file does already exist"

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

    /// Updates the existing investigation file in the arc with the given investigation metadata contained in cliArgs.
    let update (arcConfiguration : ArcConfiguration) (investigationArgs : Map<string,Argument>) = 

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Investigation Update"

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

        API.Investigation.update updateOption originalInvestigation investigation
        |> Investigation.toFile investigationFilePath

    /// Opens the existing investigation info in the arc with the text editor set in globalArgs.
    let edit (arcConfiguration : ArcConfiguration) =
       
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Investigation Edit"

        let editor = GeneralConfiguration.getEditor arcConfiguration

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = Investigation.fromFile investigationFilePath
               
        let editedInvestigation =
            ArgumentProcessing.Prompt.createIsaItemQuery editor Investigation.InvestigationInfo.toRows
                (Investigation.InvestigationInfo.fromRows 1 >> fun (_,_,_,item) -> Investigation.fromParts item [] [] [] [] []) 
                investigation
               
        API.Investigation.update API.Update.UpdateAllAppendLists investigation editedInvestigation
        |> Investigation.toFile investigationFilePath

    /// Deletes the existing investigation file in the arc if the given identifier matches the identifier set in the investigation file.
    let delete (arcConfiguration : ArcConfiguration) (investigationArgs : Map<string,Argument>) = 

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Investigation Delete"      

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = Investigation.fromFile investigationFilePath

        let identifier = getFieldValueByName "Identifier" investigationArgs
        
        if Some identifier = investigation.Identifier then
            System.IO.File.Delete investigationFilePath

    /// Lists the data of the investigation in this ARC.
    let show (arcConfiguration : ArcConfiguration) (investigationArgs : Map<string,Argument>) = 

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Investigation Show"

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get  

        let investigation = Investigation.fromFile investigationFilePath
        
        Prompt.serializeXSLXWriterOutput Investigation.InvestigationInfo.toRows investigation
        |> printfn "%s" 

    /// Functions for altering investigation contacts.
    module Contacts =

        /// Updates an existing assay file in the arc with the given assay metadata contained in cliArgs.
        let update (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Update"

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
                    if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the investigation" firstName midInitials lastName
                    if containsFlag "AddIfMissing" personArgs then
                        if verbosity >= 1 then printfn "Registering person as AddIfMissing Flag was set" 
                        API.Person.add persons person
                        |> API.Investigation.setContacts investigation
                    else 
                        if verbosity >= 2 then printfn "AddIfMissing argument can be used to register person with the update command if it is missing" 
                        investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any persons"
                if containsFlag "AddIfMissing" personArgs then
                    if verbosity >= 1 then printfn "Registering person as AddIfMissing Flag was set" 
                    [person]
                    |> API.Investigation.setContacts investigation
                else 
                    if verbosity >= 2 then printfn "AddIfMissing argument can be used to register person with the update command if it is missing" 
                    investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Edit"

            let editor = GeneralConfiguration.getEditor arcConfiguration

            let lastName = (getFieldValueByName  "LastName"   personArgs)
            let firstName = (getFieldValueByName  "FirstName"     personArgs)
            let midInitials = (getFieldValueByName  "MidInitials"  personArgs)

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
                    if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the investigation" firstName midInitials lastName
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any persons"
                investigation
            |> Investigation.toFile investigationFilePath


        /// Registers a person in the arc's investigation file with the given person metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Register"

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
                    if verbosity >= 1 then printfn "Person with the name %s %s %s already exists in the investigation file" firstName midInitials lastName
                    persons
                else
                    API.Person.add persons person            
            | None -> [person]   
            |> API.Investigation.setContacts investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let unregister (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Unregister"

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
                    if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the investigation file" firstName midInitials lastName
                    investigation    
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any persons"
                investigation
            |> Investigation.toFile investigationFilePath

        /// Gets an existing person by fullname (lastName,firstName,MidInitials) and prints its metadata.
        let show (arcConfiguration : ArcConfiguration) (personArgs : Map<string,Argument>) =
           
            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Get"

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
                    |> printfn "%s"
                | None -> printfn "Person with the name %s %s %s  does not exist in the investigation" firstName midInitials lastName
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any persons"
               

        /// Lists the full names of all persons included in the investigation.
        let list (arcConfiguration : ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Get"

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
                        printfn "--Person: %s %s" firstName lastName
                    else
                        printfn "--Person: %s %s %s" firstName midInitials lastName
                )
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any persons"

    /// Functions for altering investigation publications.
    module Publications =

        /// Updates an existing assay file in the arc with the given assay metadata contained in cliArgs.
        let update (arcConfiguration : ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication Update"

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
                    if verbosity >= 1 then printfn "Publication with the DOI %s does not exist in the investigation" doi
                    if containsFlag "AddIfMissing" publicationArgs then
                        if verbosity >= 1 then printfn "Registering publication as AddIfMissing Flag was set" 
                        API.Publication.add publications publication
                        |> API.Investigation.setPublications investigation
                    else 
                        if verbosity >= 2 then printfn "AddIfMissing argument can be used to register publication with the update command if it is missing" 
                        investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any publications"
                if containsFlag "AddIfMissing" publicationArgs then
                    if verbosity >= 1 then printfn "Registering publication as AddIfMissing Flag was set" 
                    [publication]
                    |> API.Investigation.setPublications investigation
                else 
                    if verbosity >= 2 then printfn "AddIfMissing argument can be used to register publication with the update command if it is missing" 
                    investigation
            |> Investigation.toFile investigationFilePath
        
        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let edit (arcConfiguration : ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication Edit"

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
                    if verbosity >= 1 then printfn "Publication with the DOI %s does not exist in the investigation" doi
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any publications"
                investigation  
            |> Investigation.toFile investigationFilePath


        /// Registers a person in the arc's investigation file with the given person metadata contained in personArgs.
        let register (arcConfiguration : ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication Register"

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
                    if verbosity >= 1 then printfn "Publication with the DOI %s already exists in the investigation" doi
                    publications
                else
                    API.Publication.add publications publication
            | None -> [publication]
            |> API.Investigation.setPublications investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let unregister (arcConfiguration : ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication Unregister"

            let doi = getFieldValueByName  "DOI" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Publications with
            | Some publications ->
                if API.Publication.existsByDoi doi publications then           
                    API.Publication.removeByDoi doi publications
                    |> API.Investigation.setPublications investigation
                else
                    if verbosity >= 1 then printfn "Publication with the DOI %s does not exist in the investigation" doi
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any publications"
                investigation
            |> Investigation.toFile investigationFilePath

        /// Gets an existing publication by its doi and prints its metadata.
        let show (arcConfiguration : ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication Get"

            let doi = getFieldValueByName  "DOI" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            match investigation.Publications with
            | Some publications ->
                match API.Publication.tryGetByDoi doi publications with
                | Some publication ->
                    [publication]
                    |> Prompt.serializeXSLXWriterOutput (Publications.toRows None)
                    |> printfn "%s"

                | None -> 
                    if verbosity >= 1 then printfn "Publication with the DOI %s does not exist in the investigation" doi
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any publications"

        /// Lists the full names of all persons included in the investigation.
        let list (arcConfiguration : ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Publications with
            | Some publications ->
                publications
                |> Seq.iter (fun publication ->
                    printfn "Publication (DOI): %s" (Option.defaultValue "" publication.DOI)
                )
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any publications"