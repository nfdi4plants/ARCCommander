namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISADotNet
open ISADotNet.XLSX


/// ArcCommander Study API functions that get executed by the study focused subcommand verbs
module StudyAPI =

    /// Initializes a new empty study file in the arc.
    let init (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = 
            
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Study Init"

        let studyFilePath = IsaModelConfiguration.tryGetStudiesFilePath arcConfiguration |> Option.get

        System.IO.File.Create studyFilePath
        |> ignore

    /// Updates an existing study info in the arc with the given study metadata contained in cliArgs.
    let update (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = // NotImplementedException()
    
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Study Update"

        // ?TODO? <- Test this : Add updateoption which updates by existing values and appends list
        let updateOption = if containsFlag "ReplaceWithEmptyValues" studyArgs then API.Update.UpdateAllAppendLists else API.Update.UpdateByExisting            

        let identifier = getFieldValueByName "Identifier" studyArgs

        let study = 
            let studyInfo = 
                Study.StudyInfo.create
                    (identifier)
                    (getFieldValueByName "Title"                studyArgs)
                    (getFieldValueByName "Description"          studyArgs)
                    (getFieldValueByName "SubmissionDate"       studyArgs)
                    (getFieldValueByName "PublicReleaseDate"    studyArgs)
                    (IsaModelConfiguration.tryGetStudiesFileName arcConfiguration |> Option.get)
                    []
            Study.fromParts studyInfo [] [] [] [] [] [] 

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = Investigation.fromFile investigationFilePath

        let studies = investigation.Studies

        if API.Study.existsByIdentifier identifier studies then
            API.Study.updateByIdentifier updateOption study studies
            |> API.Investigation.setStudies investigation
        else 
            if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation" identifier
            investigation
        |> Investigation.toFile investigationFilePath
        

    // /// Opens an existing study file in the arc with the text editor set in globalArgs, additionally setting the given study metadata contained in cliArgs.
    /// Opens the existing study info in the arc with the text editor set in globalArgs.
    let edit (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = 

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Study Edit"


        let identifier = getFieldValueByName "Identifier" studyArgs

        let editor = GeneralConfiguration.getEditor arcConfiguration
        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = Investigation.fromFile investigationFilePath

        let studies = investigation.Studies

        match API.Study.tryGetByIdentifier identifier studies with
        | Some study -> 
            let editedStudy =
                ArgumentProcessing.Prompt.createIsaItemQuery editor workDir Study.StudyInfo.WriteStudyInfo 
                    (Study.StudyInfo.ReadStudyInfo 1 >> fun (_,_,_,item) -> Study.fromParts item [] [] [] [] [] []) 
                    study                   
            API.Study.updateBy ((=) study) API.Update.UpdateAllAppendLists editedStudy studies
            |> API.Investigation.setStudies investigation
        | None -> 
            if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation" identifier
            investigation
        |> Investigation.toFile investigationFilePath

    /// Registers an existing study in the arc's investigation file with the given study metadata contained in cliArgs.
    let register (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Study Register"

        let identifier = getFieldValueByName "Identifier" studyArgs

        let study = 
            let studyInfo = 
                Study.StudyInfo.create
                    (identifier)
                    (getFieldValueByName "Title"                studyArgs)
                    (getFieldValueByName "Description"          studyArgs)
                    (getFieldValueByName "SubmissionDate"       studyArgs)
                    (getFieldValueByName "PublicReleaseDate"    studyArgs)
                    (IsaModelConfiguration.tryGetStudiesFileName arcConfiguration |> Option.get)
                    []
            Study.fromParts studyInfo [] [] [] [] [] [] 

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath

        let studies = investigation.Studies

        match API.Study.tryGetByIdentifier identifier studies with
        | Some study -> 
            if verbosity >= 1 then printfn "Study with the identifier %s already exists in the investigation file" identifier
            investigation
        | None -> 
            API.Study.add studies study
            |> API.Investigation.setStudies investigation
        |> Investigation.toFile investigationFilePath

    /// Creates a new study file in the arc and registers it in the arc's investigation file with the given study metadata contained in cliArgs.
    let add (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = 
        init arcConfiguration studyArgs
        register arcConfiguration studyArgs

    /// Deletes the study file from the arc.
    let delete (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = 
    
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Study Delete"

        let studyFilePath = IsaModelConfiguration.tryGetStudiesFilePath arcConfiguration |> Option.get

        System.IO.File.Delete studyFilePath
        |> ignore

    /// Unregisters an existing study from the arc's investigation file.
    let unregister (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Study Unregister"

        let identifier = getFieldValueByName "Identifier" studyArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath

        let studies = investigation.Studies

        match API.Study.tryGetByIdentifier identifier studies with
        | Some study -> 
            API.Study.removeByIdentifier identifier studies 
            |> API.Investigation.setStudies investigation            
        | None -> 
            if verbosity >= 1 then printfn "Study with the identifier %s does not in the investigation file" identifier

            investigation
        |> Investigation.toFile investigationFilePath

    /// Removes a study file from the arc and unregisters it from the investigation file
    let remove (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = 
        delete arcConfiguration studyArgs
        unregister arcConfiguration studyArgs

    /// Lists all study identifiers registered in this arc's investigation file
    let get (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Study Get"

        let identifier = getFieldValueByName "Identifier" studyArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get  

        let investigation = Investigation.fromFile investigationFilePath
        

        match API.Study.tryGetByIdentifier identifier investigation.Studies with
        | Some study ->
            study
            |> Prompt.serializeXSLXWriterOutput Study.StudyInfo.WriteStudyInfo
            |> printfn "%s"
        | None -> 
            if verbosity >= 1 then printfn "Study with the identifier %s does not in the investigation file" identifier
            ()

    /// Lists all study identifiers registered in this arc's investigation file
    let list (arcConfiguration:ArcConfiguration) =
        
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Study List"

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get  
        printfn "InvestigationFile: %s"  investigationFilePath

        let investigation = Investigation.fromFile investigationFilePath
        
        if List.isEmpty investigation.Studies then 
            printfn "The Investigation contains no studies"
        else 
            investigation.Studies
            |> List.iter (fun s ->
            
                printfn "Study: %s" s.Identifier
            )

    /// Functions for altering investigation contacts
    module Contacts =

        /// Updates an existing person in the arc investigation study with the given person metadata contained in cliArgs.
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

            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let persons = study.Contacts
                if API.Person.existsByFullName lastName midInitials firstName persons then
                    API.Person.updateByFullName updateOption person persons
                    |> API.Study.setContacts study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                else
                    if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the study with the identifier %s" firstName midInitials lastName studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath
        
        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Person Edit"

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let lastName = (getFieldValueByName  "LastName"   personArgs)
            let firstName = (getFieldValueByName  "FirstName"     personArgs)
            let midInitials = (getFieldValueByName  "MidInitials"  personArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let persons = study.Contacts
                match API.Person.tryGetByFullName firstName midInitials lastName persons with
                | Some person -> 
                    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                        (List.singleton >> Contacts.writePersons "Person") 
                        (Contacts.readPersons "Person" 1 >> fun (_,_,_,items) -> items.Head) 
                        person
                    |> fun p -> API.Person.updateBy ((=) person) API.Update.UpdateAll p persons
                    |> API.Study.setContacts study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                | None ->
                    investigation
            | None -> 
                investigation
            |> Investigation.toFile investigationFilePath

        /// Registers a person in the arc investigation study with the given person metadata contained in personArgs.
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
            
            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let persons = study.Contacts
                if API.Person.existsByFullName firstName midInitials lastName persons then               
                    if verbosity >= 1 then printfn "Person with the name %s %s %s already exists in the investigation file" firstName midInitials lastName
                    investigation
                else
                    API.Person.add persons person
                    |> API.Study.setContacts study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
            | None ->
                printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath
    

        /// Opens an existing person by fullname (lastName,firstName,MidInitials) in the arc with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Person Unregister"

            let lastName = (getFieldValueByName  "LastName"   personArgs)
            let firstName = (getFieldValueByName  "FirstName"     personArgs)
            let midInitials = (getFieldValueByName  "MidInitials"  personArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let persons = study.Contacts
                if API.Person.existsByFullName firstName midInitials lastName persons then
                    API.Person.removeByFullName firstName midInitials lastName persons
                    |> API.Study.setContacts study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                else
                    if verbosity >= 1 then printfn "Person with the name %s %s %s  does not exist in the study with the identifier %s" firstName midInitials lastName studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath

        /// Gets an existing person by fullname (lastName,firstName,MidInitials) and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =
          
            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Person Get"

            let lastName = (getFieldValueByName  "LastName"   personArgs)
            let firstName = (getFieldValueByName  "FirstName"     personArgs)
            let midInitials = (getFieldValueByName  "MidInitials"  personArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match API.Study.tryGetByIdentifier studyIdentifier investigation.Studies with
            | Some study -> 
                match API.Person.tryGetByFullName firstName midInitials lastName study.Contacts with
                | Some person ->
                    [person]
                    |> Prompt.serializeXSLXWriterOutput (Contacts.writePersons "Person")
                    |> printfn "%s"
                | None -> printfn "Person with the name %s %s %s  does not exist in the study with the identifier %s" firstName midInitials lastName studyIdentifier
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier

        /// Lists the full names of all persons included in the investigation
        let list (arcConfiguration:ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Person List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            investigation.Studies
            |> Seq.iter (fun study ->
                let persons = study.Contacts
                if Seq.isEmpty persons |> not then
                    printfn "Study: %s" study.Identifier
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

            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let publications = study.Publications
                if API.Publication.existsByDoi doi publications then
                    API.Publication.updateByDOI updateOption publication publications
                    |> API.Study.setPublications study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll  s studies
                    |> API.Investigation.setStudies investigation
                else
                    if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the study with the identifier %s" doi studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath
        
        /// Opens an existing publication by doi in the arc investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication edit"

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let doi = (getFieldValueByName  "DOI"   publicationArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let publications = study.Publications
                match API.Publication.tryGetByDoi assayFileName assays with
                | Some publication ->                    
                    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                        (List.singleton >> Publications.writePublications "Publication") 
                        (Publications.readPublications "Publication" 1 >> fun (_,_,_,items) -> items.Head) 
                        publication
                    |> fun p -> API.Publication.updateBy ((=) publication) API.Update.UpdateAll p publications
                    |> API.Study.setPublications study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation

                | None ->
                    if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the study with the identifier %s" doi studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath


        /// Registers a publication in the arc investigation study with the given publication metadata contained in personArgs.
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
            
            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let publications = study.Publications
                if API.Publication.existsByDoi doi publications then           
                    if verbosity >= 1 then printfn "Publication with the doi %s already exists in the study with the identifier %s" doi studyIdentifier
                    investigation
                else
                    API.Publication.add publications publication
                    |> API.Study.setPublications study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
            | None ->
                printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing publication by doi in the arc investigation study with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication Unregister"

            let doi = (getFieldValueByName  "DOI"   publicationArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let publications = study.Publications
                if API.Publication.existsByDoi doi publications then           
                    API.Publication.removeByDoi doi publications
                    |> API.Study.setPublications study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                else
                    if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the study with the identifier %s" doi studyIdentifier
                    investigation                   
            | None ->
                printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath

        /// Gets an existing publication by doi from the arc investigation study and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication Get"

            let doi = (getFieldValueByName  "DOI"   publicationArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match API.Study.tryGetByIdentifier studyIdentifier investigation.Studies with
            | Some study -> 
                match API.Publication.tryGetByDoi doi study.Publications with
                | Some publication ->
                    [publication]
                    |> Prompt.serializeXSLXWriterOutput (Publications.writePublications "Publication")
                    |> printfn "%s"

                | None -> 
                    if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the study with the identifier %s" doi studyIdentifier
                    
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier

        /// Lists the dois of all publications included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            investigation.Studies
            |> Seq.iter (fun study ->
                let publications = study.Publications
                if Seq.isEmpty publications |> not then
                    printfn "Study: %s" study.Identifier
                    publications
                    |> Seq.iter (fun publication -> printfn "--Publication DOI: %s" publication.DOI)
            )

    /// Functions for altering investigation Designs
    module Designs =

        /// Updates an existing design in the arc investigation study with the given design metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (designArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Design update"

            let updateOption = if containsFlag "ReplaceWithEmptyValues" designArgs then API.Update.UpdateAll else API.Update.UpdateByExisting            

            let name = getFieldValueByName "DesignType" designArgs

            let design = 
                 DesignDescriptors.fromString
                     name
                     (getFieldValueByName  "TypeTermAccessionNumber"    designArgs)
                     (getFieldValueByName  "TypeTermSourceREF"          designArgs)

                     []

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let studyIdentifier = getFieldValueByName "StudyIdentifier" designArgs

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let designs = study.StudyDesignDescriptors
                if API.OntologyAnnotation.existsByName design.Name designs then
                    API.OntologyAnnotation.updateByName updateOption design designs
                    |> API.Study.setDescriptors study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll  s studies
                    |> API.Investigation.setStudies investigation
                else
                    if verbosity >= 1 then printfn "Design with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath
        
        /// Opens an existing design by design type in the arc investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (designArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Design Edit"

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let name = (getFieldValueByName  "DesignType"   designArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" designArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let designs = study.StudyDesignDescriptors
                match API.OntologyAnnotation.tryGetByName (AnnotationValue.fromString name) designs with
                | Some design ->                    
                    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                        (List.singleton >> DesignDescriptors.writeDesigns "Design") 
                        (DesignDescriptors.readDesigns "Design" 1 >> fun (_,_,_,items) -> items.Head) 
                        design
                    |> fun d -> API.OntologyAnnotation.updateBy ((=) design) API.Update.UpdateAll d designs
                    |> API.Study.setDescriptors study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation

                | None ->
                    if verbosity >= 1 then printfn "Design with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath


        /// Registers a design in the arc investigation study with the given publication metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (designArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Design Register"

            let name = getFieldValueByName  "DesignType"                 designArgs

            let design = 
                DesignDescriptors.fromString
                    name
                    (getFieldValueByName  "TypeTermAccessionNumber"    designArgs)
                    (getFieldValueByName  "TypeTermSourceREF"          designArgs)

                    []
            
            let studyIdentifier = getFieldValueByName "StudyIdentifier" designArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let designs = study.StudyDesignDescriptors
                if API.OntologyAnnotation.existsByName (AnnotationValue.fromString name) designs then           
                    if verbosity >= 1 then printfn "Design with the name %s already exists in the study with the identifier %s" name studyIdentifier
                    investigation
                else
                    API.OntologyAnnotation.add designs design
                    |> API.Study.setDescriptors study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
            | None ->
                printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing design by design type in the arc investigation study with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (designArgs : Map<string,Argument>) =
            
            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Design Unregister"

            let name = getFieldValueByName  "DesignType"   designArgs

            let studyIdentifier = getFieldValueByName "StudyIdentifier" designArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let designs = study.StudyDesignDescriptors
                if API.OntologyAnnotation.existsByName (AnnotationValue.fromString name) designs then           
                    API.OntologyAnnotation.removeByName (AnnotationValue.fromString name) designs
                    |> API.Study.setDescriptors study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                else
                    if verbosity >= 1 then printfn "Design with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                    investigation                   
            | None ->
                printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath

        /// Gets an existing design by design type from the arc investigation study and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (designArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Design Get"

            let name = getFieldValueByName  "DesignType"   designArgs

            let studyIdentifier = getFieldValueByName "StudyIdentifier" designArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match API.Study.tryGetByIdentifier studyIdentifier investigation.Studies with
            | Some study -> 
                match API.OntologyAnnotation.tryGetByName (AnnotationValue.fromString name) study.StudyDesignDescriptors with
                | Some design ->
                    [design]
                    |> Prompt.serializeXSLXWriterOutput (DesignDescriptors.writeDesigns "Design")
                    |> printfn "%s"
                | None -> 
                    if verbosity >= 1 then printfn "Design with the doi %s does not exist in the study with the identifier %s" name studyIdentifier                    
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier

        /// Lists the designs included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Design List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            investigation.Studies
            |> Seq.iter (fun study ->
                let designs = study.StudyDesignDescriptors
                if Seq.isEmpty designs |> not then
                    printfn "Study: %s" study.Identifier
                    designs
                    |> Seq.iter (fun design -> printfn "--Design Type: %s" (AnnotationValue.toString design.Name))
            )

    /// Functions for altering investigation factors
    module Factors =

        /// Updates an existing factor in the arc investigation study with the given factor metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (factorArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Factor update"

            let updateOption = if containsFlag "ReplaceWithEmptyValues" factorArgs then API.Update.UpdateAll else API.Update.UpdateByExisting            

            let name = getFieldValueByName  "Name" factorArgs

            let factor = 
                 Factors.fromString
                    name
                    (getFieldValueByName  "FactorType"                 factorArgs)
                    (getFieldValueByName  "TypeTermAccessionNumber"    factorArgs)
                    (getFieldValueByName  "TypeTermSourceREF"          factorArgs)
                     []

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let factors = study.Factors
                if API.Factor.existsByName name factors then
                    API.Factor.updateByName updateOption factor factors
                    |> API.Study.setFactors study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll  s studies
                    |> API.Investigation.setStudies investigation
                else
                    if verbosity >= 1 then printfn "Factor with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath
        
        /// Opens an existing factor by name in the arc investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (factorArgs : Map<string,Argument>) =
            
            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Factor Edit"

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let name = getFieldValueByName  "Name" factorArgs

            let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let factors = study.Factors
                match API.Factor.tryGetByName name factors with
                | Some factor ->                    
                    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                        (List.singleton >> Factors.writeFactors "Factor") 
                        (Factors.readFactors "Factor" 1 >> fun (_,_,_,items) -> items.Head) 
                        factor
                    |> fun f -> API.Factor.updateBy ((=) factor) API.Update.UpdateAll f factors
                    |> API.Study.setFactors study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation

                | None ->
                    if verbosity >= 1 then printfn "Factor with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath


        /// Registers a factor in the arc investigation study with the given factor metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (factorArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Factor Register"
            
            let name = getFieldValueByName  "Name" factorArgs

            let factor = 
                 Factors.fromString
                    name
                    (getFieldValueByName  "FactorType"                 factorArgs)
                    (getFieldValueByName  "TypeTermAccessionNumber"    factorArgs)
                    (getFieldValueByName  "TypeTermSourceREF"          factorArgs)
                     []
            
            let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let factors = study.Factors
                if API.Factor.existsByName name factors then           
                    if verbosity >= 1 then printfn "Factor with the name %s already exists in the study with the identifier %s" name studyIdentifier
                    investigation
                else
                    API.Factor.add factors factor
                    |> API.Study.setFactors study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
            | None ->
                printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing factor by name in the arc investigation study with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (factorArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Factor Unregister"
            
            let name = getFieldValueByName  "Name" factorArgs

            let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let factors = study.Factors
                if API.Factor.existsByName name factors then           
                    API.Factor.removeByName name factors
                    |> API.Study.setFactors study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                else
                    if verbosity >= 1 then printfn "Factor with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                    investigation                   
            | None ->
                printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath

        /// Gets an existing factor by name from the arc investigation study and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (factorArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Factor Get"

            let name = getFieldValueByName  "Name" factorArgs

            let studyIdentifier = getFieldValueByName "StudyIdentifier" factorArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match API.Study.tryGetByIdentifier studyIdentifier investigation.Studies with
            | Some study -> 
                match API.Factor.tryGetByName name study.Factors with
                | Some factor ->
                    [factor]
                    |> Prompt.serializeXSLXWriterOutput (Factors.writeFactors "Factor")
                    |> printfn "%s"
                | None -> 
                    if verbosity >= 1 then printfn "Factor with the doi %s does not exist in the study with the identifier %s" name studyIdentifier                    
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier


        /// Lists the factors included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 
            
            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Factor List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            investigation.Studies
            |> Seq.iter (fun study ->
                let factors = study.Factors
                if Seq.isEmpty factors |> not then
                    printfn "Study: %s" study.Identifier
                    factors
                    |> Seq.iter (fun factor -> printfn "--Factor Name: %s" factor.Name)
            )

    /// Functions for altering investigation protocols
    module Protocols =

        /// Updates an existing protocol in the arc investigation study with the given protocol metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (protocolArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Protocol Update"

            let updateOption = if containsFlag "ReplaceWithEmptyValues" protocolArgs then API.Update.UpdateAll else API.Update.UpdateByExisting            

            let name = getFieldValueByName "Name" protocolArgs

            let protocol = 
                 Protocols.fromString
                    name
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
            
            let investigation = Investigation.fromFile investigationFilePath

            let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let protocols = study.Protocols
                if API.Protocol.existsByName name protocols then
                    API.Protocol.updateByName updateOption protocol protocols
                    |> API.Study.setProtocols study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll  s studies
                    |> API.Investigation.setStudies investigation
                else
                    if verbosity >= 1 then printfn "Protocol with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath
        
        /// Opens an existing protocol by name in the arc investigation study with the text editor set in globalArgs.
        let edit (arcConfiguration:ArcConfiguration) (protocolArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Protocol Edit"

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let name = (getFieldValueByName  "Name" protocolArgs)

            let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
            
            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let protocols = study.Protocols
                match API.Protocol.tryGetByName name protocols with
                | Some protocol ->                    
                    ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                        (List.singleton >> Protocols.writeProtocols "Protocol") 
                        (Protocols.readProtocols "Protocol" 1 >> fun (_,_,_,items) -> items.Head) 
                        protocol
                    |> fun f -> API.Protocol.updateBy ((=) protocol) API.Update.UpdateAll f protocols
                    |> API.Study.setProtocols study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation

                | None ->
                    if verbosity >= 1 then printfn "Protocol with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath


        /// Registers a protocol in the arc investigation study with the given protocol metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (protocolArgs : Map<string,Argument>) =
           
            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Protocol Register"
            
            let name = getFieldValueByName "Name" protocolArgs

            let protocol = 
                 Protocols.fromString
                    name
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
            
            let investigation = Investigation.fromFile investigationFilePath

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let protocols = study.Protocols
                if API.Protocol.existsByName name protocols then           
                    if verbosity >= 1 then printfn "Protocol with the name %s already exists in the study with the identifier %s" name studyIdentifier
                    investigation
                else
                    API.Protocol.add protocols protocol
                    |> API.Study.setProtocols study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
            | None ->
                printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath

        /// Opens an existing protocol by name in the arc investigation study with the text editor set in globalArgs.
        let unregister (arcConfiguration:ArcConfiguration) (protocolArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Protocol Unregister"

            let name = getFieldValueByName  "Name" protocolArgs

            let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            let studies = investigation.Studies

            match API.Study.tryGetByIdentifier studyIdentifier studies with
            | Some study -> 
                let protocols = study.Protocols
                if API.Protocol.existsByName name protocols then           
                    API.Protocol.removeByName name protocols
                    |> API.Study.setProtocols study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                else
                    if verbosity >= 1 then printfn "Protocol with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                    investigation                   
            | None ->
                printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                investigation
            |> Investigation.toFile investigationFilePath


        /// Gets an existing protocol by name from the arc investigation study and prints its metadata.
        let get (arcConfiguration:ArcConfiguration) (protocolArgs : Map<string,Argument>) =
         
            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Protocol Get"

            let name = getFieldValueByName  "Name" protocolArgs

            let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match API.Study.tryGetByIdentifier studyIdentifier investigation.Studies with
            | Some study -> 
                match API.Protocol.tryGetByName name study.Protocols with
                | Some protocol ->
                    [protocol]
                    |> Prompt.serializeXSLXWriterOutput (Protocols.writeProtocols "Protocol")
                    |> printfn "%s"
                | None -> 
                    if verbosity >= 1 then printfn "Protocol with the doi %s does not exist in the study with the identifier %s" name studyIdentifier                    
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier


        /// Lists the protocols included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Protocol List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            investigation.Studies
            |> Seq.iter (fun study ->
                let protocols = study.Protocols
                if Seq.isEmpty protocols |> not then
                    printfn "Study: %s" study.Identifier
                    protocols
                    |> Seq.iter (fun factor -> printfn "--Protocol Name: %s" factor.Name)
            )