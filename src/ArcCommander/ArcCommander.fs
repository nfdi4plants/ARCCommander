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

module ArcCommander =
    
    module Investigation =

        let init (globalParams:Map<string,string>) (parameters : Map<string,string>) =

            let workDir = globalParams.["WorkingDir"]
            let investigation = isaItemOfParameters (InvestigationItem()) parameters

            let investigationFilePath = workDir + "/" + "isa_investigation.xlsx"
                   
            investigation
            |> ISA_XLSX.IO.ISA_Investigation.createEmpty investigationFilePath 

    module Study =

        let register  (globalParams:Map<string,string>) (parameters : Map<string,string>) =

            let study = isaItemOfParameters (StudyItem()) parameters

            let investigationFilePath = IO.findInvestigationFile globalParams.["WorkingDir"]          
            
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            if ISA_XLSX.IO.ISA_Investigation.studyExists study.Identifier doc then
                printfn "Study %s already exists" study.Identifier
            else 
                ISA_XLSX.IO.ISA_Investigation.addStudy study doc |> ignore
            doc.Save()
            doc.Close()

        let list (globalParams:Map<string,string>) (parameters : Map<string,string>) =

            printfn "Start study list"
            
            let investigationFilePath = IO.findInvestigationFile globalParams.["WorkingDir"]
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

    module Assay =        
        
        let add (globalParams:Map<string,string>) (parameters : Map<string,string>) =

            let assay = isaItemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]
           
            let name = assay.FileName

            let dir = System.IO.Directory.CreateDirectory (globalParams.["WorkingDir"] + @"\assays\" + name)

            System.IO.File.Create (dir.FullName + "\dataset")   |> ignore
            System.IO.File.Create (dir.FullName + "\protocols") |> ignore
            System.IO.File.Create (dir.FullName + "\isa.tab")   |> ignore

            let investigationFilePath = IO.findInvestigationFile globalParams.["WorkingDir"]
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            
            if ISA_XLSX.IO.ISA_Investigation.studyExists studyIdentifier doc |> not then
                ISA_XLSX.IO.ISA_Investigation.addStudy (StudyItem(identifier = studyIdentifier)) doc |> ignore
            ISA_XLSX.IO.ISA_Investigation.tryAddItemToStudy assay studyIdentifier doc

            doc.Save()
            doc.Close()

        let update (globalParams:Map<string,string>) (parameters : Map<string,string>) =

            let assay = isaItemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]

            let investigationFilePath = IO.findInvestigationFile globalParams.["WorkingDir"]
            
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            ISA_XLSX.IO.ISA_Investigation.tryUpdateItemInStudy assay studyIdentifier doc
            doc.Save()
            doc.Close()

        let register (globalParams:Map<string,string>) (parameters : Map<string,string>) =

            let assay = isaItemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]
            
            let investigationFilePath = IO.findInvestigationFile globalParams.["WorkingDir"]          
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true

            if ISA_XLSX.IO.ISA_Investigation.studyExists studyIdentifier doc |> not then
                ISA_XLSX.IO.ISA_Investigation.addStudy (StudyItem(identifier = studyIdentifier)) doc |> ignore
            ISA_XLSX.IO.ISA_Investigation.tryAddItemToStudy assay studyIdentifier doc

            doc.Save()
            doc.Close()

        let create (globalParams:Map<string,string>) (parameters : Map<string,string>) =

            let assay = isaItemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            
            let name = assay.FileName
            
            let dir = System.IO.Directory.CreateDirectory (globalParams.["WorkingDir"] + @"\assays\" + name)

            System.IO.File.Create (dir.FullName + "\dataset")   |> ignore
            System.IO.File.Create (dir.FullName + "\protocols") |> ignore
            System.IO.File.Create (dir.FullName + "\isa.tab")   |> ignore

        let remove (globalParams:Map<string,string>) (parameters : Map<string,string>) =

            let assay = isaItemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]
            
            let name = assay.FileName
            
            IO.purgeAndDeleteDirectory (globalParams.["WorkingDir"] + @"\assays\" + name)

            let investigationFilePath = IO.findInvestigationFile globalParams.["WorkingDir"]
            
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            ISA_XLSX.IO.ISA_Investigation.tryRemoveItemFromStudy assay studyIdentifier doc
            doc.Save()
            doc.Close()
            
        let move (globalParams:Map<string,string>) (parameters : Map<string,string>) =

            let assay = isaItemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]
            let targetStudy = parameters.["TargetStudyIdentifier"]

            let investigationFilePath = IO.findInvestigationFile globalParams.["WorkingDir"]
            
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

        let edit (globalParams:Map<string,string>) (parameters : Map<string,string>) =

            printf "Start assay edit"

            let assay = isaItemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]
            
            let investigationFilePath = IO.findInvestigationFile globalParams.["WorkingDir"]

            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            match ISA_XLSX.IO.ISA_Investigation.tryGetItemInStudy assay studyIdentifier doc with
            | Some assay ->
                Prompt.createItemQuery globalParams.["EditorPath"] globalParams.["WorkingDir"] assay
                |> fun assay -> ISA_XLSX.IO.ISA_Investigation.tryUpdateItemInStudy assay studyIdentifier doc
                |> ignore
            | None -> 
                printfn "Assay %s does not exist" assay.FileName
                ()
            doc.Save()
            doc.Close()

        let list (globalParams:Map<string,string>) _ =

            printfn "Start assay list"
            
            let investigationFilePath = IO.findInvestigationFile globalParams.["WorkingDir"]

            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true

            ISA_XLSX.IO.ISA_Investigation.getStudies doc
            |> Seq.iter (fun s ->
                let assays = ISA_XLSX.IO.ISA_Investigation.getItemsInStudy (Assay()) s.Identifier doc
                if Seq.isEmpty assays |> not then
                    printfn "Study: %s" s.Identifier
                    assays 
                    |> Seq.iter (fun a -> printfn "---Assay: %s" a.FileName)
            )


    module Arc = 

        // Creates Arc specific folder structure 
        let init (globalParams: Map<string,string>) (parameters : Map<string,string>) =

            let workDir = globalParams.["WorkingDir"]
            printfn "init arc in %s" workDir
            

            let dir = System.IO.Directory.CreateDirectory workDir
            dir.CreateSubdirectory "assays"     |> ignore
            dir.CreateSubdirectory "codecaps"   |> ignore
            dir.CreateSubdirectory "externals"  |> ignore
            dir.CreateSubdirectory "runs"       |> ignore
            dir.CreateSubdirectory ".arc"       |> ignore

            Prompt.writeGlobalParams dir.FullName parameters            

        // Returns true if called anywhere in an arc 
        let isArc () =
           NotImplementedException() |> raise

