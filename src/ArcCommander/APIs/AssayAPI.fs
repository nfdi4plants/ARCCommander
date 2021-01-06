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

        AssayConfiguration.getSubFolderPaths name arcConfiguration
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
            API.Study.Assay.updateByFileName assay study
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
            IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration
            |> Option.get

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = IO.fromFile investigationFilePath

        match API.Study.tryGetByIdentifier studyIdentifier investigation with
        | Some study -> 
            match API.Study.Assay.tryGetByFileName assayFileName study with
            | Some assay -> 
                ArgumentProcessing.Prompt.createIsaItemQuery editor workDir 
                    (List.singleton >> IO.writeAssays "Assay") 
                    (IO.readAssays "Assay" 1 >> fun (_,_,_,items) -> items.Head) 
                    assay
                |> fun a -> API.Study.Assay.updateBy ((=) assay) a study
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
            API.Study.Assay.add assay study
            |> fun s -> API.Study.updateByIdentifier s investigation
        | None when studyIdentifier = "" ->
            let info = StudyInfo.create assayIdentifier "" "" "" "" "" []
            let study = Study.create info [] [] [] [assay] [] []                 
            API.Study.add study investigation   
        | None -> 
            let info = StudyInfo.create studyIdentifier "" "" "" "" "" []
            let study = Study.create info [] [] [] [assay] [] []                 
            API.Study.add study investigation   
        |> IO.toFile investigationFilePath
    
    /// Creates a new assay file and associated folder structure in the arc and registers it in the arc's investigation file with the given assay metadata contained in assayArgs.
    let add (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        init arcConfiguration assayArgs
        register arcConfiguration assayArgs

    /// Unregisters an assay file from the arc's investigation file assay register
    let unregister (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs
        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs

        let assayFileName = 
            IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration
            |> Option.get

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = IO.fromFile investigationFilePath

        match API.Study.tryGetByIdentifier studyIdentifier investigation with
        | Some study -> 
            API.Study.Assay.removeByFileName assayFileName study
            |> fun s -> API.Study.updateByIdentifier s investigation
        | None -> 
            investigation
        |> IO.toFile investigationFilePath
    
    /// Deletes assay folder and underlying file structure of given assay
    let delete (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFolder = 
            AssayConfiguration.tryGetFolderPath assayIdentifier arcConfiguration
            |> Option.get

        if System.IO.Directory.Exists(assayFolder) then
            System.IO.Directory.Delete(assayFolder,true)

    /// Remove an assay from the arc by both unregistering it from the investigation file and removing its folder with the underlying file structure
    let remove (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =
        unregister arcConfiguration assayArgs
        delete arcConfiguration assayArgs

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
            match API.Study.Assay.tryGetByFileName assayFileName study with
            | Some assay ->
                let s = API.Study.Assay.removeByFileName assayFileName study
                match API.Study.tryGetByIdentifier targetStudyIdentifer investigation with
                | Some targetStudy ->
                    API.Study.Assay.add assay targetStudy
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

    /// Moves an assay file from one study group to another (provided by assayArgs)
    let get (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let assayIdentifier = getFieldValueByName "AssayIdentifier" assayArgs

        let assayFileName = IsaModelConfiguration.tryGetAssayFileName assayIdentifier arcConfiguration |> Option.get

        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let investigation = IO.fromFile investigationFilePath
        
        match API.Study.tryGetByIdentifier studyIdentifier investigation with
        | Some study -> 
            match API.Study.Assay.tryGetByFileName assayFileName study with
            | Some assay ->
                printfn "%s:%s" Assay.FileNameLabel assay.FileName
                printfn "%s:%s" Assay.MeasurementTypeLabel assay.MeasurementType
                printfn "%s:%s" Assay.MeasurementTypeTermAccessionNumberLabel assay.MeasurementTypeTermAccessionNumber
                printfn "%s:%s" Assay.MeasurementTypeTermSourceREFLabel assay.MeasurementTypeTermSourceREF
                printfn "%s:%s" Assay.TechnologyTypeLabel assay.TechnologyType
                printfn "%s:%s" Assay.TechnologyTypeTermAccessionNumberLabel assay.TechnologyTypeTermAccessionNumber
                printfn "%s:%s" Assay.TechnologyTypeTermSourceREFLabel assay.TechnologyTypeTermSourceREF
                printfn "%s:%s" Assay.TechnologyPlatformLabel assay.TechnologyPlatform
                
            | None -> printfn "assay %s not found" assayIdentifier
        | None -> 
            printfn "study %s not found" studyIdentifier 



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
