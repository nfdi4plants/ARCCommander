namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISA.DataModel.InvestigationFile

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

        let assay = isaItemOfArguments (Assay(fileName = getFieldValueByName "AssayIdentifier" assayArgs)) assayArgs
        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
        ISA_XLSX.IO.ISA_Investigation.tryUpdateItemInStudy assay studyIdentifier doc
        doc.Save()
        doc.Close()
    
    /// Opens an existing assay file in the arc with the text editor set in globalArgs, additionally setting the given assay metadata contained in assayArgs.
    let edit (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        printf "Start assay edit"
        let editor = GeneralConfiguration.getEditor arcConfiguration
        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        let assay = isaItemOfArguments (Assay(fileName = getFieldValueByName "AssayIdentifier" assayArgs)) assayArgs
        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs
        
        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get

        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
        match ISA_XLSX.IO.ISA_Investigation.tryGetItemInStudy assay studyIdentifier doc with
        | Some assay ->
            Prompt.createItemQuery editor workDir assay
            |> fun assay -> ISA_XLSX.IO.ISA_Investigation.tryUpdateItemInStudy assay studyIdentifier doc
            |> ignore
        | None -> 
            printfn "Assay %s does not exist" assay.FileName
            ()
        doc.Save()
        doc.Close()
    
    /// Registers an existing assay in the arc's investigation file with the given assay metadata contained in assayArgs.
    let register (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let assay = isaItemOfArguments (Assay(fileName = getFieldValueByName "AssayIdentifier" assayArgs)) assayArgs
        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true

        if ISA_XLSX.IO.ISA_Investigation.studyExists studyIdentifier doc |> not then
            ISA_XLSX.IO.ISA_Investigation.addStudy (StudyItem(identifier = studyIdentifier)) doc |> ignore
        ISA_XLSX.IO.ISA_Investigation.tryAddItemToStudy assay studyIdentifier doc

        doc.Save()
        doc.Close()
    
    /// Creates a new assay file and associated folder structure in the arc and registers it in the arc's investigation file with the given assay metadata contained in assayArgs.
    let add (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let assay = isaItemOfArguments (Assay(fileName = getFieldValueByName "AssayIdentifier" assayArgs)) assayArgs
        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs
        let name = assay.FileName

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get

        AssayConfiguration.getFolderPaths name arcConfiguration
        |> Array.iter (System.IO.Directory.CreateDirectory >> ignore)

        IsaModelConfiguration.tryGetAssayFilePath name arcConfiguration
        |> Option.get
        |> System.IO.File.Create
        |> ignore

        AssayConfiguration.getFilePaths name arcConfiguration
        |> Array.iter (System.IO.File.Create >> ignore)

        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
        
        if ISA_XLSX.IO.ISA_Investigation.studyExists studyIdentifier doc |> not then
            ISA_XLSX.IO.ISA_Investigation.addStudy (StudyItem(identifier = studyIdentifier)) doc |> ignore
        ISA_XLSX.IO.ISA_Investigation.tryAddItemToStudy assay studyIdentifier doc

        doc.Save()
        doc.Close()
    
    /// Removes an assay file from the arc's investigation file assay register
    let remove (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let assay = isaItemOfArguments (Assay(fileName = getFieldValueByName "AssayIdentifier" assayArgs)) assayArgs
        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs
        let name = assay.FileName

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
        System.IO.Directory.Delete (workDir + @"\assays\" + name,true)

        
        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
        ISA_XLSX.IO.ISA_Investigation.tryRemoveItemFromStudy assay studyIdentifier doc
        doc.Save()
        doc.Close()
    
    /// Moves an assay file from one study group to another (provided by assayArgs)
    let move (arcConfiguration:ArcConfiguration) (assayArgs : Map<string,Argument>) =

        let assay = isaItemOfArguments (Assay(fileName = getFieldValueByName "AssayIdentifier" assayArgs)) assayArgs
        let studyIdentifier = getFieldValueByName "StudyIdentifier" assayArgs
        let targetStudy = getFieldValueByName "TargetStudyIdentifier" assayArgs

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
        
        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
        match ISA_XLSX.IO.ISA_Investigation.studyExists targetStudy doc, ISA_XLSX.IO.ISA_Investigation.tryGetItemInStudy assay studyIdentifier doc with
        | (true, Some assay) ->
            ISA_XLSX.IO.ISA_Investigation.tryRemoveItemFromStudy assay studyIdentifier doc
            ISA_XLSX.IO.ISA_Investigation.tryAddItemToStudy assay targetStudy doc
            ()
        | false, _ -> 
            printfn "Target Study does not exist"
            ()
        | _ -> 
            ()
        doc.Save()
        doc.Close()

    /// Lists all assay identifiers registered in this investigation
    let list (arcConfiguration:ArcConfiguration) =

        printfn "Start assay list"
        
        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get

        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true

        ISA_XLSX.IO.ISA_Investigation.getStudies doc
        |> Seq.iter (fun s ->
            let assays = ISA_XLSX.IO.ISA_Investigation.getItemsInStudy (Assay()) s.Identifier doc
            if Seq.isEmpty assays |> not then
                printfn "Study: %s" s.Identifier
                assays 
                |> Seq.iter (fun a -> printfn "--Assay: %s" a.FileName)
        )
