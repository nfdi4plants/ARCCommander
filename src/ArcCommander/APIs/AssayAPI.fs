namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISA.DataModel.InvestigationFile

/// ArcCommander Assay API functions that get executed by the assay focused subcommand verbs
module AssayAPI =        

    /// Initializes a new empty assay file and associated folder structure in the arc.
    let init (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) =

        let name = cliArgs.["AssayIdentifier"]
        let dir = System.IO.Directory.CreateDirectory (globalArgs.["WorkingDir"] + @"\assays\" + name)

        System.IO.File.Create (dir.FullName + "\dataset")   |> ignore
        System.IO.File.Create (dir.FullName + "\protocols") |> ignore
        System.IO.File.Create (dir.FullName + "\isa.tab")   |> ignore

    /// Updates an existing assay file in the arc with the given assay metadata contained in cliArgs.
    let update (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) =

        let assay = isaItemOfParameters (Assay(fileName = cliArgs.["AssayIdentifier"])) cliArgs
        let studyIdentifier = cliArgs.["StudyIdentifier"]

        let investigationFilePath = IO.findInvestigationFile globalArgs.["WorkingDir"]
        
        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
        ISA_XLSX.IO.ISA_Investigation.tryUpdateItemInStudy assay studyIdentifier doc
        doc.Save()
        doc.Close()
    
    /// Opens an existing assay file in the arc with the text editor set in globalArgs, additionally setting the given assay metadata contained in cliArgs.
    let edit (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) =

        printf "Start assay edit"

        let assay = isaItemOfParameters (Assay(fileName = cliArgs.["AssayIdentifier"])) cliArgs
        let studyIdentifier = cliArgs.["StudyIdentifier"]
        
        let investigationFilePath = IO.findInvestigationFile globalArgs.["WorkingDir"]

        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
        match ISA_XLSX.IO.ISA_Investigation.tryGetItemInStudy assay studyIdentifier doc with
        | Some assay ->
            Prompt.createItemQuery globalArgs.["EditorPath"] globalArgs.["WorkingDir"] assay
            |> fun assay -> ISA_XLSX.IO.ISA_Investigation.tryUpdateItemInStudy assay studyIdentifier doc
            |> ignore
        | None -> 
            printfn "Assay %s does not exist" assay.FileName
            ()
        doc.Save()
        doc.Close()
    
    /// Registers an existing assay in the arc's investigation file with the given assay metadata contained in cliArgs.
    let register (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) =

        let assay = isaItemOfParameters (Assay(fileName = cliArgs.["AssayIdentifier"])) cliArgs
        let studyIdentifier = cliArgs.["StudyIdentifier"]
        
        let investigationFilePath = IO.findInvestigationFile globalArgs.["WorkingDir"]          
        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true

        if ISA_XLSX.IO.ISA_Investigation.studyExists studyIdentifier doc |> not then
            ISA_XLSX.IO.ISA_Investigation.addStudy (StudyItem(identifier = studyIdentifier)) doc |> ignore
        ISA_XLSX.IO.ISA_Investigation.tryAddItemToStudy assay studyIdentifier doc

        doc.Save()
        doc.Close()
    
    /// Creates a new assay file and associated folder structure in the arc and registers it in the arc's investigation file with the given assay metadata contained in cliArgs.
    let add (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) =

        let assay = isaItemOfParameters (Assay(fileName = cliArgs.["AssayIdentifier"])) cliArgs
        let studyIdentifier = cliArgs.["StudyIdentifier"]
       
        let name = assay.FileName

        let dir = System.IO.Directory.CreateDirectory (globalArgs.["WorkingDir"] + @"\assays\" + name)

        System.IO.File.Create (dir.FullName + "\dataset")   |> ignore
        System.IO.File.Create (dir.FullName + "\protocols") |> ignore
        System.IO.File.Create (dir.FullName + "\isa.tab")   |> ignore

        let investigationFilePath = IO.findInvestigationFile globalArgs.["WorkingDir"]
        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
        
        if ISA_XLSX.IO.ISA_Investigation.studyExists studyIdentifier doc |> not then
            ISA_XLSX.IO.ISA_Investigation.addStudy (StudyItem(identifier = studyIdentifier)) doc |> ignore
        ISA_XLSX.IO.ISA_Investigation.tryAddItemToStudy assay studyIdentifier doc

        doc.Save()
        doc.Close()
    
    /// Removes an assay file from the arc's investigation file assay register
    let remove (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) =

        let assay = isaItemOfParameters (Assay(fileName = cliArgs.["AssayIdentifier"])) cliArgs
        let studyIdentifier = cliArgs.["StudyIdentifier"]
        
        let name = assay.FileName
        
        IO.purgeAndDeleteDirectory (globalArgs.["WorkingDir"] + @"\assays\" + name)

        let investigationFilePath = IO.findInvestigationFile globalArgs.["WorkingDir"]
        
        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
        ISA_XLSX.IO.ISA_Investigation.tryRemoveItemFromStudy assay studyIdentifier doc
        doc.Save()
        doc.Close()
    
    /// Moves an assay file from one study group to another (provided by cliArgs)
    let move (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) =

        let assay = isaItemOfParameters (Assay(fileName = cliArgs.["AssayIdentifier"])) cliArgs
        let studyIdentifier = cliArgs.["StudyIdentifier"]
        let targetStudy = cliArgs.["TargetStudyIdentifier"]

        let investigationFilePath = IO.findInvestigationFile globalArgs.["WorkingDir"]
        
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
    let list (globalArgs:Map<string,string>) =

        printfn "Start assay list"
        
        let investigationFilePath = IO.findInvestigationFile globalArgs.["WorkingDir"]

        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true

        ISA_XLSX.IO.ISA_Investigation.getStudies doc
        |> Seq.iter (fun s ->
            let assays = ISA_XLSX.IO.ISA_Investigation.getItemsInStudy (Assay()) s.Identifier doc
            if Seq.isEmpty assays |> not then
                printfn "Study: %s" s.Identifier
                assays 
                |> Seq.iter (fun a -> printfn "--Assay: %s" a.FileName)
        )
