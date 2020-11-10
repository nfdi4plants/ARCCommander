namespace ArcCommander

open ISA.DataModel.InvestigationFile
open ParameterProcessing
open System


module IO =

    let rec purgeAndDeleteDirectory (directoryPath:string) =
        System.IO.Directory.GetFiles(directoryPath)
        |> Array.iter System.IO.File.Delete

        System.IO.Directory.GetDirectories(directoryPath)
        |> Array.iter purgeAndDeleteDirectory

        System.IO.Directory.Delete directoryPath

    let findInvestigationFile (dir) =
        System.IO.DirectoryInfo(dir).GetFiles()
        |> Seq.tryFind (fun fi -> 
            fi.Name = "isa_investigation.xlsx"
        )
        |> fun p -> 
            match p with
            | Some f ->
                let path = f.FullName
                printfn "found investigation file %s" path
                path
            | None ->  
                failwith "could not find investigation file"

/// The ArcCommander API functions that get executed by commands
module ArcCommander =
    
    /// ArcCommander Investigation API functions that get executed by the investigation focused subcommand verbs
    module Investigation =

        /// Creates an investigation file in the arc from the given investigation metadata contained in cliArgs that contains no studies or assays.
        let create (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) =
            
            let workDir = globalArgs.["WorkingDir"]
            let investigation = isaItemOfParameters (InvestigationItem()) cliArgs

            let investigationFilePath = workDir + "/" + "isa_investigation.xlsx"
                   
            investigation
            |> ISA_XLSX.IO.ISA_Investigation.createEmpty investigationFilePath 

        /// [Not Implemented] Updates the existing investigation file in the arc with the given investigation metadata contained in cliArgs.
        let update (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())
        
        /// [Not Implemented] Opens the existing investigation file in the arc with the text editor set in globalArgs, additionally setting the given investigation metadata contained in cliArgs.
        let edit (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())
        
        /// [Not Implemented] Deletes the existing investigation file in the arc
        let delete (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())

    /// ArcCommander Study API functions that get executed by the study focused subcommand verbs
    module Study =

        /// [Not Implemented] Initializes a new empty study file in the arc.
        let init (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())
        
        /// [Not Implemented] Updates an existing study file in the arc with the given study metadata contained in cliArgs.
        let update (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())
        
        /// [Not Implemented] Opens an existing study file in the arc with the text editor set in globalArgs, additionally setting the given study metadata contained in cliArgs.
        let edit (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())
        
        /// Registers an existing study in the arc's investigation file with the given study metadata contained in cliArgs.
        let register (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) =

            let study = isaItemOfParameters (StudyItem()) cliArgs

            let investigationFilePath = IO.findInvestigationFile globalArgs.["WorkingDir"]          
            
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            if ISA_XLSX.IO.ISA_Investigation.studyExists study.Identifier doc then
                printfn "Study %s already exists" study.Identifier
            else 
                ISA_XLSX.IO.ISA_Investigation.addStudy study doc |> ignore
            doc.Save()
            doc.Close()

        /// [Not Implemented] Creates a new study file in the arc and registers it in the arc's investigation file with the given study metadata contained in cliArgs.
        let add (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())
        
        /// [Not Implemented] Removes a study file from the arc's investigation file study register
        let remove (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())

        /// Lists all study identifiers registered in this arc's investigation file
        let list (globalArgs:Map<string,string>) =

            printfn "Start study list"
            
            let investigationFilePath = IO.findInvestigationFile globalArgs.["WorkingDir"]
            printfn "InvestigationFile: %s"  investigationFilePath

            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true

            let studies = ISA_XLSX.IO.ISA_Investigation.getStudies doc
            
            if Seq.isEmpty studies then 
                printfn "The Investigation contains no studies"
            else 
                studies
                |> Seq.iter (fun s ->
                
                    printfn "Study: %s" s.Identifier
                )

    /// ArcCommander Assay API functions that get executed by the assay focused subcommand verbs
    module Assay =        

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

    /// ArcCommander API functions that get executed by top level subcommand verbs
    module Arc = 

        /// Initializes the arc specific folder structure
        let init (globalArgs: Map<string,string>) (cliArgs : Map<string,string>) =

            let workDir = globalArgs.["WorkingDir"]
            printfn "init arc in %s" workDir
            
            let dir = System.IO.Directory.CreateDirectory workDir
            dir.CreateSubdirectory "assays"     |> ignore
            dir.CreateSubdirectory "codecaps"   |> ignore
            dir.CreateSubdirectory "externals"  |> ignore
            dir.CreateSubdirectory "runs"       |> ignore
            dir.CreateSubdirectory ".arc"       |> ignore

            Prompt.writeGlobalParams dir.FullName cliArgs            

        /// Returns true if called anywhere in an arc 
        let isArc (globalArgs: Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())

