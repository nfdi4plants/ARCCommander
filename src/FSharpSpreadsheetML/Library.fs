namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet

module H = let notImplemented() = failwith "function not yet implemented"
open H


//module CellType = 

//    let empty = CellType()


module CellValue = 

    let empty = CellValue()

    let inline createString (value:string) = CellValue(value)

    let getValue (cellValue:CellValue) = cellValue.InnerText
    let setValue (value:'T) (cellValue:CellValue) =  notImplemented()



module Cell =

    let empty = Cell()

    /// reference in "A1"-Style
    let create (dataType : CellValues) (reference) (value:CellValue) = 
        Cell(CellReference = reference, DataType = EnumValue(dataType), CellValue = value)

    /// "A1"-Style
    let getReference (cell:Cell) = cell.CellReference
    /// "A1"-Style
    let setReference (reference) (cell:Cell) = 
        cell.CellReference <- reference
        cell

    let getValue (cell:Cell) = cell.CellValue
    let setValue (value:CellValue) (cell:Cell) = 
        cell.CellValue <- value
        cell

    let getType (cell:Cell) = cell.DataType
    let setType (dataType:CellValues) (cell:Cell) = 
        cell.DataType <- EnumValue(dataType)
        cell

module Row =

    let emtpy = Row()

    let getCells (row:Row) = notImplemented()
    let mapCells (f) (row:Row) = notImplemented()
    let iterCells (f) (row:Row) = notImplemented()

    ///Reference in 
    let insertBefore () (reference) (row:Row) = 
        notImplemented()
        //row.Insert


module SheetData = 

    let empty = new SheetData()

    let appendRow (row:Row) (sheetData:SheetData) = 
        sheetData.Append row
        sheetData

    let getRow (sheetData:SheetData) = 
        notImplemented()
        //sheetData.
        //sheetData


    ///Convenience

    //let insertRow (rowIndex) (values: 'T seq) (sheetData:SheetData) = notImplemented()
    //let overWriteRow (rowIndex) (values: 'T seq) (sheetData:SheetData) = notImplemented()
    //let appendRow (values: 'T seq) (sheetData:SheetData) = notImplemented()
    //let getRow (rowIndex) (sheetData:SheetData) = notImplemented()
    //let deleteRow rowIndex (sheetData:SheetData) = notImplemented()

    //let insertColumn (columnIndex) (values: 'T seq) (sheetData:SheetData) = notImplemented()
    //let overWriteColumn (columnIndex) (values: 'T seq) (sheetData:SheetData) = notImplemented()
    //let appendColumn (values: 'T seq) (sheetData:SheetData) = notImplemented()
    //let getColumn (columnIndex) (sheetData:SheetData) = notImplemented()
    //let deleteColumn (columnIndex) (sheetData:SheetData) = notImplemented()

    ////let setCellValue (rowIndex,columnIndex) value (sheetData:SheetData) = notImplemented()
    //let setCellValue adress value (sheetData:SheetData) = notImplemented()
    //let getCellValue adress (sheetData:SheetData) = notImplemented()
    //let deleteCellValue adress (sheetData:SheetData) = notImplemented()

/// Stores data of the sheet 
module Worksheet = 

    let empty = Worksheet()

    let containsSheetData (worksheet:Worksheet) = worksheet.HasChildren
    let ofSheetData (sheetData:SheetData) = Worksheet(sheetData)
    let getSheetData (worksheet:Worksheet) = worksheet.GetFirstChild<SheetData>()
      
    let setSheetData (sheetData:SheetData) (worksheet:Worksheet) = notImplemented()

    ///Convenience

    //let insertRow (rowIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let overWriteRow (rowIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let appendRow (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let getRow (rowIndex) (worksheet:Worksheet) = notImplemented()
    //let deleteRow rowIndex (worksheet:Worksheet) = notImplemented()

    //let insertColumn (columnIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let overWriteColumn (columnIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let appendColumn (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let getColumn (columnIndex) (worksheet:Worksheet) = notImplemented()
    //let deleteColumn (columnIndex) (worksheet:Worksheet) = notImplemented()

    ////let setCellValue (rowIndex,columnIndex) value (worksheet:Worksheet) = notImplemented()
    //let setCellValue adress value (worksheet:Worksheet) = notImplemented()
    //let getCellValue adress (worksheet:Worksheet) = notImplemented()
    //let deleteCellValue adress (worksheet:Worksheet) = notImplemented()


module WorksheetPart = 

    let containsWorksheet (worksheetPart : WorksheetPart) = worksheetPart.Worksheet
    //let addWorksheet (worksheet : Worksheet) (worksheetPart : WorksheetPart) = worksheetPart.
    let setWorksheet (worksheet : Worksheet) (worksheetPart : WorksheetPart) = worksheetPart.Worksheet <- worksheet
    let getWorksheet (worksheetPart : WorksheetPart) = worksheetPart.Worksheet

    //let setID id (worksheetPart : WorksheetPart) = notImplemented()
    //let getID (worksheetPart : WorksheetPart) = notImplemented()




/// Part of the Workbook, stores name and other additional info of the sheet
module Sheet = 
    
    let empty = Sheet()

    let setName (name:string) (sheet : Sheet) = sheet.Name <- StringValue.FromString name
    let getName (sheet : Sheet) = sheet.Name.Value

    /// ID shared with worksheet
    let setID id (sheet : Sheet) = sheet.Id <- StringValue.FromString id
    /// ID shared with worksheet
    let getID (sheet : Sheet) = sheet.Id.Value

    /// ID used for sorting
    let setSheetID id (sheet : Sheet) = sheet.SheetId <- id
    /// ID used for sorting
    let getSheetID (sheet : Sheet) = sheet.SheetId

module Sheets = 

    let empty = new Sheets()

    let getFirstSheet (sheets:Sheets) = sheets.GetFirstChild<Sheet>()
    let getSheets (sheets:Sheets) = sheets.Elements<Sheet>()
    let addSheets (sheets:Sheets) (newSheets:Sheet seq) = 
        newSheets |> Seq.iter (fun sheet -> sheets.Append sheet) 
        sheets

    let addSheet (sheets:Sheets) (newSheet:Sheet) = 
        sheets.Append(newSheet)
        sheets

    /// Id Name SheetID -> bool
    let findSheet (f:string -> string -> uint32 -> bool) (sheets:Sheets) =
        getSheets sheets
        |> Seq.find (fun sheet -> f sheet.Id.Value sheet.Name.Value sheet.SheetId.Value)

    let countSheets (sheets:Sheets) = getSheets sheets |> Seq.length
    let mapSheets f (sheets:Sheets) = notImplemented()
    let iterSheets f (sheets:Sheets) = notImplemented()
    let filterSheets f (sheets:Sheets) = notImplemented()

module Workbook =

    let empty = new Workbook()
    let initSheets (workbook:Workbook) = workbook.AppendChild<Sheets>(new Sheets());
    let getSheets (workbook:Workbook) = workbook.Sheets
    let containsSheets (workbook:Workbook) = workbook.Sheets

    let addSheets (sheets:Sheets) (workbook:Workbook) = workbook.AppendChild<Sheets>(sheets);

    let ofWorkbookPart (workbookPart:WorkbookPart) = workbookPart.Workbook 

module SharedStringItem = 

    let f (x:SharedStringItem) = x
    

module SharedStringTable = 

    let empty = SharedStringTable() 

    let getIndexByString (s:string) (sst:SharedStringTable) = notImplemented()
    let getItem (sst:SharedStringTable) = notImplemented()
    let addItem (sharedStringItem:SharedStringItem) (sst:SharedStringTable) = notImplemented()

    let exists f (sst:SharedStringTable) = notImplemented()
    let getItems (sst:SharedStringTable) = notImplemented()
    let getMappings (sst:SharedStringTable) = notImplemented()
    let count (sst:SharedStringTable) = sst.Count


module SharedStringTablePart = 
    

    let addEmptySharedStringTable (sstPart:SharedStringTablePart) = sstPart.SharedStringTable <- SharedStringTable.empty
    let getSharedStringTable (sstPart:SharedStringTablePart) = sstPart.SharedStringTable
    let setSharedStringTable (sst:SharedStringTable) (sstPart:SharedStringTablePart) = sstPart.SharedStringTable <- sst



module WorkbookPart = 

    let ofSpreadsheet (spreadsheet:SpreadsheetDocument) = spreadsheet.WorkbookPart

    let getWorkbook (workbookPart:WorkbookPart) = workbookPart.Workbook 
    let addWorkBook (workbookPart:WorkbookPart) = notImplemented()
    let containsWorkBook (workbookPart:WorkbookPart) = notImplemented()

    let addWorksheetPart (worksheetPart : WorksheetPart) (workbookPart:WorkbookPart) = workbookPart.AddPart(worksheetPart)
    let addEmptyWorksheetPart (workbookPart:WorkbookPart) = workbookPart.AddNewPart<WorksheetPart>()
    let getWorkSheetParts (workbookPart:WorkbookPart) = workbookPart.WorksheetParts
    let containsWorkSheetParts (workbookPart:WorkbookPart) = workbookPart.GetPartsOfType<WorksheetPart>() |> Seq.length |> (<>) 0
    let getWorkSheetPartById (id:string) (workbookPart:WorkbookPart) = workbookPart.GetPartById(id) :?> WorksheetPart 

    //let addworkSheet (workbookPart:WorkbookPart) (worksheet : Worksheet) = 
    //    let emptySheet = (addNewWorksheetPart workbookPart)
    //    emptySheet.Worksheet <- worksheet
    
    let addEmptySharedStringTablePart (workbookPart:WorkbookPart) = workbookPart.AddNewPart<SharedStringTablePart>();
    let getSharedStringTableParts (workbookPart:WorkbookPart) = workbookPart.GetPartsOfType<SharedStringTablePart>()
    let getFirstSharedStringTablePart (workbookPart:WorkbookPart) = workbookPart.SharedStringTablePart
    let containsSharedStringTablePart (workbookPart:WorkbookPart) = getSharedStringTableParts workbookPart |> Seq.length |> (<>) 0
    

module Spreadsheet = 

    let openSpreadsheet (path:string) isEditable = SpreadsheetDocument.Open(path,isEditable)

    let createSpreadsheet (path:string) = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook)

    // The one thats included
    let getWorkbookPart (spreadsheet:SpreadsheetDocument) = spreadsheet.WorkbookPart

    // Only if none there
    let addNewWorkbookPart (spreadsheet:SpreadsheetDocument) = spreadsheet.AddWorkbookPart()

    let saveChanges (spreadsheet:SpreadsheetDocument) = notImplemented()

    let close (spreadsheet:SpreadsheetDocument) = notImplemented()

    //let getWorksheets = ""

    //let getWorksheetByName = ""
    
    //let getWorksheetByNumber = ""

    //let getWorksheetFirst = ""

