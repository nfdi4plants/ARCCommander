(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "C:\Users\HLWei\source\diverse\OpenXML\.paket\load\DocumentFormat.OpenXml.fsx"
#I @"../../src/FSharpSpreadsheetML/bin/Release/netstandard2.1"
#r"FSharpSpreadsheetML.dll"

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet
open FSharpSpreadsheetML




let doc = Spreadsheet.openSpreadsheet "C:\Users\HLWei\source\diverse\XLSXUnzip\schema2.xlsx" true

let workBookPart = Spreadsheet.getWorkbookPart doc

let sheetID = 
    WorkbookPart.getWorkbook workBookPart
    |> Workbook.getSheets
    |> Sheets.getFirstSheet
    |> Sheet.getID

let data =
    WorkbookPart.getWorksheetPartById sheetID workBookPart
    |> WorksheetPart.getWorksheet
    |> Worksheet.getSheetData


let myRow =
    data.FirstChild :?> Row


/// Create Row

let myNewCell1 =
    "hello"
    |> CellValue.create
    |> Cell.create CellValues.String "E1"


let myNewRow = 
    [
    "hello" |> CellValue.create |> Cell.create CellValues.String "A5"
    "my" |> CellValue.create |> Cell.create CellValues.String "B5"
    "dude" |> CellValue.create |> Cell.create CellValues.String "C5"
    "m8" |> CellValue.create |> Cell.create CellValues.String "D5"
    ]
    |> Row.create 5u (Spans.fromBoundaries 1 4)

//(myNewRow.CloneNode(true) :?> Row)

//SheetData.empty.AppendChild (myNewRow.CloneNode(true) :?> Row)


let newSheetData =
    SheetData.empty
    |> SheetData.appendRow (myNewRow.CloneNode(true) :?> Row)

Workbook.addSheet "maDude" newSheetData workBookPart.Workbook


let myNewWorksheet = 
    newSheetData
    |> fun x -> x.CloneNode(true) :?> SheetData
    |> fun x -> Worksheet.addSheetData x Worksheet.empty

let newWorkSheetPart = 
    WorkbookPart.addEmptyWorksheetPart workBookPart
    |> WorksheetPart.setWorksheet myNewWorksheet

let newSheet = 
    Sheet.empty
    |> Sheet.setID (workBookPart.GetIdOfPart newWorkSheetPart)
    |> Sheet.setName "TestSheet"
    |> Sheet.setSheetID 2u

WorkbookPart.getWorkbook workBookPart
|> Workbook.getSheets
|> Sheets.addSheet newSheet


myNewCell1.CellValue.Text <- "lol"

let fifthCell = myRow |> Row.getCells |> Seq.find (Cell.getReference >> (=) "E1")

fifthCell
|> Cell.setReference ("F1")

myRow
|> Row.insertCellBefore myNewCell1 fifthCell

myRow
|> Row.getCells
|> Seq.map (Cell.getReference)
|> Seq.toArray


data.FirstChild :?> Row
|> Row.getCells
|> Seq.head
|> Cell.getValue
|> CellValue.getValue



let sst = workBookPart.SharedStringTablePart.SharedStringTable

SharedStringItem.create "testCase"
|> fun x -> sst.AppendChild x

let item = new SharedStringItem(Text("testCase3123"))

item.Text <- Text("testCase3123")


sst.AppendChild(item)



sst
|> SharedStringTable.getItems
|> Seq.map (SharedStringItem.getText)
|> Seq.toArray

CellValues.SharedString

doc
|> Spreadsheet.saveChanges

doc
|> Spreadsheet.close

let insertRowWithHorizontalOffsetAt (offset:int) (vals: 'T seq)=
    let uiO = uint32 offset
    Spans.fromBoundaries (uiO + 1u) (Seq.length vals |> uint32 |> (+) uiO )

/// 1 based index   
let insertRowAt (vals: 'T seq) =
    insertRowWithHorizontalOffsetAt 0 vals

data
|> SheetTransformation.appendRow [1;3;7]
|> SheetTransformation.insertRowWithHorizontalOffsetAt 3 ["who";"are";"Youre2e2"] 2

1+1