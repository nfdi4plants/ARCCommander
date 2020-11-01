namespace ArcCommander

open ISA.DataModel
open System

module ArcCommander =
    
    let rec purgeAndDeleteDirectory (directoryPath:string) =
        System.IO.Directory.GetFiles(directoryPath)
        |> Array.iter System.IO.File.Delete

        System.IO.Directory.GetDirectories(directoryPath)
        |> Array.iter purgeAndDeleteDirectory

        System.IO.Directory.Delete directoryPath

    let findInvestigationFile (dir) =
        System.IO.DirectoryInfo(dir).GetFiles()
        |> Seq.tryFind (fun fi -> 
            fi.Name.StartsWith "i_"
            &&
            fi.Name.EndsWith ".xlsx"
        )
        |> fun p -> 
            match p with
            | Some f ->
                let path = f.FullName
                printfn "found investigation file %s" path
                path
            | None ->  
                failwith "could not find investigation file"

    module Assay = 
        
        
        let add workingDir studyIdentifier (assay : InvestigationFile.Assay) =

           
            let name = assay.FileName

            let dir = System.IO.Directory.CreateDirectory (workingDir + @"\assays\" + name)

            System.IO.File.Create (dir.FullName + "\dataset")   |> ignore
            System.IO.File.Create (dir.FullName + "\protocols") |> ignore
            System.IO.File.Create (dir.FullName + "\isa.tab")   |> ignore

            let investigationFilePath = findInvestigationFile workingDir
            
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            ISA_XLSX.IO.ISA_Investigation.tryAddItemToStudy assay studyIdentifier doc
            doc.Save()
            doc.Close()

        let update workingDir studyIdentifier (assay : InvestigationFile.Assay) =
            
            let investigationFilePath = findInvestigationFile workingDir
            
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            ISA_XLSX.IO.ISA_Investigation.tryUpdateItemInStudy assay studyIdentifier doc
            doc.Save()
            doc.Close()

        let register workingDir studyIdentifier (assay : InvestigationFile.Assay) =
            
            let investigationFilePath = findInvestigationFile workingDir          

            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            ISA_XLSX.IO.ISA_Investigation.tryAddItemToStudy assay studyIdentifier doc
            doc.Save()
            doc.Close()

        let create workingDir studyIdentifier (assay : InvestigationFile.Assay) =
            
            let name = assay.FileName
            
            let dir = System.IO.Directory.CreateDirectory (workingDir + @"\assays\" + name)

            System.IO.File.Create (dir.FullName + "\dataset")   |> ignore
            System.IO.File.Create (dir.FullName + "\protocols") |> ignore
            System.IO.File.Create (dir.FullName + "\isa.tab")   |> ignore

        let remove workingDir studyIdentifier (assay : InvestigationFile.Assay) =
            
            let name = assay.FileName
            
            purgeAndDeleteDirectory (workingDir + @"\assays\" + name)

            let investigationFilePath = findInvestigationFile workingDir
            
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            ISA_XLSX.IO.ISA_Investigation.tryRemoveItemFromStudy assay studyIdentifier doc
            doc.Save()
            doc.Close()
            
        let move workingDir studyIdentifier targetStudy (assay : InvestigationFile.Assay) =

            let investigationFilePath = findInvestigationFile workingDir
            
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

        let edit workingDir editorPath studyIdentifier (assay : InvestigationFile.Assay) =
            
            let investigationFilePath = findInvestigationFile workingDir
            
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            match ISA_XLSX.IO.ISA_Investigation.tryGetItemInStudy assay studyIdentifier doc with
            | Some assay ->
                ArgumentQuery.askForFillout editorPath workingDir assay
                |> fun assay -> ISA_XLSX.IO.ISA_Investigation.tryUpdateItemInStudy assay studyIdentifier doc
                |> ignore
            | None -> 
                printfn "Assay %s does not exist" assay.FileName
                ()
            doc.Save()
            doc.Close()

    module Arc = 
        // Creates Arc specific folder structure 
        let init workingDir (inv : InvestigationFile.InvestigationItem) =

            let dir = System.IO.Directory.CreateDirectory workingDir
            dir.CreateSubdirectory "assays"     |> ignore
            dir.CreateSubdirectory "codecaps"   |> ignore
            dir.CreateSubdirectory "externals"  |> ignore
            dir.CreateSubdirectory "runs"       |> ignore
            dir.CreateSubdirectory ".arc"       |> ignore

            let investigationFilePath = dir.FullName + "/" + inv.Identifier + ".xlsx"

            inv
            |> ISA_XLSX.IO.ISA_Investigation.createEmpty investigationFilePath 

        // Returns true if called anywhere in an arc 
        let isArc () =
           NotImplementedException() |> raise

