namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing
open IsaXLSX.InvestigationFile

/// ArcCommander Study API functions that get executed by the study focused subcommand verbs
module StudyAPI =

    /// Initializes a new empty study file in the arc.
    let init (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = 
    
        let studyFilePath = IsaModelConfiguration.tryGetStudiesFilePath arcConfiguration |> Option.get

        System.IO.File.Create studyFilePath
        |> ignore

    /// Updates an existing study info in the arc with the given study metadata contained in cliArgs.
    let update (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) =
    
        let identifier = getFieldValueByName "Identifier" studyArgs

        let studyInfo = 
            StudyInfo.create
                (identifier)
                (getFieldValueByName "Title"                studyArgs)
                (getFieldValueByName "Description"          studyArgs)
                (getFieldValueByName "SubmissionDate"       studyArgs)
                (getFieldValueByName "PublicReleaseDate"    studyArgs)
                (IsaModelConfiguration.tryGetStudiesFileName arcConfiguration |> Option.get)
                []
        
        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = IO.fromFile investigationFilePath

        match API.Study.tryGetByIdentifier identifier investigation with
        | Some study -> 
            API.Study.updateBy ((=) study) {study with Info = studyInfo} investigation
        | None -> 
            investigation
        |> IO.toFile investigationFilePath

    // /// Opens an existing study file in the arc with the text editor set in globalArgs, additionally setting the given study metadata contained in cliArgs.
    /// Opens the existing study info in the arc with the text editor set in globalArgs.
    let edit (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = 

        let identifier = getFieldValueByName "Identifier" studyArgs

        let editor = GeneralConfiguration.getEditor arcConfiguration
        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = IO.fromFile investigationFilePath

        match API.Study.tryGetByIdentifier identifier investigation with
        | Some study -> 
            let editedStudyInfo =
                ArgumentProcessing.Prompt.createIsaItemQuery editor workDir IO.writeStudyInfo 
                    (IO.readStudyInfo 1 >> fun (_,_,_,item) -> item) 
                    study.Info
                         
            API.Study.updateBy ((=) study) {study with Info = editedStudyInfo} investigation
        | None -> 
            investigation
        |> IO.toFile investigationFilePath

    /// Registers an existing study in the arc's investigation file with the given study metadata contained in cliArgs.
    let register (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) =

        let identifier = getFieldValueByName "Identifier" studyArgs

        let studyInfo = 
            StudyInfo.create
                (identifier)
                (getFieldValueByName "Title"                studyArgs)
                (getFieldValueByName "Description"          studyArgs)
                (getFieldValueByName "SubmissionDate"       studyArgs)
                (getFieldValueByName "PublicReleaseDate"    studyArgs)
                (IsaModelConfiguration.tryGetStudiesFileName arcConfiguration |> Option.get)
                []
        
        let study = Study.create studyInfo [] [] [] [] [] []

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = IO.fromFile investigationFilePath

        match API.Study.tryGetByIdentifier identifier investigation with
        | Some study -> 
            investigation
        | None -> 
            API.Study.add study investigation          
        |> IO.toFile investigationFilePath

    /// Creates a new study file in the arc and registers it in the arc's investigation file with the given study metadata contained in cliArgs.
    let add (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = 
        init arcConfiguration studyArgs
        register arcConfiguration studyArgs

    /// Deletes the study file from the arc.
    let delete (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = 
    
        let studyFilePath = IsaModelConfiguration.tryGetStudiesFilePath arcConfiguration |> Option.get

        System.IO.File.Delete studyFilePath
        |> ignore

    /// Unregisters an existing study from the arc's investigation file.
    let unregister (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) =

        let identifier = getFieldValueByName "Identifier" studyArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = IO.fromFile investigationFilePath

        API.Study.removeByIdentifier identifier investigation         
        |> IO.toFile investigationFilePath

    /// Removes a study file from the arc and unregisters it from the investigation file
    let remove (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = 
        delete arcConfiguration studyArgs
        unregister arcConfiguration studyArgs

    /// Lists all study identifiers registered in this arc's investigation file
    let get (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) =

        let identifier = getFieldValueByName "Identifier" studyArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get  


        let investigation = IO.fromFile investigationFilePath
        
        match API.Study.tryGetByIdentifier identifier investigation with
        | Some study ->
            printf "%s:%s" StudyInfo.IdentifierLabel study.Info.Identifier
            printf "%s:%s" StudyInfo.TitleLabel study.Info.Title
            printf "%s:%s" StudyInfo.DescriptionLabel study.Info.Description
            printf "%s:%s" StudyInfo.PublicReleaseDateLabel study.Info.PublicReleaseDate
            printf "%s:%s" StudyInfo.SubmissionDateLabel study.Info.SubmissionDate
            printf "%s:%s" StudyInfo.FileNameLabel study.Info.FileName
        | None -> ()

    /// Lists all study identifiers registered in this arc's investigation file
    let list (arcConfiguration:ArcConfiguration) =
        
        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get  
        printfn "InvestigationFile: %s"  investigationFilePath

        let investigation = IO.fromFile investigationFilePath
        
        if List.isEmpty investigation.Studies then 
            printfn "The Investigation contains no studies"
        else 
            investigation.Studies
            |> List.iter (fun s ->
            
                printfn "Study: %s" s.Info.Identifier
            )

    /// Functions for altering investigation contacts
    module Contacts =

        /// Updates an existing person in the arc investigation study with the given person metadata contained in cliArgs.
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

            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Person.updateByFullName person study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> investigation
            |> IO.toFile investigationFilePath
        
        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            printf "Start assay edit"
            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let lastName = (getFieldValueByName  "LastName"   personArgs)
            let firstName = (getFieldValueByName  "FirstName"     personArgs)
            let midInitials = (getFieldValueByName  "MidInitials"  personArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                match API.Study.Person.tryGetByFullName firstName midInitials lastName study with
                | Some person -> 
                    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                        (List.singleton >> IO.writePersons "Person") 
                        (IO.readPersons "Person" 1 >> fun (_,_,_,items) -> items.Head) 
                        person
                    |> fun p -> API.Study.Person.updateBy ((=) person) p study
                    |> fun s -> API.Study.updateByIdentifier s investigation
                | None ->
                    investigation
            | None -> 
                investigation
            |> IO.toFile investigationFilePath


        /// Registers a person in the arc investigation study with the given person metadata contained in personArgs.
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
            
            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Person.add person study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> 
                let info = StudyInfo.create studyIdentifier "" "" "" "" "" []
                let study = Study.create info [] [] [] [] [] [person]                 
                API.Study.add study investigation   
            |> IO.toFile investigationFilePath

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let lastName = (getFieldValueByName  "LastName"   personArgs)
            let firstName = (getFieldValueByName  "FirstName"     personArgs)
            let midInitials = (getFieldValueByName  "MidInitials"  personArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Person.removeByFullName firstName midInitials lastName study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> 
                investigation
            |> IO.toFile investigationFilePath

        /// Gets an existing person by fullname (lastName,firstName,MidInitials) and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let lastName = (getFieldValueByName  "LastName"   personArgs)
            let firstName = (getFieldValueByName  "FirstName"     personArgs)
            let midInitials = (getFieldValueByName  "MidInitials"  personArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                match API.Study.Person.tryGetByFullName firstName midInitials lastName study with
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
            | None -> ()

        /// Lists the full names of all persons included in the investigation
        let list (arcConfiguration:ArcConfiguration) = 

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath


            investigation.Studies
            |> Seq.iter (fun study ->
                let persons = study.Contacts
                if Seq.isEmpty persons |> not then
                    printfn "Study: %s" study.Info.Identifier
                    persons 
                    |> Seq.iter (fun person -> 
                        if person.MidInitials = "" then
                            printfn "--Person: %s %s" person.FirstName person.LastName
                        else
                            printfn "--Person: %s %s %s" person.FirstName person.MidInitials person.LastName)
            )

    /// Functions for altering investigation Publications
    module Publications =

        /// Updates an existing publication in the arc investigation study with the given publication metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let publication = 
                 Publication.create
                     (getFieldValueByName  "PubMedID"                             publicationArgs)
                     (getFieldValueByName  "DOI"                                  publicationArgs)
                     (getFieldValueByName  "AuthorList"                           publicationArgs)
                     (getFieldValueByName  "PublicationTitle"                     publicationArgs)
                     (getFieldValueByName  "PublicationStatus"                    publicationArgs)
                     (getFieldValueByName  "PublicationStatusTermAccessionNumber" publicationArgs)
                     (getFieldValueByName  "StatusTermSourceREF"                  publicationArgs)
                     []

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Publication.updateByDoi publication study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> investigation
            |> IO.toFile investigationFilePath
        
        /// Opens an existing publication by doi in the arc investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let doi = (getFieldValueByName  "DOI"   publicationArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                match API.Study.Publication.tryGetByDOI doi study with
                | Some publication -> 
                    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                        (List.singleton >> IO.writePublications "Publication") 
                        (IO.readPublications "Publication" 1 >> fun (_,_,_,items) -> items.Head) 
                        publication
                    |> fun p -> API.Study.Publication.updateBy ((=) publication) p study
                    |> fun s -> API.Study.updateByIdentifier s investigation
                | None ->
                    investigation
            | None -> 
                investigation
            |> IO.toFile investigationFilePath


        /// Registers a publication in the arc investigation study with the given publication metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let publication = 
                 Publication.create
                     (getFieldValueByName  "PubMedID"                             publicationArgs)
                     (getFieldValueByName  "DOI"                                  publicationArgs)
                     (getFieldValueByName  "AuthorList"                           publicationArgs)
                     (getFieldValueByName  "PublicationTitle"                     publicationArgs)
                     (getFieldValueByName  "PublicationStatus"                    publicationArgs)
                     (getFieldValueByName  "PublicationStatusTermAccessionNumber" publicationArgs)
                     (getFieldValueByName  "StatusTermSourceREF"                  publicationArgs)
                     []
            
            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Publication.add publication study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> 
                let info = StudyInfo.create studyIdentifier "" "" "" "" "" []
                let study = Study.create info [] [publication] [] [] [] []                 
                API.Study.add study investigation   
            |> IO.toFile investigationFilePath

        /// Opens an existing publication by doi in the arc investigation study with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let doi = (getFieldValueByName  "DOI"   publicationArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Publication.removeByDoi doi study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> 
                investigation
            |> IO.toFile investigationFilePath

        /// Gets an existing publication by doi from the arc investigation study and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let doi = (getFieldValueByName  "DOI"   publicationArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                match API.Study.Publication.tryGetByDOI doi study with
                | Some publication ->
                    printfn "%s:%s" Publication.PubMedIDLabel publication.PubMedID
                    printfn "%s:%s" Publication.DOILabel publication.DOI
                    printfn "%s:%s" Publication.AuthorListLabel publication.AuthorList
                    printfn "%s:%s" Publication.TitleLabel publication.Title
                    printfn "%s:%s" Publication.StatusLabel publication.Status
                    printfn "%s:%s" Publication.StatusTermAccessionNumberLabel publication.StatusTermAccessionNumber
                    printfn "%s:%s" Publication.StatusTermSourceREFLabel publication.StatusTermSourceREF
                | None -> ()
            | None -> ()

        /// Lists the dois of all publications included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath


            investigation.Studies
            |> Seq.iter (fun study ->
                let publications = study.Publications
                if Seq.isEmpty publications |> not then
                    printfn "Study: %s" study.Info.Identifier
                    publications
                    |> Seq.iter (fun publication -> printfn "--Publication DOI: %s" publication.DOI)
            )

    /// Functions for altering investigation Designs
    module Designs =

        /// Updates an existing design in the arc investigation study with the given design metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (designArgs : Map<string,Argument>) =

            let design = 
                 Design.create
                     (getFieldValueByName  "DesignType"                 designArgs)
                     (getFieldValueByName  "TypeTermAccessionNumber"    designArgs)
                     (getFieldValueByName  "TypeTermSourceREF"          designArgs)

                     []

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            let studyIdentifier = getFieldValueByName "StudyIdentifier" designArgs

            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Design.updateByDesignType design study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> investigation
            |> IO.toFile investigationFilePath
        
        /// Opens an existing design by design type in the arc investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (designArgs : Map<string,Argument>) =

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let designType = (getFieldValueByName  "DesignType"   designArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" designArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                match API.Study.Design.tryGetByDesignType designType study with
                | Some design -> 
                    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                        (List.singleton >> IO.writeDesigns "Design") 
                        (IO.readDesigns "Design" 1 >> fun (_,_,_,items) -> items.Head) 
                        design
                    |> fun d -> API.Study.Design.updateBy ((=) design) d study
                    |> fun s -> API.Study.updateByIdentifier s investigation
                | None ->
                    investigation
            | None -> 
                investigation
            |> IO.toFile investigationFilePath


        /// Registers a design in the arc investigation study with the given publication metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (designArgs : Map<string,Argument>) =

            let design = 
                Design.create
                    (getFieldValueByName  "DesignType"                 designArgs)
                    (getFieldValueByName  "TypeTermAccessionNumber"    designArgs)
                    (getFieldValueByName  "TypeTermSourceREF"          designArgs)

                    []
            
            let studyIdentifier = getFieldValueByName "StudyIdentifier" designArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Design.add design study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> 
                let info = StudyInfo.create studyIdentifier "" "" "" "" "" []
                let study = Study.create info [design] [] [] [] [] []                 
                API.Study.add study investigation   
            |> IO.toFile investigationFilePath

        /// Opens an existing design by design type in the arc investigation study with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (designArgs : Map<string,Argument>) =

            let designType = (getFieldValueByName  "DesignType"   designArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" designArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Design.removeByDesignType designType study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> 
                investigation
            |> IO.toFile investigationFilePath

        /// Gets an existing design by design type from the arc investigation study and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (designArgs : Map<string,Argument>) =

            let designType = (getFieldValueByName  "DesignType"   designArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" designArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                match API.Study.Design.tryGetByDesignType designType study with
                | Some design ->
                    printfn "%s:%s" Design.DesignTypeLabel              design.DesignType
                    printfn "%s:%s" Design.TypeTermAccessionNumberLabel design.TypeTermAccessionNumber
                    printfn "%s:%s" Design.TypeTermSourceREFLabel       design.TypeTermSourceREF
                | None -> ()
            | None -> ()

        /// Lists the designs included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath


            investigation.Studies
            |> Seq.iter (fun study ->
                let designs = study.DesignDescriptors
                if Seq.isEmpty designs |> not then
                    printfn "Study: %s" study.Info.Identifier
                    designs
                    |> Seq.iter (fun design -> printfn "--Design Type: %s" design.DesignType)
            )

    /// Functions for altering investigation factors
    module Factors =

        /// Updates an existing factor in the arc investigation study with the given factor metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (factorArgs : Map<string,Argument>) =

            let factor = 
                 Factor.create
                    (getFieldValueByName  "Name"                 factorArgs)
                    (getFieldValueByName  "FactorType"                 factorArgs)
                    (getFieldValueByName  "TypeTermAccessionNumber"    factorArgs)
                    (getFieldValueByName  "TypeTermSourceREF"          factorArgs)

                     []

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Factor.updateByName factor study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> investigation
            |> IO.toFile investigationFilePath
        
        /// Opens an existing factor by name in the arc investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (factorArgs : Map<string,Argument>) =

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let name = (getFieldValueByName  "Name" factorArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                match API.Study.Factor.tryGetByName name study with
                | Some factor -> 
                    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                        (List.singleton >> IO.writeFactors "Factor") 
                        (IO.readFactors "Factor" 1 >> fun (_,_,_,items) -> items.Head) 
                        factor
                    |> fun f -> API.Study.Factor.updateBy ((=) factor) f study
                    |> fun s -> API.Study.updateByIdentifier s investigation
                | None ->
                    investigation
            | None -> 
                investigation
            |> IO.toFile investigationFilePath


        /// Registers a factor in the arc investigation study with the given factor metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (factorArgs : Map<string,Argument>) =

            let factor = 
                 Factor.create
                    (getFieldValueByName  "Name"                    factorArgs)
                    (getFieldValueByName  "FactorType"              factorArgs)
                    (getFieldValueByName  "TypeTermAccessionNumber" factorArgs)
                    (getFieldValueByName  "TypeTermSourceREF"       factorArgs)

                     []

            
            let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Factor.add factor study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> 
                let info = StudyInfo.create studyIdentifier "" "" "" "" "" []
                let study = Study.create info [] [] [factor] [] [] []                 
                API.Study.add study investigation   
            |> IO.toFile investigationFilePath

        /// Opens an existing factor by name in the arc investigation study with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (factorArgs : Map<string,Argument>) =

            let name = (getFieldValueByName  "Name" factorArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Factor.removeByName name study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> 
                investigation
            |> IO.toFile investigationFilePath

        /// Gets an existing factor by name from the arc investigation study and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (factorArgs : Map<string,Argument>) =

            let name = (getFieldValueByName  "Name" factorArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                match API.Study.Factor.tryGetByName name study with
                | Some factor ->
                    printfn "%s:%s" Factor.NameLabel                    factor.Name
                    printfn "%s:%s" Factor.FactorTypeLabel              factor.FactorType
                    printfn "%s:%s" Factor.TypeTermAccessionNumberLabel factor.TypeTermAccessionNumber
                    printfn "%s:%s" Factor.TypeTermSourceREFLabel       factor.TypeTermSourceREF
                | None -> ()
            | None -> ()

        /// Lists the factors included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath


            investigation.Studies
            |> Seq.iter (fun study ->
                let factors = study.Factors
                if Seq.isEmpty factors |> not then
                    printfn "Study: %s" study.Info.Identifier
                    factors
                    |> Seq.iter (fun factor -> printfn "--Factor Name: %s" factor.Name)
            )

    /// Functions for altering investigation protocols
    module Protocols =

        /// Updates an existing protocol in the arc investigation study with the given protocol metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (protocolArgs : Map<string,Argument>) =

            let protocol = 
                 Protocol.create
                    (getFieldValueByName "Name"                                 protocolArgs)
                    (getFieldValueByName "ProtocolType"                         protocolArgs)
                    (getFieldValueByName "TypeTermAccessionNumber"              protocolArgs)
                    (getFieldValueByName "TypeTermSourceREF"                    protocolArgs)
                    (getFieldValueByName "Description"                          protocolArgs)
                    (getFieldValueByName "URI"                                  protocolArgs)
                    (getFieldValueByName "Version"                              protocolArgs)
                    (getFieldValueByName "ParametersName"                       protocolArgs)
                    (getFieldValueByName "ParametersTermAccessionNumber"        protocolArgs)
                    (getFieldValueByName "ParametersTermSourceREF"              protocolArgs)
                    (getFieldValueByName "ComponentsName"                       protocolArgs)
                    (getFieldValueByName "ComponentsType"                       protocolArgs)
                    (getFieldValueByName "ComponentsTypeTermAccessionNumber"    protocolArgs)
                    (getFieldValueByName "ComponentsTypeTermSourceREF"          protocolArgs)
                    []


            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Protocol.updateByName protocol study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> investigation
            |> IO.toFile investigationFilePath
        
        /// Opens an existing protocol by name in the arc investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (protocolArgs : Map<string,Argument>) =

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let name = (getFieldValueByName  "Name" protocolArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                match API.Study.Protocol.tryGetByName name study with
                | Some protocol -> 
                    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                        (List.singleton >> IO.writeProtocols "Protocol") 
                        (IO.readProtocols "Protocol" 1 >> fun (_,_,_,items) -> items.Head) 
                        protocol
                    |> fun p -> API.Study.Protocol.updateBy ((=) protocol) p study
                    |> fun s -> API.Study.updateByIdentifier s investigation
                | None ->
                    investigation
            | None -> 
                investigation
            |> IO.toFile investigationFilePath


        /// Registers a protocol in the arc investigation study with the given protocol metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (protocolArgs : Map<string,Argument>) =

            let protocol = 
                 Protocol.create
                    (getFieldValueByName "Name"                                 protocolArgs)
                    (getFieldValueByName "ProtocolType"                         protocolArgs)
                    (getFieldValueByName "TypeTermAccessionNumber"              protocolArgs)
                    (getFieldValueByName "TypeTermSourceREF"                    protocolArgs)
                    (getFieldValueByName "Description"                          protocolArgs)
                    (getFieldValueByName "URI"                                  protocolArgs)
                    (getFieldValueByName "Version"                              protocolArgs)
                    (getFieldValueByName "ParametersName"                       protocolArgs)
                    (getFieldValueByName "ParametersTermAccessionNumber"        protocolArgs)
                    (getFieldValueByName "ParametersTermSourceREF"              protocolArgs)
                    (getFieldValueByName "ComponentsName"                       protocolArgs)
                    (getFieldValueByName "ComponentsType"                       protocolArgs)
                    (getFieldValueByName "ComponentsTypeTermAccessionNumber"    protocolArgs)
                    (getFieldValueByName "ComponentsTypeTermSourceREF"          protocolArgs)
                    []

            
            let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Protocol.add protocol study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> 
                let info = StudyInfo.create studyIdentifier "" "" "" "" "" []
                let study = Study.create info [] [] [] [] [protocol] []                 
                API.Study.add study investigation   
            |> IO.toFile investigationFilePath

        /// Opens an existing protocol by name in the arc investigation study with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (protocolArgs : Map<string,Argument>) =

            let name = (getFieldValueByName  "Name" protocolArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                API.Study.Protocol.removeByName name study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None -> 
                investigation
            |> IO.toFile investigationFilePath

        /// Gets an existing protocol by name from the arc investigation study and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (protocolArgs : Map<string,Argument>) =

            let name = (getFieldValueByName  "Name" protocolArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath
            match API.Study.tryGetByIdentifier studyIdentifier investigation with
            | Some study -> 
                match API.Study.Protocol.tryGetByName name study with
                | Some protocol ->
                    printfn "%s:%s" Protocol.NameLabel                              protocol.Name
                    printfn "%s:%s" Protocol.ProtocolTypeLabel                      protocol.ProtocolType
                    printfn "%s:%s" Protocol.TypeTermAccessionNumberLabel           protocol.TypeTermAccessionNumber
                    printfn "%s:%s" Protocol.TypeTermSourceREFLabel                 protocol.TypeTermSourceREF
                    printfn "%s:%s" Protocol.DescriptionLabel                       protocol.Description
                    printfn "%s:%s" Protocol.URILabel                               protocol.URI
                    printfn "%s:%s" Protocol.VersionLabel                           protocol.Version
                    printfn "%s:%s" Protocol.ParametersNameLabel                    protocol.ParametersName
                    printfn "%s:%s" Protocol.ParametersTermAccessionNumberLabel     protocol.ParametersTermAccessionNumber
                    printfn "%s:%s" Protocol.ParametersTermSourceREFLabel           protocol.ParametersTermSourceREF
                    printfn "%s:%s" Protocol.ComponentsNameLabel                    protocol.ComponentsName
                    printfn "%s:%s" Protocol.ComponentsTypeLabel                    protocol.ComponentsType
                    printfn "%s:%s" Protocol.ComponentsTypeTermAccessionNumberLabel protocol.ComponentsTypeTermAccessionNumber
                    printfn "%s:%s" Protocol.ComponentsTypeTermSourceREFLabel       protocol.ComponentsTypeTermSourceREF
                | None -> ()
            | None -> ()

        /// Lists the protocols included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = IO.fromFile investigationFilePath

            investigation.Studies
            |> Seq.iter (fun study ->
                let protocols = study.Protocols
                if Seq.isEmpty protocols |> not then
                    printfn "Study: %s" study.Info.Identifier
                    protocols
                    |> Seq.iter (fun factor -> printfn "--Protocol Name: %s" factor.Name)
            )