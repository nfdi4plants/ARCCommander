namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet

module internal H = let notImplemented() = failwith "function not yet implemented"
open H



module CellValue = 

    let empty = CellValue()

    let create (value:string) = CellValue(value)

    let getValue (cellValue:CellValue) = cellValue.Text
    let setValue (value:string) (cellValue:CellValue) =  cellValue.Text <- value

module Reference =

    let letterToIndex (l:string) = 
        l.ToCharArray()
        |> Array.rev
        |> Array.mapi (fun i c -> 
            let factor = 26. ** (float i) |> uint
            System.Char.ToUpper c
            |> uint 
            |> fun u -> u - 64u
            |> (*) factor
            )
        |> Array.sum

    let indexToLetter i =
        let sb = System.Text.StringBuilder()
        let rec loop residual = 
            if residual = 0u then
                ()
            else
                let modulo = (residual - 1u) % 26u
                sb.Insert(0, char (modulo + 65u)) |> ignore
                loop ((residual - modulo) / 26u)
        loop i
        sb.ToString()

    /// 1 Based indices
    let ofIndices column (row:uint32) = 
        sprintf "%s%i" (indexToLetter column) row

    /// reference in "A1" style, returns 1 Based indices (column*row)
    let toIndices (reference : string) = 
        let inp = reference.ToUpper()
        let pattern = "([A-Z]*)(\d*)"
        let regex = System.Text.RegularExpressions.Regex.Match(inp,pattern)
        
        if regex.Success then
            regex.Groups
            |> fun a -> letterToIndex a.[1].Value, uint a.[2].Value
        else 
            failwithf "Reference %s does not match Excel A1-style" reference

    let moveHorizontal amount reference = 
        reference
        |> toIndices
        |> fun (c,r) -> (int64 c) + (int64 amount) |> uint32, r
        ||> ofIndices

    let moveVertical amount reference = 
        reference
        |> toIndices
        |> fun (c,r) -> c, (int64 r) + (int64 amount) |> uint32
        ||> ofIndices

module Cell =

    let empty = Cell()


    let inferCellValue (value : 'T) = 
        let value = box value
        match value with
        | :? char as c -> CellValues.String,c.ToString()
        | :? string as s -> CellValues.String,s.ToString()
        | :? bool as c -> CellValues.Boolean,c.ToString()
        | :? byte as i -> CellValues.Number,i.ToString()
        | :? sbyte as i -> CellValues.Number,i.ToString()
        | :? int as i -> CellValues.Number,i.ToString()
        | :? int16 as i -> CellValues.Number,i.ToString()
        | :? int64 as i -> CellValues.Number,i.ToString()
        | :? uint as i -> CellValues.Number,i.ToString()
        | :? uint16 as i -> CellValues.Number,i.ToString()
        | :? uint64 as i -> CellValues.Number,i.ToString()
        | :? single as i -> CellValues.Number,i.ToString()
        | :? float as i -> CellValues.Number,i.ToString()
        | :? decimal as i -> CellValues.Number,i.ToString()
        | :? System.DateTime as d -> CellValues.Date,d.Date.ToString()
        | _ ->  CellValues.String,value.ToString()

    /// reference in "A1"-Style
    let create (dataType : CellValues) (reference:string) (value:CellValue) = 
        Cell(CellReference = StringValue.FromString reference, DataType = EnumValue(dataType), CellValue = value)

    let createGeneric columnIndex rowIndex (value:'T) =
        let valType,value = inferCellValue value
        let reference = Reference.ofIndices columnIndex (rowIndex)
        create valType reference (CellValue.create value)

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
 
    let tryGetType (cell:Cell) = 
        if cell.DataType <> null then
            Some cell.DataType.Value
        else
            None

    let getType (cell:Cell) = cell.DataType.Value
    let setType (dataType:CellValues) (cell:Cell) = 
        cell.DataType <- EnumValue(dataType)
        cell

module Spans =


    let fromBoundaries fromColumnIndex toColumnIndex = 
        sprintf "%i:%i" fromColumnIndex toColumnIndex
        |> StringValue.FromString
        |> List.singleton
        |> ListValue

    // System.MissingMethodException: Method not found: 'System.String[] System.String.Split(Char, System.StringSplitOptions)'.
    //let toBoundaries (spans:ListValue<StringValue>) = 
    //    spans.Items
    //    |> Seq.head
    //    |> fun x -> x.Value.Split ':'
    //    |> fun a -> uint32 a.[0],uint32 a.[1]

    let toBoundaries (spans:ListValue<StringValue>) = 
        spans.Items
        |> Seq.head
        |> fun x -> System.Text.RegularExpressions.Regex.Matches(x.Value,@"\d*")
        |> fun a -> uint32 a.[0].Value,uint32 a.[2].Value


    let rightBoundary (spans:ListValue<StringValue>) = 
        toBoundaries spans
        |> snd

    let leftBoundary (spans:ListValue<StringValue>) = 
        toBoundaries spans
        |> fst

    let moveHorizontal amount (spans:ListValue<StringValue>) =
        spans
        |> toBoundaries
        |> fun (f,t) -> amount + f, amount + t
        ||> fromBoundaries

    let extendRight amount (spans:ListValue<StringValue>) =
        spans
        |> toBoundaries
        |> fun (f,t) -> f, amount + t
        ||> fromBoundaries

    let extendLeft amount (spans:ListValue<StringValue>) =
        spans
        |> toBoundaries
        |> fun (f,t) -> f - amount, t
        ||> fromBoundaries

    let referenceExtendsSpansToRight reference spans = 
        (reference |> Reference.toIndices |> fst) 
            > (spans |> toBoundaries |> snd |> uint)
        
        
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

    let getIndex (row:Row) = row.RowIndex.Value

    let containsCellAt (columnIndex) (row:Row) =
        row
        |> getCells
        |> Seq.exists (Cell.getReference >> Reference.toIndices >> fst >> (=) columnIndex)

    let getCellAt (columnIndex) (row:Row) =
        row
        |> getCells
        |> Seq.find (Cell.getReference >> Reference.toIndices >> fst >> (=) columnIndex)

    let tryGetCellAt (columnIndex) (row:Row) =
        row
        |> getCells
        |> Seq.tryFind (Cell.getReference >> Reference.toIndices >> fst >> (=) columnIndex)

    let tryGetCellAfter (columnIndex) (row:Row) =
        row
        |> getCells
        |> Seq.tryFind (Cell.getReference >> Reference.toIndices >> fst >> (<=) columnIndex)


    let setIndex (index) (row:Row) =
        row.RowIndex <- UInt32Value.FromUInt32 index
        row

    let getSpan (row:Row) = row.Spans

    let setSpan spans (row:Row) = 
        row.Spans <- spans
        row

    let extendSpanRight amount row = 
        getSpan row
        |> Spans.extendRight amount
        |> fun s -> setSpan s row

    let extendSpanLeft amount row = 
        getSpan row
        |> Spans.extendLeft amount
        |> fun s -> setSpan s row

    let appendCell (cell:Cell) (row:Row) = 
        row.AppendChild(cell) |> ignore
        row

    let create index spans (cells:Cell seq) = 
        Row(childElements = (cells |> Seq.map (fun x -> x :> OpenXmlElement)))
        |> setIndex index
        |> setSpan spans

module SheetData = 

    let empty = new SheetData()

    let insertBefore (row:Row) (refRow:Row) (sheetData:SheetData) = 
        sheetData.InsertBefore(row,refRow) |> ignore
        sheetData

    let appendRow (row:Row) (sheetData:SheetData) = 
        sheetData.AppendChild row |> ignore
        sheetData

    let getRows (sheetData:SheetData) = 
        sheetData.Descendants<Row>()

    let countRows (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.length
    
    let tryGetRowAfter index (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.tryFind (Row.getIndex >> (<=) index)

    let tryGetRowAt index (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.tryFind (Row.getIndex >> (=) index)

    let getRowAt index (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.find (Row.getIndex >> (=) index)
        //sheetData.
        //sheetData

    let containsRowAt index (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.exists (Row.getIndex >> (=) index)

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
    //let inferCellValue adress (worksheet:Worksheet) = notImplemented()
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

    let getText (ssi:SharedStringItem) = ssi.InnerText

    let setText text (ssi:SharedStringItem) = 
        ssi.Text <- Text(text)
        ssi

    let create text = 
        SharedStringItem()
        |> setText text


module SharedStringTable = 

    let empty = SharedStringTable() 

    let getItems (sst:SharedStringTable) = sst.Elements<SharedStringItem>()
    let tryGetIndexByString (s:string) (sst:SharedStringTable) = 
        getItems sst 
        |> Seq.tryFindIndex (fun x -> x.Text.Text = s)
    let getText i (sst:SharedStringTable) = 
        getItems sst
        |> Seq.item i
    let addItem (sharedStringItem:SharedStringItem) (sst:SharedStringTable) = 
        sst.Append(sharedStringItem.CloneNode(false) :?> SharedStringItem)
        sst
    let count (sst:SharedStringTable) = sst.Count.Value


module SharedStringTablePart = 
    
    let initSharedStringTable (sstPart:SharedStringTablePart) = 
        sstPart.SharedStringTable <- SharedStringTable.empty
        sstPart
    let getSharedStringTable (sstPart:SharedStringTablePart) = sstPart.SharedStringTable
    let setSharedStringTable (sst:SharedStringTable) (sstPart:SharedStringTablePart) = 
        sstPart.SharedStringTable <- sst
        sstPart



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
    let getSharedStringTablePart (workbookPart:WorkbookPart) = workbookPart.SharedStringTablePart
    let containsSharedStringTablePart (workbookPart:WorkbookPart) = workbookPart.SharedStringTablePart <> null

    let getSharedStringTablePartOrInitSharedStringTablePart (workbookPart:WorkbookPart) =
        if containsSharedStringTablePart workbookPart then
            getSharedStringTablePart workbookPart
        else 
            initSharedStringTablePart workbookPart
            |> getSharedStringTablePart


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
        let sheetID = 
            sheets |> Sheets.getSheets |> Seq.map Sheet.getSheetID
            |> fun s -> 
                if Seq.length s = 0 then 1u
                else s |> Seq.max |> (+) 1ul
        let sheet = Sheet.create id name sheetID

        Sheets.addSheet sheet sheets |> ignore
        workbook


module SheetTransformation =
       
    ///If the row with index rowIndex exists in the sheet, moves it downwards by amount. Negative amounts will move the row upwards
    let moveRowVertical (amount:int) rowIndex (sheet:SheetData) = 
        match sheet |> SheetData.tryGetRowAt (rowIndex |> uint32) with
        | Some row -> 
            Row.setIndex (Row.getIndex row |> int64 |> (+) (int64 amount) |> uint32) row
            |> Row.getCells
            |> Seq.iter (fun cell -> 
                Cell.setReference (Cell.getReference cell |> Reference.moveVertical amount) cell
                |> ignore            
            )
            sheet
        | None -> 
            printfn "Warning: Row with index %i does not exist" rowIndex
            sheet

    ///If a cell with the given columnIndex exists in the row. Moves it one column to the right. 
    ///
    /// If there already was a cell at the new postion, moves that one too. Repeats until a value is moved into a position previously unoccupied.
    let rec shoveValuesInRowToRight columnIndex (row:Row) : Row=
        let spans = Row.getSpan row
        match Row.tryGetCellAt columnIndex row with
        | Some cell ->
            shoveValuesInRowToRight (columnIndex+1u) row |> ignore
            let newReference = (Cell.getReference cell |> Reference.moveHorizontal 1)            
            Cell.setReference newReference cell |> ignore
            if Spans.referenceExtendsSpansToRight newReference spans then
                Row.extendSpanRight 1u row
            else row
        | None -> row

    ///If a row with the given rowIndex exists in the sheet, moves it one position downwards. 
    ///
    /// If there already was a row at the new postion, moves that one too. Repeats until a row is moved into a position previously unoccupied.
    let rec shoveRowsDownward rowIndex (sheet) =
        if SheetData.containsRowAt rowIndex sheet then             
            shoveRowsDownward (rowIndex+1u) sheet
            |> moveRowVertical 1 rowIndex
        else
            sheet

    ///If a row with the given rowIndex exists in the sheet, moves all values inside it to the right by the specified amount. Negative amounts will move the values to the left.
    let moveRowHorizontal amount rowIndex (sheet:SheetData) = 
        match sheet |> SheetData.tryGetRowAt (rowIndex |> uint32) with
        | Some row -> 
            Row.setIndex (Row.getIndex row |> (+) (uint32 amount)) row
            |> Row.getCells
            |> Seq.iter (fun cell -> 
                Cell.setReference (Cell.getReference cell |> Reference.moveVertical amount) cell
                |> ignore            
            )
            sheet
        | None -> 
            printfn "Warning: Row with index %i does not exist" rowIndex
            sheet  

    let maxRowIndex (sheet) =
        SheetData.getRows sheet
        |> fun s -> 
            if Seq.isEmpty s then 
                0u
            else 
                s
                |> Seq.map (Row.getIndex)
                |> Seq.max

    let firstSheetOfWorkbookPart (workbookPart) = 
        workbookPart
        |> WorkbookPart.getWorkSheetParts
        |> Seq.head
        |> WorksheetPart.getWorksheet
        |> Worksheet.getSheetData

    module SSTSheets =
        let private getValueSST (sst:SharedStringTable) cell =
            match cell |> Cell.tryGetType with
            | Some (CellValues.SharedString) ->
                cell
                |> Cell.getValue
                |> CellValue.getValue
                |> int
                |> fun i -> SharedStringTable.getText i sst
                |> SharedStringItem.getText
            | _ ->
                cell
                |> Cell.getValue
                |> CellValue.getValue   
            
        let getValuesOfRowSST (workbookPart:WorkbookPart) (row:Row) =
            let sst = 
                workbookPart 
                |> WorkbookPart.getSharedStringTablePart 
                |> SharedStringTablePart.getSharedStringTable
            row
            |> Row.getCells
            |> Seq.map (getValueSST sst)
        
        let getIndexedValuesOfRowSST (workbookPart:WorkbookPart) (row:Row) =
            let sst = 
                workbookPart 
                |> WorkbookPart.getSharedStringTablePart 
                |> SharedStringTablePart.getSharedStringTable
            row
            |> Row.getCells
            |> Seq.map (fun c -> c |> Cell.getReference |> Reference.toIndices |> fst, getValueSST sst c)
        
        let getCellValueSSTAt (workbookPart:WorkbookPart) (columnIndex : uint32) rowIndex (sheet:SheetData) =
               let sst = 
                   workbookPart 
                   |> WorkbookPart.getSharedStringTablePart 
                   |> SharedStringTablePart.getSharedStringTable
               SheetData.getRowAt rowIndex sheet
               |> Row.getCellAt columnIndex
               |> getValueSST sst

        let getRowValuesSSTAt (workbookPart:WorkbookPart) rowIndex (sheet:SheetData) =
            let sst = 
                workbookPart 
                |> WorkbookPart.getSharedStringTablePart 
                |> SharedStringTablePart.getSharedStringTable
            sheet
            |> SheetData.getRowAt rowIndex
            |> Row.getCells
            |> Seq.map (getValueSST sst)

        let tryGetRowValuesSSTAt (workbookPart:WorkbookPart) rowIndex (sheet:SheetData) =
            let sst = 
                workbookPart 
                |> WorkbookPart.getSharedStringTablePart 
                |> SharedStringTablePart.getSharedStringTable
            sheet
            |> SheetData.tryGetRowAt rowIndex
            |> Option.map (
                Row.getCells
                >> Seq.map (getValueSST sst)
            )

        let tryGetIndexedRowValuesSSTAt (workbookPart:WorkbookPart) rowIndex (sheet:SheetData) =
            let sst = 
                workbookPart 
                |> WorkbookPart.getSharedStringTablePart 
                |> SharedStringTablePart.getSharedStringTable
            sheet
            |> SheetData.tryGetRowAt rowIndex
            |> Option.map (
                Row.getCells
                >> Seq.map (fun c -> Cell.getReference c |> Reference.toIndices |> fst,getValueSST sst c)
            )

        let createSSTCell (sst:SharedStringTable) columnIndex rowIndex  (value:'T) = 
            let value = box value
            match value with
            | :? string as s -> 
                let reference = Reference.ofIndices columnIndex (rowIndex)
                match SharedStringTable.tryGetIndexByString s sst with
                | Some i -> 
                    sst,Cell.create CellValues.SharedString reference (i |> string |> CellValue.create)
                | None ->
                    let sst = SharedStringTable.addItem (SharedStringItem.create s) sst
                    sst, Cell.create CellValues.SharedString reference (SharedStringTable.count sst |> (+) 1u |> string |> CellValue.create)
            | _  -> 
                sst,Cell.createGeneric columnIndex rowIndex (value.ToString())

        
        let insertRowWithHorizontalOffsetSSTAt (workbookPart) (offset:int) (vals: 'T seq) rowIndex (sheet:SheetData) =
    
            let sst = workbookPart |> WorkbookPart.getSharedStringTablePart |> SharedStringTablePart.getSharedStringTable

            let uiO = uint32 offset
            let spans = Spans.fromBoundaries (uiO + 1u) (Seq.length vals |> uint32 |> (+) uiO )
            let newRow = 
                vals
                |> Seq.mapi (fun i v -> 
                    createSSTCell sst ((int64 i) + 1L + (int64 offset) |> uint32) rowIndex v
                    |> snd
                    |> fun r -> r.CloneNode(true) :?> Cell
                )
                |> Row.create (uint32 rowIndex) spans
                |> fun r -> r.CloneNode(true) :?> Row
            let refRow = SheetData.tryGetRowAfter (uint rowIndex) sheet
            match refRow with
            | Some ref -> 
                shoveRowsDownward rowIndex sheet
                |> SheetData.insertBefore newRow ref
            | None ->
                SheetData.appendRow newRow sheet
               
        let insertValueIntoRowSST workbookPart index (value:'T) (row:Row) = 

            let sst = workbookPart |> WorkbookPart.getSharedStringTablePart |> SharedStringTablePart.getSharedStringTable
            let refCell = Row.tryGetCellAfter index row

            let updatedSST,cell = createSSTCell sst index (Row.getIndex row) value

            match refCell with
            | Some ref -> 
                shoveValuesInRowToRight index row
                |> Row.insertCellBefore cell ref
            | None ->
                let spans = Row.getSpan row
                let spanExceedance = (uint index) - (spans |> Spans.rightBoundary)
                                   
                Row.extendSpanRight spanExceedance row
                |> Row.appendCell cell

        
        let appendRowSST workbookPart (vals: 'T seq) (sheet:SheetData) =
            let i = 
                sheet
                |> maxRowIndex
                |> (+) 1u
            insertRowWithHorizontalOffsetSSTAt workbookPart 0 vals i sheet

        let appendValueToRowSST workbookPart (value:'T) (row:Row) = 
            let sst = workbookPart |> WorkbookPart.getSharedStringTablePart |> SharedStringTablePart.getSharedStringTable
            row
            |> Row.getSpan
            |> Spans.rightBoundary
            |> fun col -> createSSTCell sst (col + 1u) (row |> Row.getIndex) value
            |> fun (newSST,c) -> Row.appendCell c row
            |> Row.extendSpanRight 1u 

        let insertRowSSTAt workbookpart (vals: 'T seq) rowIndex (sheet:SheetData) =
            insertRowWithHorizontalOffsetSSTAt workbookpart 0 vals rowIndex sheet

        /// 1 based index   
        let insertValueSST workbookPart columnIndex rowIndex (value:'T) (sheet:SheetData) =
            match SheetData.tryGetRowAt rowIndex sheet with
            | Some row -> 
                insertValueIntoRowSST workbookPart columnIndex value row |> ignore
                sheet
            | None -> insertRowWithHorizontalOffsetSSTAt workbookPart (columnIndex - 1u |> int) [value] rowIndex sheet

    module DirectSheets = 
        let getCellValueAt columnIndex rowIndex (sheet:SheetData) =
            SheetData.getRowAt rowIndex sheet
            |> Row.getCellAt columnIndex
            |> Cell.getValue
            |> CellValue.getValue
 

        let getRowValuesAt rowIndex (sheet:SheetData) =
            sheet
            |> SheetData.getRowAt rowIndex
            |> Row.getCells
            |> Seq.map (Cell.getValue >> CellValue.getValue)


        let tryGetRowValuesAt rowIndex (sheet:SheetData) =
            sheet 
            |> SheetData.tryGetRowAt rowIndex
            |> Option.map (
                Row.getCells
                >> Seq.map (Cell.getValue >> CellValue.getValue)
            )
     
        /// 1 based index   
        let insertRowWithHorizontalOffsetAt (offset:int) (vals: 'T seq) rowIndex (sheet:SheetData) =
        
            let uiO = uint32 offset
            let spans = Spans.fromBoundaries (uiO + 1u) (Seq.length vals |> uint32 |> (+) uiO )
            let newRow = 
                vals
                |> Seq.mapi (fun i v -> 
                    Cell.createGeneric ((int64 i) + 1L + (int64 offset) |> uint32) rowIndex v
                )
                |> Row.create (uint32 rowIndex) spans
            let refRow = SheetData.tryGetRowAfter (uint rowIndex) sheet
            match refRow with
            | Some ref -> 
                shoveRowsDownward rowIndex sheet
                |> SheetData.insertBefore newRow ref
            | None ->
                SheetData.appendRow newRow sheet

        let insertValueIntoRow index (value:'T) (row:Row) = 
            let refCell = Row.tryGetCellAfter index row

            let cell = Cell.createGeneric index (Row.getIndex row) value

            match refCell with
            | Some ref -> 
                shoveValuesInRowToRight index row
                |> Row.insertCellBefore cell ref
            | None ->
                let spans = Row.getSpan row
                let spanExceedance = (uint index) - (spans |> Spans.rightBoundary)
                               
                Row.extendSpanRight spanExceedance row
                |> Row.appendCell cell

        let appendValueToRow (value:'T) (row:Row) = 
            row
            |> Row.getSpan
            |> Spans.rightBoundary
            |> fun col -> Cell.createGeneric (col + 1u) (row |> Row.getIndex) value
            |> fun c -> Row.appendCell c row
            |> Row.extendSpanRight 1u 

        /// 1 based index   
        let insertRowAt (vals: 'T seq) rowIndex (sheet:SheetData) =
            insertRowWithHorizontalOffsetAt 0 vals rowIndex sheet


        /// 1 based index   
        let insertValue columnIndex rowIndex (value:'T) (sheet:SheetData) =
            match SheetData.tryGetRowAt rowIndex sheet with
            | Some row -> 
                insertValueIntoRow columnIndex value row |> ignore
                sheet
            | None -> insertRowWithHorizontalOffsetAt (columnIndex - 1u |> int) [value] rowIndex sheet

        /// 1 based index 
        let appendValueToRowAt rowIndex (value:'T) (sheet:SheetData) =
            match SheetData.tryGetRowAt rowIndex sheet with
            | Some row -> 
                appendValueToRow value row |> ignore
                sheet
            | None -> insertRowAt [value] rowIndex sheet    


        let appendRow (vals: 'T seq) (sheet:SheetData) =
            let i = 
                sheet
                |> maxRowIndex
                |> (+) 1u
            insertRowAt vals i sheet
  

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
    //let inferCellValue adress (sheetData:SheetData) = notImplemented()
    //let deleteCellValue adress (sheetData:SheetData) = notImplemented()

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

    let createEmptySSTSpreadsheet sheetName (path:string) = 
        let doc = createSpreadsheet path
        let workbookPart = addNewWorkbookPart doc

        let sharedStringTablePart = WorkbookPart.getSharedStringTablePartOrInitSharedStringTablePart workbookPart
        SharedStringTablePart.initSharedStringTable sharedStringTablePart |> ignore

        let workbook = WorkbookPart.getWorkbookOrInitWorkbook workbookPart
        Workbook.addSheet sheetName (SheetData.empty) workbook |> ignore
        doc

    //let getWorksheets = ""

    //let getWorksheetByName = ""
    
    //let getWorksheetByNumber = ""

    //let getWorksheetFirst = ""

