namespace ArcCommander

open ISA.DataModel.InvestigationFile
open ArgumentQuery
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

module ArcCommander =
    


    module Assay =        
        
        let add workingDir (parameters : Map<string,string>) =

            let assay = itemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]
           
            let name = assay.FileName

            let dir = System.IO.Directory.CreateDirectory (workingDir + @"\assays\" + name)

            System.IO.File.Create (dir.FullName + "\dataset")   |> ignore
            System.IO.File.Create (dir.FullName + "\protocols") |> ignore
            System.IO.File.Create (dir.FullName + "\isa.tab")   |> ignore

            let investigationFilePath = IO.findInvestigationFile workingDir
            
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            ISA_XLSX.IO.ISA_Investigation.tryAddItemToStudy assay studyIdentifier doc
            doc.Save()
            doc.Close()

        let update workingDir (parameters : Map<string,string>) =

            let assay = itemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]

            let investigationFilePath = IO.findInvestigationFile workingDir
            
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            ISA_XLSX.IO.ISA_Investigation.tryUpdateItemInStudy assay studyIdentifier doc
            doc.Save()
            doc.Close()

        let register workingDir (parameters : Map<string,string>) =

            let assay = itemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]
            
            let investigationFilePath = IO.findInvestigationFile workingDir          

            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            ISA_XLSX.IO.ISA_Investigation.tryAddItemToStudy assay studyIdentifier doc
            doc.Save()
            doc.Close()

        let create workingDir (parameters : Map<string,string>) =

            let assay = itemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]
            
            let name = assay.FileName
            
            let dir = System.IO.Directory.CreateDirectory (workingDir + @"\assays\" + name)

            System.IO.File.Create (dir.FullName + "\dataset")   |> ignore
            System.IO.File.Create (dir.FullName + "\protocols") |> ignore
            System.IO.File.Create (dir.FullName + "\isa.tab")   |> ignore

        let remove workingDir (parameters : Map<string,string>) =

            let assay = itemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]
            
            let name = assay.FileName
            
            IO.purgeAndDeleteDirectory (workingDir + @"\assays\" + name)

            let investigationFilePath = IO.findInvestigationFile workingDir
            
            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            ISA_XLSX.IO.ISA_Investigation.tryRemoveItemFromStudy assay studyIdentifier doc
            doc.Save()
            doc.Close()
            
        let move workingDir (parameters : Map<string,string>) =

            let assay = itemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]
            let targetStudy = parameters.["TargetStudyIdentifier"]

            let investigationFilePath = IO.findInvestigationFile workingDir
            
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

        let edit editorPath workingDir (parameters : Map<string,string>) =

            printf "Start assay edit"

            let assay = itemOfParameters (Assay(fileName = parameters.["AssayIdentifier"])) parameters
            let studyIdentifier = parameters.["StudyIdentifier"]
            
            let investigationFilePath = IO.findInvestigationFile workingDir
            printfn "InvestigationFile: %s"  investigationFilePath

            let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
            match ISA_XLSX.IO.ISA_Investigation.tryGetItemInStudy assay studyIdentifier doc with
            | Some assay ->
                ArgumentQuery.createItemQuery editorPath workingDir assay
                |> fun assay -> ISA_XLSX.IO.ISA_Investigation.tryUpdateItemInStudy assay studyIdentifier doc
                |> ignore
            | None -> 
                printfn "Assay %s does not exist" assay.FileName
                ()
            doc.Save()
            doc.Close()

        let list workingDir (parameters : Map<string,string>) =

            printfn "Start assay list"
            
            let investigationFilePath = IO.findInvestigationFile workingDir
            printfn "InvestigationFile: %s"  investigationFilePath

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
        let init workingDir (parameters : Map<string,string>) =

            let investigation = itemOfParameters (InvestigationItem()) parameters

            let dir = System.IO.Directory.CreateDirectory workingDir
            dir.CreateSubdirectory "assays"     |> ignore
            dir.CreateSubdirectory "codecaps"   |> ignore
            dir.CreateSubdirectory "externals"  |> ignore
            dir.CreateSubdirectory "runs"       |> ignore
            dir.CreateSubdirectory ".arc"       |> ignore

            let investigationFilePath = dir.FullName + "/" + investigation.Identifier + ".xlsx"

            investigation
            |> ISA_XLSX.IO.ISA_Investigation.createEmpty investigationFilePath 

        // Returns true if called anywhere in an arc 
        let isArc () =
           NotImplementedException() |> raise

