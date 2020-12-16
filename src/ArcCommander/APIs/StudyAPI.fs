namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing
open IsaXLSX.InvestigationFile

/// ArcCommander Study API functions that get executed by the study focused subcommand verbs
module StudyAPI =

    /// [Not Implemented] Initializes a new empty study file in the arc.
    let init (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = raise (NotImplementedException())
    
    /// [Not Implemented] Updates an existing study file in the arc with the given study metadata contained in cliArgs.
    let update (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = raise (NotImplementedException())
    
    /// [Not Implemented] Opens an existing study file in the arc with the text editor set in globalArgs, additionally setting the given study metadata contained in cliArgs.
    let edit (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = raise (NotImplementedException())
    
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
                (IsaModelConfiguration.tryGetStudiesFilePath arcConfiguration |> Option.get)
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

    /// [Not Implemented] Creates a new study file in the arc and registers it in the arc's investigation file with the given study metadata contained in cliArgs.
    let add (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = raise (NotImplementedException())
    
    /// [Not Implemented] Removes a study file from the arc's investigation file study register
    let remove (arcConfiguration:ArcConfiguration) (studyArgs : Map<string,Argument>) = raise (NotImplementedException())

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
            
                printfn "Study: %s" s.StudyInfo.Identifier
            )
