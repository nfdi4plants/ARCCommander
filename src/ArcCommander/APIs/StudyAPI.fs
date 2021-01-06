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
