namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet

module H = let notImplemented() = failwith "function not yet implemented"
open H

module SheetData = 

    let empty = new SheetData()

    let insertRow (rowIndex) (values: 'T seq) (sheetData:SheetData) = notImplemented()
    let overWriteRow (rowIndex) (values: 'T seq) (sheetData:SheetData) = notImplemented()
    let appendRow (values: 'T seq) (sheetData:SheetData) = notImplemented()
    let getRow (rowIndex) (sheetData:SheetData) = notImplemented()
    let deleteRow rowIndex (sheetData:SheetData) = notImplemented()

    let insertColumn (columnIndex) (values: 'T seq) (sheetData:SheetData) = notImplemented()
    let overWriteColumn (columnIndex) (values: 'T seq) (sheetData:SheetData) = notImplemented()
    let appendColumn (values: 'T seq) (sheetData:SheetData) = notImplemented()
    let getColumn (columnIndex) (sheetData:SheetData) = notImplemented()
    let deleteColumn (columnIndex) (sheetData:SheetData) = notImplemented()

    //let setCellValue (rowIndex,columnIndex) value (sheetData:SheetData) = notImplemented()
    let setCellValue adress value (sheetData:SheetData) = notImplemented()
    let getCellValue adress (sheetData:SheetData) = notImplemented()
    let deleteCellValue adress (sheetData:SheetData) = notImplemented()

/// Stores data of the sheet 
module Worksheet = 

    let empty = Worksheet(new SheetData())

    let ofSheetData (sheetData:SheetData) = notImplemented()
    let getSheetData (worksheet:Worksheet) = notImplemented()
    let setSheetData (sheetData:SheetData) (worksheet:Worksheet) = notImplemented()

    let insertRow (rowIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    let overWriteRow (rowIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    let appendRow (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    let getRow (rowIndex) (worksheet:Worksheet) = notImplemented()
    let deleteRow rowIndex (worksheet:Worksheet) = notImplemented()

    let insertColumn (columnIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    let overWriteColumn (columnIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    let appendColumn (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    let getColumn (columnIndex) (worksheet:Worksheet) = notImplemented()
    let deleteColumn (columnIndex) (worksheet:Worksheet) = notImplemented()

    //let setCellValue (rowIndex,columnIndex) value (worksheet:Worksheet) = notImplemented()
    let setCellValue adress value (worksheet:Worksheet) = notImplemented()
    let getCellValue adress (worksheet:Worksheet) = notImplemented()
    let deleteCellValue adress (worksheet:Worksheet) = notImplemented()


module WorksheetPart = 

    let setWorksheet (worksheet : Worksheet) (worksheetPart : WorksheetPart) = notImplemented()
    let getWorksheet (worksheetPart : WorksheetPart) = notImplemented()
    //let setID id (worksheetPart : WorksheetPart) = notImplemented()
    //let getID (worksheetPart : WorksheetPart) = notImplemented()


/// Part of the Workbook, stores name and other additional info of the sheet
module Sheet = 
    
    let empty = Sheet()

    let setName name (sheet : Sheet) = notImplemented()
    let getName (sheet : Sheet) = notImplemented()

    /// ID shared with worksheet
    let setID id (sheet : Sheet) = notImplemented()
    /// ID shared with worksheet
    let getID (sheet : Sheet) = notImplemented()

    /// ID used for sorting
    let setSheetID id (sheet : Sheet) = notImplemented()
    /// ID used for sorting
    let getSheetID (sheet : Sheet) = notImplemented()

module Sheets = 
    
    let empty = new Sheets()

    let getSheets (sheets:Sheets) = notImplemented()
    let addSheets (sheets:Sheets) (newSheets:Sheet seq) = notImplemented()
    let addSheet (sheets:Sheets) (newSheet:Sheet) = notImplemented()

    let mapSheets f (sheets:Sheets) = notImplemented()
    let iterSheets f (sheets:Sheets) = notImplemented()
    let filterSheets f (sheets:Sheets) = notImplemented()

module Workbook =

    let empty = new Workbook()
    let initSheets (workbook:Workbook) = notImplemented()
    let getSheets (workbook:Workbook) = workbook.Sheets

    let addSheets (sheets:Sheets) (workbook:Workbook) = notImplemented()

    let ofWorkbookPart (workbookPart:WorkbookPart) = workbookPart.Workbook 

module WorkbookPart = 

    let ofSpreadsheet (spreadsheet:SpreadsheetDocument) = spreadsheet.WorkbookPart

    let getWorkbook (workbookPart:WorkbookPart) = workbookPart.Workbook 

    let addWorksheetPart (worksheetPart : WorksheetPart) (workbookPart:WorkbookPart) = notImplemented()
    let addEmptyWorksheetPart (workbookPart:WorkbookPart) = workbookPart.AddNewPart<WorksheetPart>()
    let getWorkSheetParts (workbookPart:WorkbookPart) = workbookPart.WorksheetParts

    //let addworkSheet (workbookPart:WorkbookPart) (worksheet : Worksheet) = 
    //    let emptySheet = (addNewWorksheetPart workbookPart)
    //    emptySheet.Worksheet <- worksheet


module Spreadsheet = 

    let openSpreadsheet (path:string) isEditable = SpreadsheetDocument.Open(path,isEditable)

    let createSpreadsheet (path:string) = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook)

    // The one thats included
    let getWorkbookPart (spreadsheet:SpreadsheetDocument) = spreadsheet.WorkbookPart

    // Only if none there
    let addNewWorkbookPart (spreadsheet:SpreadsheetDocument) = spreadsheet.AddWorkbookPart()

    //let getWorksheets = ""

    //let getWorksheetByName = ""
    
    //let getWorksheetByNumber = ""

    //let getWorksheetFirst = ""

