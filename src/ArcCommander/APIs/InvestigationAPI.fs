namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISADotNet
open ISADotNet.XLSX


/// ArcCommander Investigation API functions that get executed by the investigation focused subcommand verbs
module InvestigationAPI =

    /// Creates an investigation file in the arc from the given investigation metadata contained in cliArgs that contains no studies or assays.
    let create (arcConfiguration:ArcConfiguration) (investigationArgs : Map<string,Argument>) =
           
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Investigation Create"

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
    let update (arcConfiguration:ArcConfiguration) (investigationArgs : Map<string,Argument>) = 

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
    let edit (arcConfiguration:ArcConfiguration) =
       
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Investigation Edit"

        let editor = GeneralConfiguration.getEditor arcConfiguration
        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = Investigation.fromFile investigationFilePath
               
        let editedInvestigation =
            ArgumentProcessing.Prompt.createIsaItemQuery editor workDir Investigation.InvestigationInfo.WriteInvestigationInfo 
                (Investigation.InvestigationInfo.ReadInvestigationInfo 1 >> fun (_,_,_,item) -> Investigation.fromParts item [] [] [] [] []) 
                investigation
               
        Investigation.update API.Update.UpdateAllAppendLists investigation editedInvestigation
        |> Investigation.toFile investigationFilePath

    /// Deletes the existing investigation file in the arc if the given identifier matches the identifier set in the investigation file
    let delete (arcConfiguration:ArcConfiguration) (investigationArgs : Map<string,Argument>) = 

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Investigation Delete"      

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = Investigation.fromFile investigationFilePath

        let identifier = getFieldValueByName "Identifier" investigationArgs
        
        if identifier = investigation.Identifier then
            System.IO.File.Delete investigationFilePath

    /// Functions for altering investigation contacts
    module Contacts =

        /// Updates an existing assay file in the arc with the given assay metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Update"

            let updateOption = if containsFlag "ReplaceWithEmptyValues" personArgs then API.Update.UpdateAll else API.Update.UpdateByExisting            

            let lastName    = getFieldValueByName "LastName"    personArgs                   
            let firstName   = getFieldValueByName "FirstName"   personArgs
            let midInitials = getFieldValueByName "MidInitials" personArgs

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
                    []

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let persons = investigation.Contacts

            if API.Person.existsByFullName lastName midInitials firstName persons then
                API.Person.updateByFullName updateOption person persons
                |> API.Investigation.setContacts investigation
            else
                if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the investigation" firstName midInitials lastName
                investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Edit"

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let lastName = (getFieldValueByName  "LastName"   personArgs)
            let firstName = (getFieldValueByName  "FirstName"     personArgs)
            let midInitials = (getFieldValueByName  "MidInitials"  personArgs)

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            let persons = investigation.Contacts
            match API.Person.tryGetByFullName firstName midInitials lastName persons with
            | Some person -> 
                ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                    (List.singleton >> Contacts.writePersons "Person") 
                    (Contacts.readPersons "Person" 1 >> fun (_,_,_,items) -> items.Head) 
                    person
                |> fun p -> API.Person.updateBy ((=) person) API.Update.UpdateAll p persons
                |> API.Investigation.setContacts investigation
            | None ->
                if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the investigation" firstName midInitials lastName
                investigation
            |> Investigation.toFile investigationFilePath


        /// Registers a person in the arc's investigation file with the given person metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Register"

            let lastName    = getFieldValueByName "LastName"    personArgs                   
            let firstName   = getFieldValueByName "FirstName"   personArgs
            let midInitials = getFieldValueByName "MidInitials" personArgs

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
                    []
            
            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let persons = investigation.Contacts

            if API.Person.existsByFullName firstName midInitials lastName persons then               
                if verbosity >= 1 then printfn "Person with the name %s %s %s already exists in the investigation file" firstName midInitials lastName
                investigation
            else
                API.Person.add persons person               
                |> API.Investigation.setContacts investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Unregister"

            let lastName    = getFieldValueByName "LastName"    personArgs                   
            let firstName   = getFieldValueByName "FirstName"   personArgs
            let midInitials = getFieldValueByName "MidInitials" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            let persons = investigation.Contacts

            if API.Person.existsByFullName firstName midInitials lastName persons then               
                API.Person.removeByFullName firstName midInitials lastName persons   
                |> API.Investigation.setContacts investigation
            else
                if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the investigation file" firstName midInitials lastName
                investigation                
            |> Investigation.toFile investigationFilePath

        /// Gets an existing person by fullname (lastName,firstName,MidInitials) and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =
           
            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Get"

            let lastName    = getFieldValueByName "LastName"    personArgs                   
            let firstName   = getFieldValueByName "FirstName"   personArgs
            let midInitials = getFieldValueByName "MidInitials" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            match API.Person.tryGetByFullName firstName midInitials lastName investigation.Contacts with
            | Some person ->
                [person]
                |> Prompt.serializeXSLXWriterOutput (Contacts.writePersons "Person")
                |> printfn "%s"
            | None -> printfn "Person with the name %s %s %s  does not exist in the investigation" firstName midInitials lastName


        /// Lists the full names of all persons included in the investigation
        let list (arcConfiguration:ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

            if verbosity >= 1 then printfn "Start Person Get"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            investigation.Contacts
            |> Seq.iter (fun person ->
                if person.MidInitials <> "" then
                    printfn "Person: %s %s %s" person.FirstName person.MidInitials person.LastName
                else
                    printfn "Person: %s %s" person.FirstName person.LastName
            )

    /// Functions for altering investigation publications
    module Publications =

        /// Updates an existing assay file in the arc with the given assay metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication update"

            let updateOption = if containsFlag "ReplaceWithEmptyValues" publicationArgs then API.Update.UpdateAll else API.Update.UpdateByExisting            

            let doi = getFieldValueByName  "DOI"                                  publicationArgs

            let publication = 
                 Publications.fromString
                     (getFieldValueByName  "PubMedID"                             publicationArgs)
                     doi
                     (getFieldValueByName  "AuthorList"                           publicationArgs)
                     (getFieldValueByName  "PublicationTitle"                     publicationArgs)
                     (getFieldValueByName  "PublicationStatus"                    publicationArgs)
                     (getFieldValueByName  "PublicationStatusTermAccessionNumber" publicationArgs)
                     (getFieldValueByName  "StatusTermSourceREF"                  publicationArgs)
                     []

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let publications = investigation.Publications

            if API.Publication.existsByDoi doi publications then
                API.Publication.updateByDOI updateOption publication publications
                |> API.Investigation.setPublications investigation
            else
                if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the investigation" doi
                investigation
            |> Investigation.toFile investigationFilePath
        
        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication Edit"

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let doi = getFieldValueByName "DOI" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            let publications = investigation.Publications

            match API.Publication.tryGetByDoi doi publications with
            | Some publication ->                    
                ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                    (List.singleton >> Publications.writePublications "Publication") 
                    (Publications.readPublications "Publication" 1 >> fun (_,_,_,items) -> items.Head) 
                    publication
                |> fun p -> API.Publication.updateBy ((=) publication) API.Update.UpdateAll p publications
                |> API.Investigation.setPublications investigation

            | None ->
                if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the investigation" doi
                investigation
            |> Investigation.toFile investigationFilePath


        /// Registers a person in the arc's investigation file with the given person metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication Register"

            let doi = getFieldValueByName  "DOI"                                  publicationArgs

            let publication = 
                 Publications.fromString
                     (getFieldValueByName  "PubMedID"                             publicationArgs)
                     doi
                     (getFieldValueByName  "AuthorList"                           publicationArgs)
                     (getFieldValueByName  "PublicationTitle"                     publicationArgs)
                     (getFieldValueByName  "PublicationStatus"                    publicationArgs)
                     (getFieldValueByName  "PublicationStatusTermAccessionNumber" publicationArgs)
                     (getFieldValueByName  "StatusTermSourceREF"                  publicationArgs)
                     []
            
            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let publications = investigation.Publications

            if API.Publication.existsByDoi doi publications then           
                if verbosity >= 1 then printfn "Publication with the doi %s already exists in the investigation" doi
                investigation
            else
                API.Publication.add publications publication
                |> API.Investigation.setPublications investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication Unregister"

            let doi = getFieldValueByName  "DOI" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let publications = investigation.Publications

            if API.Publication.existsByDoi doi publications then           
                API.Publication.removeByDoi doi publications
                |> API.Investigation.setPublications investigation
            else
                if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the investigation" doi
                investigation

            |> Investigation.toFile investigationFilePath

        /// Gets an existing publication by its doi and prints its metadata
        let get (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication Get"

            let doi = getFieldValueByName  "DOI" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            match API.Publication.tryGetByDoi doi investigation.Publications with
            | Some publication ->
                [publication]
                |> Prompt.serializeXSLXWriterOutput (Publications.writePublications "Publication")
                |> printfn "%s"

            | None -> 
                if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the investigation" doi
                

        /// Lists the full names of all persons included in the investigation
        let list (arcConfiguration:ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            investigation.Publications
            |> Seq.iter (fun publication ->
                printfn "Publication (DOI): %s" publication.DOI
            )