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
           
        let investigationInfo = 
            InvestigationInfo.create
                (getFieldValueByName "Identifier" investigationArgs)
                (getFieldValueByName "Title" investigationArgs)
                (getFieldValueByName "Description" investigationArgs)
                (getFieldValueByName "SubmissionDate" investigationArgs)
                (getFieldValueByName "PublicReleaseDate" investigationArgs)
                []

        let investigation = Investigation.create [] investigationInfo [] [] [] []

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
                          
        IO.toFile investigationFilePath investigation

    /// Updates the existing investigation file in the arc with the given investigation metadata contained in cliArgs.
    let update (arcConfiguration:ArcConfiguration) (investigationArgs : Map<string,Argument>) = 

        let investigationInfo = 
            InvestigationInfo.create
                (getFieldValueByName "Identifier" investigationArgs)
                (getFieldValueByName "Title" investigationArgs)
                (getFieldValueByName "Description" investigationArgs)
                (getFieldValueByName "SubmissionDate" investigationArgs)
                (getFieldValueByName "PublicReleaseDate" investigationArgs)
                []

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = IO.fromFile investigationFilePath

        {investigation with Info = investigationInfo}
        |> IO.toFile investigationFilePath

    /// Opens the existing investigation info in the arc with the text editor set in globalArgs.
    let edit (arcConfiguration:ArcConfiguration) =
       
        printfn "Start investigation edit"
        let editor = GeneralConfiguration.getEditor arcConfiguration
        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = IO.fromFile investigationFilePath

        let editedInvestigationInfo =
            ArgumentProcessing.Prompt.createIsaItemQuery editor workDir IO.writeInvestigationInfo 
                (IO.readInvestigationInfo 1 >> fun (_,_,_,item) -> item) 
                investigation.Info
               
        {investigation with Info = editedInvestigationInfo}
        |> IO.toFile investigationFilePath

    /// Deletes the existing investigation file in the arc if the given identifier matches the identifier set in the investigation file
    let delete (arcConfiguration:ArcConfiguration) (investigationArgs : Map<string,Argument>) = 
        printfn "Start investigation edit"       

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = IO.fromFile investigationFilePath

        let identifier = getFieldValueByName "Identifier" investigationArgs
        
        if identifier = investigation.Info.Identifier then
            System.IO.File.Delete investigationFilePath

    /// Functions for altering investigation contacts
    module Contacts =

        /// Updates an existing assay file in the arc with the given assay metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let person = 
                Person.create
                    (getFieldValueByName  "LastName"                    personArgs)
                    (getFieldValueByName  "FirstName"                   personArgs)
                    (getFieldValueByName  "MidInitials"                 personArgs)
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
            
            let investigation = IO.fromFile investigationFilePath

            API.Person.updateByFullName person investigation
            |> IO.toFile investigationFilePath
        
        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            printf "Start assay edit"
            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let personLastName = (getFieldValueByName  "LastName"   personArgs)
            let personFirstName = (getFieldValueByName  "FirstName"     personArgs)
            let personMidInitials = (getFieldValueByName  "MidInitials"  personArgs)

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Person.tryGetByFullName personFirstName personMidInitials personLastName investigation with
            | Some person -> 
                ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                    (List.singleton >> IO.writePersons "Person") 
                    (IO.readPersons "Person" 1 >> fun (_,_,_,items) -> items.Head) 
                    person
                |> fun p -> API.Person.updateBy ((=) person) p investigation
            | None ->
                investigation
            |> IO.toFile investigationFilePath


        /// Registers a person in the arc's investigation file with the given person metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let person = 
                Person.create
                    (getFieldValueByName  "LastName"                    personArgs)
                    (getFieldValueByName  "FirstName"                   personArgs)
                    (getFieldValueByName  "MidInitials"                 personArgs)
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
            
            let investigation = IO.fromFile investigationFilePath

            API.Person.add person investigation
            |> IO.toFile investigationFilePath

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let personLastName = (getFieldValueByName  "LastName"   personArgs)
            let personFirstName = (getFieldValueByName  "FirstName"     personArgs)
            let personMidInitials = (getFieldValueByName  "MidInitials"  personArgs)

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            API.Person.removeByFullName personFirstName personMidInitials personLastName investigation
            |> IO.toFile investigationFilePath

        /// Gets an existing person by fullname (lastName,firstName,MidInitials) and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let personLastName = (getFieldValueByName  "LastName"   personArgs)
            let personFirstName = (getFieldValueByName  "FirstName"     personArgs)
            let personMidInitials = (getFieldValueByName  "MidInitials"  personArgs)

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Person.tryGetByFullName personFirstName personMidInitials personLastName investigation with
            | Some person ->
                printfn "%s:%s" Person.LastNameLabel person.LastName
                printfn "%s:%s" Person.FirstNameLabel person.FirstName
                printfn "%s:%s" Person.MidInitialsLabel person.MidInitials
                printfn "%s:%s" Person.EmailLabel person.Email
                printfn "%s:%s" Person.PhoneLabel person.Phone
                printfn "%s:%s" Person.FaxLabel person.Fax
                printfn "%s:%s" Person.AddressLabel person.Address
                printfn "%s:%s" Person.AffiliationLabel person.Affiliation
                printfn "%s:%s" Person.RolesLabel person.Roles
                printfn "%s:%s" Person.RolesTermAccessionNumberLabel person.RolesTermAccessionNumber
                printfn "%s:%s" Person.RolesTermSourceREFLabel person.RolesTermSourceREF
            | None -> ()

        /// Lists the full names of all persons included in the investigation
        let list (arcConfiguration:ArcConfiguration) = 

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

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

            let publication = 
                Publications.fr
                    (getFieldValueByName  "PubMedID"                    publicationArgs)
                    (getFieldValueByName  "DOI"                         publicationArgs)
                    (getFieldValueByName  "AuthorList"                  publicationArgs)
                    (getFieldValueByName  "PublicationTitle"            publicationArgs)
                    (getFieldValueByName  "PublicationStatus"           publicationArgs)
                    (getFieldValueByName  "StatusTermAccessionNumber"   publicationArgs)
                    (getFieldValueByName  "StatusTermSourceREF"         publicationArgs)
                    []

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            API.Publication.updateByDoi publication investigation
            |> IO.toFile investigationFilePath
        
        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            printf "Start assay edit"
            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let doi = (getFieldValueByName  "DOI"   publicationArgs)

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Publication.tryGetByDOI doi investigation with
            | Some publication -> 
                ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                    (List.singleton >> IO.writePublications "Publication") 
                    (IO.readPublications "Publication" 1 >> fun (_,_,_,items) -> items.Head) 
                    publication
                |> fun p -> API.Publication.updateBy ((=) publication) p investigation
            | None ->
                investigation
            |> IO.toFile investigationFilePath


        /// Registers a person in the arc's investigation file with the given person metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let publication = 
                Publication.create
                    (getFieldValueByName  "PubMedID"                             publicationArgs)
                    (getFieldValueByName  "DOI"                                  publicationArgs)
                    (getFieldValueByName  "AuthorList"                           publicationArgs)
                    (getFieldValueByName  "PublicationTitle"                     publicationArgs)
                    (getFieldValueByName  "PublicationStatus"                    publicationArgs)
                    (getFieldValueByName  "StatusTermAccessionNumber" publicationArgs)
                    (getFieldValueByName  "StatusTermSourceREF"                  publicationArgs)
                    []
            
            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            API.Publication.add publication investigation
            |> IO.toFile investigationFilePath

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let doi = getFieldValueByName  "DOI"   publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            API.Publication.removeByDoi doi investigation
            |> IO.toFile investigationFilePath

        /// Gets an existing publication by its doi and prints its metadata
        let get (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let doi = getFieldValueByName  "DOI"   publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Publication.tryGetByDOI doi investigation with
            | Some publication ->
                printfn "%s:%s" Publication.PubMedIDLabel publication.PubMedID
                printfn "%s:%s" Publication.DOILabel publication.DOI
                printfn "%s:%s" Publication.AuthorListLabel publication.AuthorList
                printfn "%s:%s" Publication.TitleLabel publication.Title
                printfn "%s:%s" Publication.StatusLabel publication.Status
                printfn "%s:%s" Publication.StatusTermAccessionNumberLabel publication.StatusTermAccessionNumber
                printfn "%s:%s" Publication.StatusTermSourceREFLabel publication.StatusTermSourceREF
            | None -> ()

        /// Lists the full names of all persons included in the investigation
        let list (arcConfiguration:ArcConfiguration) = 

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            investigation.Publications
            |> Seq.iter (fun publication ->
                printfn "Publication (DOI): %s" publication.DOI
            )