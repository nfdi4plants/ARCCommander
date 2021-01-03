namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing

open IsaXLSX.InvestigationFile

/// ArcCommander Assay API functions that get executed by the assay focused subcommand verbs
module AssayAPI =        

    /// Initializes a new empty assay file and associated folder structure in the arc.
    let init (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let name = getFieldValueByName "AssayIdentifier" assayArgs

        AssayConfiguration.getFolderPaths name arcConfiguration
        |> Array.iter (System.IO.Directory.CreateDirectory >> ignore)

        IsaModelConfiguration.tryGetAssayFilePath name arcConfiguration
        |> Option.get
        |> System.IO.File.Create
        |> ignore

        AssayConfiguration.getFilePaths name arcConfiguration
        |> Array.iter (System.IO.File.Create >> ignore)


    /// Updates an existing assay file in the arc with the given assay metadata contained in cliArgs.
    let update (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFileName = 
            IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration
            |> Option.get

        let assay = 
            Assay.create
                (getFieldValueByName  "MeasurementType" assayArgs)
                (getFieldValueByName  "MeasurementTypeTermAccessionNumber" assayArgs)
                (getFieldValueByName  "MeasurementTypeTermSourceREF" assayArgs)
                (getFieldValueByName  "TechnologyType" assayArgs)
                (getFieldValueByName  "TechnologyTypeTermAccessionNumber" assayArgs)
                (getFieldValueByName  "TechnologyTypeTermSourceREF" assayArgs)
                (getFieldValueByName  "TechnologyPlatform" assayArgs)
                assayFileName
                []

        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = IO.fromFile investigationFilePath

        match API.Study.tryGetByIdentifier studyIdentifier investigation with
        | Some study -> 
            API.Assay.updateByFileName assay study
            |> fun s -> API.Study.updateByIdentifier s investigation
        | None -> investigation
        |> IO.toFile investigationFilePath
    
    /// Opens an existing assay file in the arc with the text editor set in globalArgs, additionally setting the given assay metadata contained in assayArgs.
    let edit (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        printf "Start assay edit"
        let editor = GeneralConfiguration.getEditor arcConfiguration
        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs

        let assayFileName = 
            IsaModelConfiguration.tryGetAssayFilePath assayIdentifier arcConfiguration
            |> Option.get

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = IO.fromFile investigationFilePath

        match API.Study.tryGetByIdentifier studyIdentifier investigation with
        | Some study -> 
            match API.Assay.tryGetByFileName assayFileName study with
            | Some assay -> 
                ArgumentProcessing.Prompt.createIsaItemQuery editor workDir IO.writeAssays IO.readAssays assay
                |> fun a -> API.Assay.updateByFileName a study
                |> fun s -> API.Study.updateByIdentifier s investigation
            | None ->
                investigation
        | None -> 
            investigation
        |> IO.toFile investigationFilePath


    /// Registers an existing assay in the arc's investigation file with the given assay metadata contained in assayArgs.
    let register (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        
        let assayFileName = 
            IsaModelConfiguration.tryGetAssayFilePath assayIdentifier arcConfiguration
            |> Option.get
        
        let assay = 
            Assay.create
                (getFieldValueByName  "MeasurementType" assayArgs)
                (getFieldValueByName  "MeasurementTypeTermAccessionNumber" assayArgs)
                (getFieldValueByName  "MeasurementTypeTermSourceREF" assayArgs)
                (getFieldValueByName  "TechnologyType" assayArgs)
                (getFieldValueByName  "TechnologyTypeTermAccessionNumber" assayArgs)
                (getFieldValueByName  "TechnologyTypeTermSourceREF" assayArgs)
                (getFieldValueByName  "TechnologyPlatform" assayArgs)
                assayFileName
                []
        
        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = IO.fromFile investigationFilePath

        match API.Study.tryGetByIdentifier studyIdentifier investigation with
        | Some study -> 
            API.Assay.add assay study
            |> fun s -> API.Study.updateByIdentifier s investigation
        | None -> 
            let info = StudyInfo.create studyIdentifier "" "" "" "" "" []
            let study = Study.create info [] [] [] [assay] [] []                 
            API.Study.add study investigation            
        |> IO.toFile investigationFilePath
    
    /// Creates a new assay file and associated folder structure in the arc and registers it in the arc's investigation file with the given assay metadata contained in assayArgs.
    let add (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        init arcConfiguration assayArgs
        register arcConfiguration assayArgs

    /// Removes an assay file from the arc's investigation file assay register
    let remove (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs

        let assayFileName = 
            IsaModelConfiguration.tryGetAssayFilePath assayIdentifier arcConfiguration
            |> Option.get

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = IO.fromFile investigationFilePath

        match API.Study.tryGetByIdentifier studyIdentifier investigation with
        | Some study -> 
            API.Assay.removeByFileName assayFileName study
            |> fun s -> API.Study.updateByIdentifier s investigation
        | None -> 
            investigation
        |> IO.toFile investigationFilePath
    
    /// Moves an assay file from one study group to another (provided by assayArgs)
    let move (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFileName = IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration |> Option.get

        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs
        let targetStudyIdentifer = getFieldValueByName "TargetStudyIdentifier" assayArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = IO.fromFile investigationFilePath
        
        match API.Study.tryGetByIdentifier studyIdentifier investigation with
        | Some study -> 
            match API.Assay.tryGetByFileName assayFileName study with
            | Some assay ->
                let s = API.Assay.removeByFileName assayFileName study
                match API.Study.tryGetByIdentifier targetStudyIdentifer investigation with
                | Some targetStudy ->
                    API.Assay.add assay targetStudy
                    |> fun ts -> 
                        API.Study.updateByIdentifier s investigation
                        |> API.Study.updateByIdentifier ts
                | None -> 
                    let info = StudyInfo.create studyIdentifier "" "" "" "" "" []
                    let targetStudy = Study.create info [] [] [] [assay] [] []                 
                    API.Study.add targetStudy investigation
                    |> API.Study.updateByIdentifier s
            | None -> investigation
        | None -> 
            investigation
        |> IO.toFile investigationFilePath

    /// Lists all assay identifiers registered in this investigation
    let list (arcConfiguration:ArcConfiguration) =

        printfn "Start assay list"
        
        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get

        let investigation = IO.fromFile investigationFilePath

        investigation.Studies
        |> Seq.iter (fun study ->
            let assays = study.Assays
            if Seq.isEmpty assays |> not then
                printfn "Study: %s" study.Info.Identifier
                assays 
                |> Seq.iter (fun assay -> printfn "--Assay: %s" assay.FileName)
        )
