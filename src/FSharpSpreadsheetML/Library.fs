namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet

module internal H = let notImplemented() = failwith "function not yet implemented"
open H


//module CellType = 

//    let empty = CellType()


module CellValue = 

    let empty = CellValue()

    let create (value:string) = CellValue(value)

    let getValue (cellValue:CellValue) = cellValue.Text
    let setValue (value:string) (cellValue:CellValue) =  cellValue.Text <- value



module Cell =

    let empty = Cell()

    /// reference in "A1"-Style
    let create (dataType : CellValues) (reference:string) (value:CellValue) = 
        Cell(CellReference = StringValue.FromString reference, DataType = EnumValue(dataType), CellValue = value)

    /// "A1"-Style
    let getReference (cell:Cell) = cell.CellReference.Value
    /// "A1"-Style
    let setReference (reference) (cell:Cell) = 
        cell.CellReference <- StringValue.FromString reference
        cell

    let getValue (cell:Cell) = cell.CellValue
    let setValue (value:CellValue) (cell:Cell) = 
        cell.CellValue <- value
        cell

    let getType (cell:Cell) = cell.DataType
    let setType (dataType:CellValues) (cell:Cell) = 
        cell.DataType <- EnumValue(dataType)
        cell

module Spans =

    let fromBoundaries fromColumnIndex toColumnIndex = 
        sprintf "%i:%i" fromColumnIndex toColumnIndex
        |> StringValue.FromString
        |> List.singleton
        |> ListValue

    let toBoundaries (spans:ListValue<StringValue>) = 
        spans.Items
        |> Seq.head
        |> fun x -> x.Value.Split ':'
        |> fun a -> uint32 a.[0],uint32 a.[1]

module Row =

    let empty = Row()

    let getCells (row:Row) = row.Descendants<Cell>()
    let mapCells (f : Cell -> Cell) (row:Row) = 
        //getCells row
        //|> Seq.toArray
        //|> Array.map (fun cell -> cell <- f cell)
        notImplemented()

    let iterCells (f) (row:Row) = notImplemented()

    let findCell (f : Cell -> bool) (row:Row) =
        getCells row
        |> Seq.find f

    ///Reference in 
    let insertCellBefore newCell refCell (row:Row) = 
        row.InsertBefore(newCell, refCell) |> ignore
        row

    let getIndex (row:Row) = row.RowIndex

    let setIndex (index) (row:Row) =
        row.RowIndex <- index
        row

    let getSpan (row:Row) = row.Spans

    let setSpan spans (row:Row) = 
        row.Spans <- spans
        row

    let addCell (cell:Cell) (row:Row) = 
        row.AppendChild(cell) |> ignore
        row

    let create index spans (cells:Cell seq) = 
        Row(childElements = (cells |> Seq.map (fun x -> x :> OpenXmlElement)))
        |> setIndex (UInt32Value.FromUInt32 index)
        |> setSpan spans

module SheetData = 

    let empty = new SheetData()

    let appendRow (row:Row) (sheetData:SheetData) = 
        sheetData.AppendChild row |> ignore
        sheetData

    let getRows (sheetData:SheetData) = 
        sheetData.Descendants<Row>()

    let countRows (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.length
        

    let tryGetRowAt index (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.tryFind (Row.getIndex >> (=) index)

    let getRowAt index (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.find (Row.getIndex >> (=) index)
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

    let addSheetData (sheetData:SheetData) (worksheet:Worksheet) = 
        worksheet.AppendChild sheetData |> ignore
        worksheet

    let containsSheetData (worksheet:Worksheet) = worksheet.HasChildren
    let ofSheetData (sheetData:SheetData) = Worksheet(sheetData)
    let getSheetData (worksheet:Worksheet) = worksheet.GetFirstChild<SheetData>()
      
    //let setSheetData (sheetData:SheetData) (worksheet:Worksheet) = worksheet.sh


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


    //let addWorksheet (worksheet : Worksheet) (worksheetPart : WorksheetPart) = worksheetPart.
    let getWorksheet (worksheetPart : WorksheetPart) = 
        worksheetPart.Worksheet

    let setWorksheet (worksheet : Worksheet) (worksheetPart : WorksheetPart) = 
        worksheetPart.Worksheet <- worksheet
        worksheetPart

    let initWorksheet (worksheetPart:WorksheetPart) = 
        setWorksheet (Worksheet.empty) worksheetPart

    let containsWorksheet (worksheetPart:WorksheetPart) = worksheetPart.Worksheet <> null  

    let getWorksheetOrInitWorksheet (worksheetPart:WorksheetPart) =
        if containsWorksheet worksheetPart then
            getWorksheet worksheetPart
        else 
            initWorksheet worksheetPart
            |> getWorksheet

    //let setID id (worksheetPart : WorksheetPart) = notImplemented()
    //let getID (worksheetPart : WorksheetPart) = notImplemented()




/// Part of the Workbook, stores name and other additional info of the sheet
module Sheet = 
    
    let empty = Sheet()

    let setName (name:string) (sheet : Sheet) = 
        sheet.Name <- StringValue.FromString name
        sheet
    let getName (sheet : Sheet) = sheet.Name.Value

    /// ID shared with worksheet
    let setID id (sheet : Sheet) = 
        sheet.Id <- StringValue.FromString id
        sheet
    /// ID shared with worksheet
    let getID (sheet : Sheet) = sheet.Id.Value

    /// ID used for sorting
    let setSheetID id (sheet : Sheet) = 
        sheet.SheetId <- UInt32Value.FromUInt32 id
        sheet

    /// ID used for sorting
    let getSheetID (sheet : Sheet) = sheet.SheetId.Value

    let create id name sheetID = 
        Sheet()
        |> setID id
        |> setName name
        |> setSheetID sheetID

module Sheets = 

    let empty = new Sheets()

    let getFirstSheet (sheets:Sheets) = sheets.GetFirstChild<Sheet>()
    let getSheets (sheets:Sheets) = sheets.Elements<Sheet>()
    let addSheets (newSheets:Sheet seq) (sheets:Sheets) = 
        newSheets |> Seq.iter (fun sheet -> sheets.Append sheet) 
        sheets

    let addSheet (newSheet:Sheet) (sheets:Sheets) = 
        sheets.AppendChild(newSheet) |> ignore
        sheets

    let removeSheet (sheetToDelete:Sheet) (sheets:Sheets)  = 
        sheets.RemoveChild(sheetToDelete) |> ignore
        sheets

    /// Id Name SheetID -> bool
    let findSheet (f:string -> string -> uint32 -> bool) (sheets:Sheets) =
        getSheets sheets
        |> Seq.find (fun sheet -> f sheet.Id.Value sheet.Name.Value sheet.SheetId.Value)

    let countSheets (sheets:Sheets) = getSheets sheets |> Seq.length
    let mapSheets f (sheets:Sheets) = getSheets sheets |> Seq.toArray |> Array.map f
    let iterSheets f (sheets:Sheets) = getSheets sheets |> Seq.toArray |> Array.iter f
    let filterSheets f (sheets:Sheets) = 
        getSheets sheets |> Seq.toArray |> Array.filter (f >> not)
        |> Array.fold (fun st sheet -> removeSheet sheet st) sheets








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

    let setWorkbook (workbook:Workbook) (workbookPart:WorkbookPart) = 
        workbookPart.Workbook <- workbook
        workbookPart

    let initWorkbook (workbookPart:WorkbookPart) = 
        setWorkbook (Workbook()) workbookPart

    let containsWorkbook (workbookPart:WorkbookPart) = workbookPart.Workbook <> null  

    let getWorkbookOrInitWorkbook (workbookPart:WorkbookPart) =
        if containsWorkbook workbookPart then
            getWorkbook workbookPart
        else 
            initWorkbook workbookPart
            |> getWorkbook

    let addWorksheetPart (worksheetPart : WorksheetPart) (workbookPart:WorkbookPart) = workbookPart.AddPart(worksheetPart)
    let initWorksheetPart (workbookPart:WorkbookPart) = workbookPart.AddNewPart<WorksheetPart>()
    let getWorkSheetParts (workbookPart:WorkbookPart) = workbookPart.WorksheetParts
    let containsWorkSheetParts (workbookPart:WorkbookPart) = workbookPart.GetPartsOfType<WorksheetPart>() |> Seq.length |> (<>) 0
    let getWorksheetPartById (id:string) (workbookPart:WorkbookPart) = workbookPart.GetPartById(id) :?> WorksheetPart 
    let getWorksheetPartID (worksheetPart:WorksheetPart) (workbookPart:WorkbookPart) = workbookPart.GetIdOfPart worksheetPart
    //let addworkSheet (workbookPart:WorkbookPart) (worksheet : Worksheet) = 
    //    let emptySheet = (addNewWorksheetPart workbookPart)
    //    emptySheet.Worksheet <- worksheet

    let initSharedStringTablePart (workbookPart:WorkbookPart) = 
        workbookPart.AddNewPart<SharedStringTablePart>() |> ignore
        workbookPart
    let getSharedStringTableParts (workbookPart:WorkbookPart) = workbookPart.GetPartsOfType<SharedStringTablePart>()
    let getFirstSharedStringTablePart (workbookPart:WorkbookPart) = workbookPart.SharedStringTablePart
    let containsSharedStringTablePart (workbookPart:WorkbookPart) = getSharedStringTableParts workbookPart |> Seq.length |> (<>) 0


module Workbook =

    let empty = new Workbook()
    
    let getSheets (workbook:Workbook) = workbook.Sheets
    let containsSheets (workbook:Workbook) = 
        workbook.Sheets <> null

    let initSheets (workbook:Workbook) = 
        workbook.AppendChild<Sheets>(Sheets()) |> ignore
        workbook

    let getSheetsOrInitSheets (workbook:Workbook) =
        if containsSheets workbook then
            getSheets workbook
        else 
            initSheets workbook
            |> getSheets


    let ofWorkbookPart (workbookPart:WorkbookPart) = workbookPart.Workbook 

    let addSheet (name : string) (data : SheetData) (workbook : Workbook) =
        let workbookPart = workbook.WorkbookPart

        let worksheetPart = WorkbookPart.initWorksheetPart workbookPart

        WorksheetPart.getWorksheetOrInitWorksheet worksheetPart
        |> Worksheet.addSheetData data
        |> ignore
        
        let sheets = getSheetsOrInitSheets workbook
        let id = WorkbookPart.getWorksheetPartID worksheetPart workbookPart
        let sheetID = sheets |> Sheets.getSheets |> Seq.map Sheet.getSheetID |> Seq.max |> (+) 1ul
        let sheet = Sheet.create id name sheetID

        Sheets.addSheet sheet sheets |> ignore
        workbook



module Spreadsheet = 

    let openSpreadsheet (path:string) isEditable = SpreadsheetDocument.Open(path,isEditable)

    let createSpreadsheet (path:string) = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook)

    // The one thats included
    let getWorkbookPart (spreadsheet:SpreadsheetDocument) = spreadsheet.WorkbookPart

    // Only if none there
    let addNewWorkbookPart (spreadsheet:SpreadsheetDocument) = spreadsheet.AddWorkbookPart()

    let saveChanges (spreadsheet:SpreadsheetDocument) = spreadsheet.Save()

    let saveAs path (spreadsheet:SpreadsheetDocument) = spreadsheet.SaveAs(path)

    let close (spreadsheet:SpreadsheetDocument) = spreadsheet.Close()

    //let getWorksheets = ""

    //let getWorksheetByName = ""
    
    //let getWorksheetByNumber = ""

    //let getWorksheetFirst = ""

