namespace ArcCommander.APIs

open System
open System.IO

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISADotNet
open ISADotNet.XLSX

/// ArcCommander API functions that get executed by top level subcommand verbs
module ArcAPI = 

    // TODO TO-DO TO DO: make use of args
    /// Initializes the arc specific folder structure
    let init (arcConfiguration : ArcConfiguration) (arcArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Arc Init"

        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        let editor =            tryGetFieldValueByName "EditorPath" arcArgs
        let gitLFSThreshold =   tryGetFieldValueByName "GitLFSByteThreshold" arcArgs
        let repositoryAdress =  tryGetFieldValueByName "RepositoryAdress" arcArgs 


        if verbosity >= 2 then printfn "Create Directory"

        Directory.CreateDirectory workDir |> ignore

        if verbosity >= 2 then printfn "Initiate folder structure"

        ArcConfiguration.getRootFolderPaths arcConfiguration
        |> Array.iter (Directory.CreateDirectory >> ignore)

        if verbosity >= 2 then printfn "Set configuration"

        match editor with
        | Some editorValue -> 
            let path = IniData.getLocalConfigPath workDir
            IniData.setValueInIniPath path "general.editor" editorValue
        | None -> ()

        match gitLFSThreshold with
        | Some gitLFSThresholdValue -> 
            let path = IniData.getLocalConfigPath workDir
            IniData.setValueInIniPath path "general.gitlfsbytethreshold" gitLFSThresholdValue
        | None -> ()

        if verbosity >= 2 then printfn "Init git repository"

        try

            Fake.Tools.Git.Repository.init workDir false true

            match repositoryAdress with
            | None -> ()
            | Some remote ->
                GitAPI.executeGitCommand verbosity workDir ("remote add origin " + remote) |> ignore

        with 
        | _ -> 

            if verbosity >= 1 then printfn "Git could not be set up, try installing git cli and run `arc git init`"


    let update (arcConfiguration : ArcConfiguration) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Arc Update"

        let assayRootFolder = AssayConfiguration.getRootFolderPath arcConfiguration

        let investigationFilePath = IsaModelConfiguration.getInvestigationFilePath arcConfiguration

        let assayNames = 
            DirectoryInfo(assayRootFolder).GetDirectories()
            |> Array.map (fun d -> d.Name)
            
        let investigation =
            try Investigation.fromFile investigationFilePath 
            with
            | :? FileNotFoundException -> 
                Investigation.empty
            | err -> raise err

        let rec updateInvestigationAssays (assayNames : string list) (investigation : Investigation) =
            match assayNames with
            | a :: t ->
                let assayFilePath = IsaModelConfiguration.getAssayFilePath a arcConfiguration
                let assayFileName = IsaModelConfiguration.getAssayFileName a arcConfiguration
                let factors,protocols,persons,assay = AssayFile.AssayFile.fromFile assayFilePath
                let studies = investigation.Studies

                match studies with
                | Some studies ->
                    match studies |> Seq.tryFind (API.Study.getAssays >> Option.defaultValue [] >> API.Assay.existsByFileName assayFileName) with
                    | Some study -> 
                        study
                        |> API.Study.mapAssays (API.Assay.updateByFileName API.Update.UpdateByExistingAppendLists assay)
                        |> API.Study.mapFactors (List.append factors >> List.distinctBy (fun f -> f.Name))
                        |> API.Study.mapProtocols (List.append protocols >> List.distinctBy (fun p -> p.Name))
                        |> API.Study.mapContacts (List.append persons >> List.distinctBy (fun p -> p.FirstName,p.LastName))
                        |> fun s -> API.Study.updateBy ((=) study) API.Update.UpdateAll s studies
                    | None ->
                        Study.fromParts (Study.StudyInfo.create a "" "" "" "" "" []) [] [] factors [assay] protocols persons
                        |> API.Study.add studies
                | None ->                   
                    [Study.fromParts (Study.StudyInfo.create a "" "" "" "" "" []) [] [] factors [assay] protocols persons]
                |> API.Investigation.setStudies investigation
                |> updateInvestigationAssays t
            | [] -> investigation

        updateInvestigationAssays (assayNames |> List.ofArray) investigation
        |> Investigation.toFile investigationFilePath

    /// Returns true if called anywhere in an arc 
    let isArc (arcConfiguration : ArcConfiguration) (arcArgs : Map<string,Argument>) = raise (NotImplementedException())