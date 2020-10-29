
//#load "C:\Users\HLWei\source\diverse\OpenXML\.paket\load\DocumentFormat.OpenXml.fsx"
#I @"../../bin\ArcCommander\netcoreapp3.1"

#r "DocumentFormat.OpenXml.dll"
#r"FSharpSpreadsheetML.dll"
#r "ISA-XLSX.dll"

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet
open FSharpSpreadsheetML

open ISA
open DataModel
open InvestigationFile
open ISA_XLSX.IO

let source = __SOURCE_DIRECTORY__

//let investPath = source + @"C:\Users\HLWei\source\diverse\XLSXUnzip\i_investigation - Copy.xlsx"

//let path = @"C:\Users\HLWei\source\diverse\XLSXUnzip\test.xlsx"

//InvestigationItem(identifier = "lul")
//|> ISA_Investigation.createEmpty (path)


//let doc = Spreadsheet.openSpreadsheet path true




//ISA_Investigation.addStudy (StudyItem(identifier = "Study1",submissionDate = "1.2.3")) doc

//ISA_Investigation.tryAddItemToStudy (Factor(name = "Factor1",factorType = "dwadw")) "Study1" doc

//ISA_Investigation.tryAddItemToStudy (Factor(name = "Factor2",factorType = "ebeb")) "Study1" doc

//ISA_Investigation.tryAddItemToStudy (Assay(FileName ="2")) "Study1" doc

//ISA_Investigation.tryRemoveItemFromStudy (Factor(name = "Factor1")) "Study1" doc

//ISA_Investigation.tryUpdateItemInStudy (Factor(name = "Factor2",factorType = "degebab",typeTermAccessionNumber = "5")) "Study1" doc

//ISA_Investigation.tryAddItemToStudy (Person(firstName = "Max",lastName = "Mus")) "Study1" doc
//ISA_Investigation.tryAddItemToStudy (Person(firstName = "Tim",lastName = "Taler")) "Study1" doc
//ISA_Investigation.tryUpdateItemInStudy (Person(firstName = "Max",lastName = "Mus",phone = "12345")) "Study1" doc


//doc.Close()

//open ISA_Investigation

//let item = (Factor(name = "Factor1")) :> ISAItem 
//let study = "Study1"


//let workbookPart = doc |> Spreadsheet.getWorkbookPart

//let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart


//let studyScope = tryGetStudyScope workbookPart study sheet
           
//let itemScope = 
//    studyScope
//    |> Option.map (fun studyScope -> tryGetItemScope workbookPart study studyScope item sheet) 
//    |> Option.flatten
//    |> Option.get


//let colI = tryFindColumnInItemScope workbookPart "Study" itemScope item sheet |> Option.get

//[itemScope.From .. itemScope.To]
//|> List.rev
//|> List.fold (fun s rowI -> SheetTransformation.DirectSheets.removeValueAt colI rowI s) sheet         
//|> removeScopeIfEmpty workbookPart itemScope

//[1]
//|> Seq.skip 2

