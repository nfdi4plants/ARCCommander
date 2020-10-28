(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I @"../../src/ArcCommander\bin\Release\netcoreapp3.1"
#r "DocumentFormat.OpenXml.dll"
#r"FSharpSpreadsheetML.dll"

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet
open FSharpSpreadsheetML

/// Create new copy of the textXLSX
let doc = 
    let source = Spreadsheet.openSpreadsheet @"C:\Users\HLWei\source\repos\arcCommander\docs\content\data\FSharpSpreadsheetMLTest.xlsx" false
    source |> Spreadsheet.saveAs @"C:\Users\HLWei\source\repos\arcCommander\docs\content\data\FSharpSpreadsheetMLTestCopy.xlsx"
    source |> Spreadsheet.close
    Spreadsheet.openSpreadsheet @"C:\Users\HLWei\source\repos\arcCommander\docs\content\data\FSharpSpreadsheetMLTestCopy.xlsx" true

let workBookPart = Spreadsheet.getWorkbookPart doc

let sheet = SheetTransformation.firstSheetOfWorkbookPart workBookPart

SheetTransformation.SSTSheets.getCellValueSSTAt workBookPart 1u 1u sheet

SheetTransformation.SSTSheets.getRowValuesSSTAt workBookPart 2u sheet

SheetTransformation.DirectSheets.insertRowAt ["omg";"this";"is";"test"] 2u sheet

SheetTransformation.DirectSheets.appendRow ["last";"row";"in";"doc"] sheet

SheetTransformation.DirectSheets.removeRowAt 3u sheet

doc
|> Spreadsheet.saveChanges
|> Spreadsheet.close