namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISADotNet
open ISADotNet.XLSX

/// ArcCommander Study API functions that get executed by the study focused subcommand verbs
module StudyAPI =

    module StudyFile =
    
        let exists (arcConfiguration:ArcConfiguration) (identifier : string) =
            IsaModelConfiguration.getStudiesFilePath identifier arcConfiguration
            |> System.IO.File.Exists
    
        let create (arcConfiguration:ArcConfiguration) (identifier : string) =
            IsaModelConfiguration.getStudiesFilePath identifier arcConfiguration
            |> FSharpSpreadsheetML.Spreadsheet.initWithSST identifier
            |> FSharpSpreadsheetML.Spreadsheet.close

    /// Initializes a new empty study file in the arc.
    let init (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = 
            
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Study Init"

        let identifier = getFieldValueByName "Identifier" studyArgs

        if StudyFile.exists arcConfiguration identifier then
            if verbosity >= 1 then printfn "Study file already exists"
        else 
            StudyFile.create arcConfiguration identifier

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
                    (IsaModelConfiguration.getStudiesFileName identifier arcConfiguration)
                    []
            Study.fromParts studyInfo [] [] [] [] [] [] 

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
       
        let investigation = Investigation.fromFile investigationFilePath

        match investigation.Studies with
        | Some studies -> 
            if API.Study.existsByIdentifier identifier studies then
                API.Study.updateByIdentifier updateOption study studies
                |> API.Investigation.setStudies investigation
            else 
                if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation" identifier
                if containsFlag "AddIfMissing" studyArgs then
                    if verbosity >= 1 then printfn "Registering study as AddIfMissing Flag was set" 
                    API.Study.add studies study
                    |> API.Investigation.setStudies investigation
                else 
                    if verbosity >= 2 then printfn "AddIfMissing argument can be used to register study with the update command if it is missing" 
                    investigation
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"  
            if containsFlag "AddIfMissing" studyArgs then
                if verbosity >= 1 then printfn "Registering study as AddIfMissing Flag was set" 
                [study]
                |> API.Investigation.setStudies investigation
            else 
                if verbosity >= 2 then printfn "AddIfMissing argument can be used to register study with the update command if it is missing" 
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

        match investigation.Studies with
        | Some studies -> 
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
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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
                    (IsaModelConfiguration.getStudiesFileName identifier arcConfiguration)
                    []
            Study.fromParts studyInfo [] [] [] [] [] [] 

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath

        match investigation.Studies with
        | Some studies -> 
            match API.Study.tryGetByIdentifier identifier studies with
            | Some study -> 
                if verbosity >= 1 then printfn "Study with the identifier %s already exists in the investigation file" identifier
                studies
            | None -> 
                API.Study.add studies study                
        | None -> 
            [study]
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

        let identifier = getFieldValueByName "Identifier" studyArgs

        let studyFilePath = IsaModelConfiguration.getStudiesFileName identifier arcConfiguration

        try System.IO.File.Delete studyFilePath with
        | err -> 
            if verbosity >= 1 then printfn "Error: Couldn't delete study file: \n %s" err.Message

    /// Unregisters an existing study from the arc's investigation file.
    let unregister (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Study Unregister"

        let identifier = getFieldValueByName "Identifier" studyArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = Investigation.fromFile investigationFilePath

        match investigation.Studies with
        | Some studies -> 
            match API.Study.tryGetByIdentifier identifier studies with
            | Some study -> 
                API.Study.removeByIdentifier identifier studies 
                |> API.Investigation.setStudies investigation            
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not in the investigation file" identifier

                investigation
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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
        
        match investigation.Studies with
        | Some studies -> 
            match API.Study.tryGetByIdentifier identifier studies with
            | Some study ->
                study
                |> Prompt.serializeXSLXWriterOutput Study.StudyInfo.WriteStudyInfo
                |> printfn "%s"
            | None -> 
                if verbosity >= 1 then printfn "Study with the identifier %s does not in the investigation file" identifier
                ()
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"  
            

    /// Lists all study identifiers registered in this arc's investigation file
    let list (arcConfiguration:ArcConfiguration) =
        
        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Study List"

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get  
        printfn "InvestigationFile: %s"  investigationFilePath

        let investigation = Investigation.fromFile investigationFilePath
        
        match investigation.Studies with
        | Some studies -> 
            studies
            |> List.iter (fun s ->
            
                printfn "Study: %s" (Option.defaultValue "" s.Identifier)
            )
        | None -> 
            if verbosity >= 1 then printfn "The investigation does not contain any studies"  
           

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

            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Contacts with
                    | Some persons -> 
                        if API.Person.existsByFullName firstName midInitials lastName persons then
                            API.Person.updateByFullName updateOption person persons
                            |> API.Study.setContacts study

                        else
                            if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the study with the identifier %s" firstName midInitials lastName studyIdentifier
                            if containsFlag "AddIfMissing" personArgs then
                                if verbosity >= 1 then printfn "Registering person as AddIfMissing Flag was set" 
                                API.Person.add persons person
                                |> API.Study.setContacts study
                            else 
                                if verbosity >= 2 then printfn "AddIfMissing argument can be used to register person with the update command if it is missing" 
                                study
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any persons" studyIdentifier
                        if containsFlag "AddIfMissing" personArgs then
                            if verbosity >= 1 then printfn "Registering person as AddIfMissing Flag was set" 
                            [person]
                            |> API.Study.setContacts study
                        else 
                            if verbosity >= 2 then printfn "AddIfMissing argument can be used to register person with the update command if it is missing" 
                            study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Contacts with
                    | Some persons -> 
                        match API.Person.tryGetByFullName firstName midInitials lastName persons with
                        | Some person -> 
                            ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                                (List.singleton >> Contacts.writePersons None) 
                                (Contacts.readPersons None 1 >> fun (_,_,_,items) -> items.Head) 
                                person
                            |> fun p -> API.Person.updateBy ((=) person) API.Update.UpdateAll p persons
                            |> API.Study.setContacts study
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                            |> API.Investigation.setStudies investigation
                        | None ->
                            if verbosity >= 1 then printfn "Person with the name %s %s %s does not exist in the study with the identifier %s" firstName midInitials lastName studyIdentifier
                            investigation
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any persons" studyIdentifier
                        investigation
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
                investigation
            |> Investigation.toFile investigationFilePath

        /// Registers a person in the arc investigation study with the given person metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (personArgs : Map<string,Argument>) =

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
            
            let studyIdentifier = getFieldValueByName "StudyIdentifier" personArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Contacts with
                    | Some persons -> 
                        if API.Person.existsByFullName firstName midInitials lastName persons then               
                            if verbosity >= 1 then printfn "Person with the name %s %s %s already exists in the investigation file" firstName midInitials lastName
                            persons
                        else
                            API.Person.add persons person                           
                    | None -> 
                        [person]
                    |> API.Study.setContacts study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                | None ->
                    printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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
            
            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Contacts with
                    | Some persons -> 
                        if API.Person.existsByFullName firstName midInitials lastName persons then
                            API.Person.removeByFullName firstName midInitials lastName persons
                            |> API.Study.setContacts study
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                            |> API.Investigation.setStudies investigation
                        else
                            if verbosity >= 1 then printfn "Person with the name %s %s %s  does not exist in the study with the identifier %s" firstName midInitials lastName studyIdentifier
                            investigation
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any persons" studyIdentifier
                        investigation
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Contacts with
                    | Some persons -> 
                        match API.Person.tryGetByFullName firstName midInitials lastName persons with
                        | Some person ->
                            [person]
                            |> Prompt.serializeXSLXWriterOutput (Contacts.writePersons None)
                            |> printfn "%s"
                        | None -> printfn "Person with the name %s %s %s  does not exist in the study with the identifier %s" firstName midInitials lastName studyIdentifier
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any persons" studyIdentifier
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"


        /// Lists the full names of all persons included in the investigation
        let list (arcConfiguration:ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Person List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Studies with
            | Some studies -> 
                studies
                |> Seq.iter (fun study ->
                    match study.Contacts with
                    | Some persons -> 
                   
                        printfn "Study: %s" (Option.defaultValue "" study.Identifier)
                        persons 
                        |> Seq.iter (fun person -> 
                            let firstName = Option.defaultValue "" person.FirstName
                            let midInitials = Option.defaultValue "" person.MidInitials
                            let lastName = Option.defaultValue "" person.LastName
                            if midInitials = "" then
                                printfn "--Person: %s %s" firstName lastName
                            else
                                printfn "--Person: %s %s %s" firstName midInitials lastName)
                    | None -> ()
                )
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  

    /// Functions for altering investigation Publications
    module Publications =

        /// Updates an existing publication in the arc investigation study with the given publication metadata contained in cliArgs.
        let update (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication update"

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

            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Publications with
                    | Some publications -> 
                        if API.Publication.existsByDoi doi publications then
                            API.Publication.updateByDOI updateOption publication publications
                            |> API.Study.setPublications study
                        else
                            if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the study with the identifier %s" doi studyIdentifier
                            if containsFlag "AddIfMissing" publicationArgs then
                                if verbosity >= 1 then printfn "Registering publication as AddIfMissing Flag was set" 
                                API.Publication.add publications publication
                                |> API.Study.setPublications study
                            else 
                                if verbosity >= 2 then printfn "AddIfMissing argument can be used to register publication with the update command if it is missing" 
                                study
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any publications" studyIdentifier
                        if containsFlag "AddIfMissing" publicationArgs then
                            if verbosity >= 1 then printfn "Registering publication as AddIfMissing Flag was set" 
                            [publication]
                            |> API.Study.setPublications study
                        else 
                            if verbosity >= 2 then printfn "AddIfMissing argument can be used to register publication with the update command if it is missing" 
                            study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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
            
            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Publications with
                    | Some publications -> 
                        // TODO : Remove the "Some" when the
                        match API.Publication.tryGetByDoi doi publications with
                        | Some publication ->                    
                            ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                                (List.singleton >> Publications.writePublications None) 
                                (Publications.readPublications None 1 >> fun (_,_,_,items) -> items.Head) 
                                publication
                            |> fun p -> API.Publication.updateBy ((=) publication) API.Update.UpdateAll p publications
                            |> API.Study.setPublications study
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                            |> API.Investigation.setStudies investigation

                        | None ->
                            if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the study with the identifier %s" doi studyIdentifier
                            investigation
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any publications" studyIdentifier
                        investigation
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
                investigation
            |> Investigation.toFile investigationFilePath


        /// Registers a publication in the arc investigation study with the given publication metadata contained in personArgs.
        let register (arcConfiguration:ArcConfiguration) (publicationArgs : Map<string,Argument>) =

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
            
            let studyIdentifier = getFieldValueByName "StudyIdentifier" publicationArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Publications with
                    | Some publications -> 
                        if API.Publication.existsByDoi doi publications then           
                            if verbosity >= 1 then printfn "Publication with the doi %s already exists in the study with the identifier %s" doi studyIdentifier
                            publications
                        else
                            API.Publication.add publications publication                           
                    | None ->
                        [publication]
                    |> API.Study.setPublications study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                | None ->
                    printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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
            
            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Publications with
                    | Some publications -> 
                        if API.Publication.existsByDoi doi publications then           
                            API.Publication.removeByDoi doi publications
                            |> API.Study.setPublications study
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                            |> API.Investigation.setStudies investigation
                        else
                            if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the study with the identifier %s" doi studyIdentifier
                            investigation               
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any publications" studyIdentifier
                        investigation
                | None ->
                    printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Publications with
                    | Some publications -> 
                        match API.Publication.tryGetByDoi doi publications with
                        | Some publication ->
                            [publication]
                            |> Prompt.serializeXSLXWriterOutput (Publications.writePublications None)
                            |> printfn "%s"
                        | None -> 
                            if verbosity >= 1 then printfn "Publication with the doi %s does not exist in the study with the identifier %s" doi studyIdentifier
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any publications" studyIdentifier
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  


        /// Lists the dois of all publications included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Publication List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Studies with
            | Some studies -> 
                studies 
                |> Seq.iter (fun study ->
                    match study.Publications with
                    | Some publications -> 
                        printfn "Study: %s" (Option.defaultValue "" study.Identifier)
                        publications
                        |> Seq.iter (fun publication -> printfn "--Publication DOI: %s" (Option.defaultValue "" publication.DOI))
                    | None -> ()
                )
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  


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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.StudyDesignDescriptors with
                    | Some designs -> 
                        if API.OntologyAnnotation.existsByName design.Name.Value designs then
                            API.OntologyAnnotation.updateByName updateOption design designs
                            |> API.Study.setDescriptors study

                        else
                            if verbosity >= 1 then printfn "Design with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                            if containsFlag "AddIfMissing" designArgs then
                                if verbosity >= 1 then printfn "Registering design as AddIfMissing Flag was set" 
                                API.OntologyAnnotation.add designs design
                                |> API.Study.setDescriptors study
                            else 
                                if verbosity >= 2 then printfn "AddIfMissing argument can be used to register design with the update command if it is missing" 
                                study
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any design descriptors" studyIdentifier
                        if containsFlag "AddIfMissing" designArgs then
                            if verbosity >= 1 then printfn "Registering design as AddIfMissing Flag was set" 
                            [design]
                            |> API.Study.setDescriptors study
                        else 
                            if verbosity >= 2 then printfn "AddIfMissing argument can be used to register design with the update command if it is missing" 
                            study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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
            
            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.StudyDesignDescriptors with
                    | Some designs -> 
                        match API.OntologyAnnotation.tryGetByName (AnnotationValue.fromString name) designs with
                        | Some design ->                    
                            ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                                (List.singleton >> DesignDescriptors.writeDesigns None) 
                                (DesignDescriptors.readDesigns None 1 >> fun (_,_,_,items) -> items.Head) 
                                design
                            |> fun d -> API.OntologyAnnotation.updateBy ((=) design) API.Update.UpdateAll d designs
                            |> API.Study.setDescriptors study
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                            |> API.Investigation.setStudies investigation

                        | None ->
                            if verbosity >= 1 then printfn "Design with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                            investigation
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any design descriptors" studyIdentifier
                        investigation
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.StudyDesignDescriptors with
                    | Some designs -> 
                        if API.OntologyAnnotation.existsByName (AnnotationValue.fromString name) designs then           
                            if verbosity >= 1 then printfn "Design with the name %s already exists in the study with the identifier %s" name studyIdentifier
                            designs
                        else
                            API.OntologyAnnotation.add designs design                           
                    | None -> 
                        [design]
                    |> API.Study.setDescriptors study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                | None ->
                    printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.StudyDesignDescriptors with
                    | Some designs -> 
                        if API.OntologyAnnotation.existsByName (AnnotationValue.fromString name) designs then           
                            API.OntologyAnnotation.removeByName (AnnotationValue.fromString name) designs
                            |> API.Study.setDescriptors study
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                            |> API.Investigation.setStudies investigation
                        else
                            if verbosity >= 1 then printfn "Design with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                            investigation    
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any design descriptors" studyIdentifier
                        investigation
                | None ->
                    printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.StudyDesignDescriptors with
                    | Some designs -> 
                        match API.OntologyAnnotation.tryGetByName (AnnotationValue.fromString name) designs with
                        | Some design ->
                            [design]
                            |> Prompt.serializeXSLXWriterOutput (DesignDescriptors.writeDesigns None)
                            |> printfn "%s"
                        | None -> 
                            if verbosity >= 1 then printfn "Design with the doi %s does not exist in the study with the identifier %s" name studyIdentifier                    
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any design descriptors" studyIdentifier
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
        /// Lists the designs included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Design List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Studies with
            | Some studies -> 
                studies
                |> Seq.iter (fun study ->
                    match study.StudyDesignDescriptors with
                    | Some designs -> 
                        printfn "Study: %s" (Option.defaultValue "" study.Identifier)
                        designs
                        |> Seq.iter (fun design -> printfn "--Design Type: %s" (design.Name |> Option.map AnnotationValue.toString |> Option.defaultValue "" ))
                    | None -> ()
                )
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  

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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Factors with
                    | Some factors -> 
                        if API.Factor.existsByName name factors then
                            API.Factor.updateByName updateOption factor factors
                            |> API.Study.setFactors study
                        else
                            if verbosity >= 1 then printfn "Factor with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                            if containsFlag "AddIfMissing" factorArgs then
                                if verbosity >= 1 then printfn "Registering factor as AddIfMissing Flag was set" 
                                API.Factor.add factors factor
                                |> API.Study.setFactors study
                            else 
                                if verbosity >= 2 then printfn "AddIfMissing argument can be used to register factor with the update command if it is missing" 
                                study
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any factors" studyIdentifier
                        if containsFlag "AddIfMissing" factorArgs then
                            if verbosity >= 1 then printfn "Registering factor as AddIfMissing Flag was set" 
                            [factor]
                            |> API.Study.setFactors study
                        else 
                            if verbosity >= 2 then printfn "AddIfMissing argument can be used to register factor with the update command if it is missing" 
                            study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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
            
            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Factors with
                    | Some factors -> 
                        match API.Factor.tryGetByName name factors with
                        | Some factor ->                    
                            ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                                (List.singleton >> Factors.writeFactors None) 
                                (Factors.readFactors None 1 >> fun (_,_,_,items) -> items.Head) 
                                factor
                            |> fun f -> API.Factor.updateBy ((=) factor) API.Update.UpdateAll f factors
                            |> API.Study.setFactors study
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                            |> API.Investigation.setStudies investigation

                        | None ->
                            if verbosity >= 1 then printfn "Factor with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                            investigation
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any factors" studyIdentifier
                        investigation
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Factors with
                    | Some factors -> 
                        if API.Factor.existsByName name factors then           
                            if verbosity >= 1 then printfn "Factor with the name %s already exists in the study with the identifier %s" name studyIdentifier
                            factors
                        else
                            API.Factor.add factors factor
                    | None -> [factor]
                    |> API.Study.setFactors study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                | None ->
                    printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Factors with
                    | Some factors -> 
                        if API.Factor.existsByName name factors then           
                            API.Factor.removeByName name factors
                            |> API.Study.setFactors study
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                            |> API.Investigation.setStudies investigation
                        else
                            if verbosity >= 1 then printfn "Factor with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                            investigation         
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any factors" studyIdentifier
                        investigation
                | None ->
                    printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Factors with
                    | Some factors -> 
                        match API.Factor.tryGetByName name factors with
                        | Some factor ->
                            [factor]
                            |> Prompt.serializeXSLXWriterOutput (Factors.writeFactors None)
                            |> printfn "%s"
                        | None -> 
                            if verbosity >= 1 then printfn "Factor with the doi %s does not exist in the study with the identifier %s" name studyIdentifier                    
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any factors" studyIdentifier
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  

        /// Lists the factors included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 
            
            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Factor List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Studies with
            | Some studies -> 
                studies
                |> Seq.iter (fun study ->
                    match study.Factors with
                    | Some factors -> 
                        printfn "Study: %s" (Option.defaultValue "" study.Identifier)
                        factors
                        |> Seq.iter (fun factor -> printfn "--Factor Name: %s" (Option.defaultValue "" factor.Name))
                    | None -> ()
                )
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  

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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Protocols with
                    | Some protocols -> 
                        if API.Protocol.existsByName name protocols then
                            API.Protocol.updateByName updateOption protocol protocols
                            |> API.Study.setProtocols study

                        else
                            if verbosity >= 1 then printfn "Protocol with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                            if containsFlag "AddIfMissing" protocolArgs then
                                if verbosity >= 1 then printfn "Registering protocol as AddIfMissing Flag was set" 
                                API.Protocol.add protocols protocol
                                |> API.Study.setProtocols study
                            else 
                                if verbosity >= 2 then printfn "AddIfMissing argument can be used to register protocol with the update command if it is missing" 
                                study
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any protocols" studyIdentifier
                        if containsFlag "AddIfMissing" protocolArgs then
                            if verbosity >= 1 then printfn "Registering protocol as AddIfMissing Flag was set" 
                            [protocol]
                            |> API.Study.setProtocols study
                        else 
                            if verbosity >= 2 then printfn "AddIfMissing argument can be used to register protocol with the update command if it is missing" 
                            study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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
            
            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Protocols with
                    | Some protocols -> 
                        match API.Protocol.tryGetByName name protocols with
                        | Some protocol ->                    
                            ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                                (List.singleton >> Protocols.writeProtocols None) 
                                (Protocols.readProtocols None 1 >> fun (_,_,_,items) -> items.Head) 
                                protocol
                            |> fun f -> API.Protocol.updateBy ((=) protocol) API.Update.UpdateAll f protocols
                            |> API.Study.setProtocols study
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                            |> API.Investigation.setStudies investigation
                        | None ->
                            if verbosity >= 1 then printfn "Protocol with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                            investigation
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any protocols" studyIdentifier
                        investigation
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Protocols with
                    | Some protocols -> 
                        if API.Protocol.existsByName name protocols then           
                            if verbosity >= 1 then printfn "Protocol with the name %s already exists in the study with the identifier %s" name studyIdentifier
                            protocols
                        else
                            API.Protocol.add protocols protocol                          
                    | None -> [protocol]
                    |> API.Study.setProtocols study
                    |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                    |> API.Investigation.setStudies investigation
                | None ->
                    printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Protocols with
                    | Some protocols -> 
                        if API.Protocol.existsByName name protocols then           
                            API.Protocol.removeByName name protocols
                            |> API.Study.setProtocols study
                            |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                            |> API.Investigation.setStudies investigation
                        else
                            if verbosity >= 1 then printfn "Protocol with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                            investigation             
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any protocols" studyIdentifier
                        investigation
                | None ->
                    printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
                investigation
            |> Investigation.toFile investigationFilePath

        let load (arcConfiguration:ArcConfiguration) (protocolArgs : Map<string,Argument>) =

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Protocol Load"

            let editor = GeneralConfiguration.getEditor arcConfiguration
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

            let path = getFieldValueByName "InputPath" protocolArgs

            let protocol =
                if containsFlag "IsProcessFile" protocolArgs then
                    let isaProcess = Json.Process.fromFile path
                    isaProcess.ExecutesProtocol
                else
                    Json.Protocol.fromFile path |> Some
                |> Option.map (fun p -> 
                    if p.Name.IsNone then
                        if verbosity >= 1 then printfn "Given protocol does not contain name, please add it in the editor" 
                        ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                            (List.singleton >> Protocols.writeProtocols None) 
                            (Protocols.readProtocols None 1 >> fun (_,_,_,items) -> items.Head) 
                            p
                    else p               
                )

            let studyIdentifier = getFieldValueByName "StudyIdentifier" protocolArgs

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath
           
            match investigation.Studies with
            | Some studies -> 
                match protocol with 
                | Some protocol ->                
                    match API.Study.tryGetByIdentifier studyIdentifier studies with
                    | Some study -> 
                        let name = protocol.Name.Value
                        match study.Protocols with
                        | Some protocols ->
                            if API.Protocol.existsByName name protocols then  
                                if verbosity >= 1 then 
                                    printfn "Protocol with the name %s already exists in the study with the identifier %s" name studyIdentifier
                                if containsFlag "UpdateExisting" protocolArgs then
                                    if verbosity >= 1 then printfn "Updating protocol as \"UpdateExisting\" flag was given" 
                                    API.Protocol.updateByName API.Update.UpdateAll protocol protocols
                                else
                                    if verbosity >= 1 then printfn "Not updating protocol as \"UpdateExisting\" flag was not given" 
                                    protocols
                            else                  
                                if verbosity >= 2 then printfn "Protocol with the name %s does not exist in the study with the identifier %s" name studyIdentifier
                                API.Protocol.add protocols protocol
                        | None -> [protocol]
                    
                        |> API.Study.setProtocols study
                        |> fun s -> API.Study.updateByIdentifier API.Update.UpdateAll s studies
                        |> API.Investigation.setStudies investigation
                    | None ->
                        printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
                        investigation              
                | None ->
                    if verbosity >= 1 then printfn "The process file did not contain a protocol" 
                    investigation
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
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

            match investigation.Studies with
            | Some studies -> 
                match API.Study.tryGetByIdentifier studyIdentifier studies with
                | Some study -> 
                    match study.Protocols with
                    | Some protocols -> 
                        match API.Protocol.tryGetByName name protocols with
                        | Some protocol ->
                            [protocol]
                            |> Prompt.serializeXSLXWriterOutput (Protocols.writeProtocols None)
                            |> printfn "%s"
                        | None -> 
                            if verbosity >= 1 then printfn "Protocol with the doi %s does not exist in the study with the identifier %s" name studyIdentifier                    
                    | None -> 
                        if verbosity >= 1 then printfn "The study with the identifier %s does not contain any protocols" studyIdentifier
                | None -> 
                    if verbosity >= 1 then printfn "Study with the identifier %s does not exist in the investigation file" studyIdentifier
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  
                

        /// Lists the protocols included in the investigation study
        let list (arcConfiguration:ArcConfiguration) = 

            let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
            
            if verbosity >= 1 then printfn "Start Protocol List"

            let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
            
            let investigation = Investigation.fromFile investigationFilePath

            match investigation.Studies with
            | Some studies -> 
                studies
                |> Seq.iter (fun study ->
                    match study.Protocols with
                    | Some protocols -> 
                        printfn "Study: %s" (Option.defaultValue "" study.Identifier)
                        protocols
                        |> Seq.iter (fun factor -> printfn "--Protocol Name: %s" (Option.defaultValue "" factor.Name))
                    | None -> ()
                )
            | None -> 
                if verbosity >= 1 then printfn "The investigation does not contain any studies"  