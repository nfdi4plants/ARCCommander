namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet



module internal H = let notImplemented() = failwith "function not yet implemented"
open H


/// Functions for manipulating CellValues
module CellValue = 

    /// Empty cellvalue
    let empty = CellValue()

    /// Create a new cellValue containing the given string
    let create (value:string) = CellValue(value)

    /// Returns the value stored inside the cellValue
    let getValue (cellValue:CellValue) = cellValue.Text

    /// Sets the value inside the cellValue
    let setValue (value:string) (cellValue:CellValue) =  cellValue.Text <- value

/// Helper functions for working with "A1" style excel cell references
module Reference =

    /// Transforms excel column string indices (e.g. A, B, Z, AA, CD) to index number (starting with A = 1)
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

    /// Transforms number index to excel column string indices (e.g. A, B, Z, AA, CD) (starting with A = 1)
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

    /// Maps 1 based column and row indices to "A1" style reference 
    let ofIndices column (row:uint32) = 
        sprintf "%s%i" (indexToLetter column) row

    /// Maps a "A1" style excel cell reference to a column*row index tuple (1 Based indices)
    let toIndices (reference : string) = 
        let inp = reference.ToUpper()
        let pattern = "([A-Z]*)(\d*)"
        let regex = System.Text.RegularExpressions.Regex.Match(inp,pattern)
        
        if regex.Success then
            regex.Groups
            |> fun a -> letterToIndex a.[1].Value, uint a.[2].Value
        else 
            failwithf "Reference %s does not match Excel A1-style" reference

    /// Changes the column portion of a "A1"-style reference by the given amount (positite amount = increase and vice versa)
    let moveHorizontal amount reference = 
        reference
        |> toIndices
        |> fun (c,r) -> (int64 c) + (int64 amount) |> uint32, r
        ||> ofIndices

    /// Changes the row portion of a "A1"-style reference by the given amount (positite amount = increase and vice versa)
    let moveVertical amount reference = 
        reference
        |> toIndices
        |> fun (c,r) -> c, (int64 r) + (int64 amount) |> uint32
        ||> ofIndices

/// Functions for creating and manipulating cells
module Cell =

    /// Empty cell
    let empty = Cell()

    /// Returns the proper CellValues case for the given value
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

    /// Creates a cell, from a CellValues type case, a "A1" style reference and a cellValue containing the value string
    let create (dataType : CellValues) (reference:string) (value:CellValue) = 
        Cell(CellReference = StringValue.FromString reference, DataType = EnumValue(dataType), CellValue = value)

    /// Creates a cell from the 1 based column and row indices and a value
    let createGeneric columnIndex rowIndex (value:'T) =
        let valType,value = inferCellValue value
        let reference = Reference.ofIndices columnIndex (rowIndex)
        create valType reference (CellValue.create value)

    /// Gets "A1"-Style cell reference
    let getReference (cell:Cell) = cell.CellReference.Value

    /// Sets "A1"-Style cell reference
    let setReference (reference) (cell:Cell) = 
        cell.CellReference <- StringValue.FromString reference
        cell

    /// Gets Some cellValue if cellValue is existent. Else returns None
    let tryGetValue (cell:Cell) = 
        if cell.CellValue <> null then
            Some cell.CellValue
        else
            None

    /// Gets cellValue
    let getValue (cell:Cell) = cell.CellValue

    /// Sets cellValue
    let setValue (value:CellValue) (cell:Cell) = 
        cell.CellValue <- value
        cell
    
    /// Gets Some type if existent. Else returns None
    let tryGetType (cell:Cell) = 
        if cell.DataType <> null then
            Some cell.DataType.Value
        else
            None
    
    /// Gets Cell type
    let getType (cell:Cell) = cell.DataType.Value

    /// sets Cell type
    let setType (dataType:CellValues) (cell:Cell) = 
        cell.DataType <- EnumValue(dataType)
        cell

/// Helper functions for working with "1:1" style row spans
///
/// The spans marks the column wise area in which the row lies. 
module Spans =

    /// Given 1 based column start and end indices, returns a "1:1" style spans
    let fromBoundaries fromColumnIndex toColumnIndex = 
        sprintf "%i:%i" fromColumnIndex toColumnIndex
        |> StringValue.FromString
        |> List.singleton
        |> ListValue

    /// Given a "1:1" style spans , returns 1 based column start and end indices
    let toBoundaries (spans:ListValue<StringValue>) = 
        spans.Items
        |> Seq.head
        |> fun x -> x.Value.Split ':'
        |> fun a -> uint32 a.[0],uint32 a.[1]

    //let toBoundaries (spans:ListValue<StringValue>) = 
    //    spans.Items
    //    |> Seq.head
    //    |> fun x -> System.Text.RegularExpressions.Regex.Matches(x.Value,@"\d*")
    //    |> fun a -> uint32 a.[0].Value,uint32 a.[2].Value

    /// Gets the right boundary of the spans
    let rightBoundary (spans:ListValue<StringValue>) = 
        toBoundaries spans
        |> snd

    /// Gets the left boundary of the spans
    let leftBoundary (spans:ListValue<StringValue>) = 
        toBoundaries spans
        |> fst

    /// Moves both start and end of the spans by the given amount (positive amount moves spans to right and vice versa)
    let moveHorizontal amount (spans:ListValue<StringValue>) =
        spans
        |> toBoundaries
        |> fun (f,t) -> amount + f, amount + t
        ||> fromBoundaries

    /// Extends the righ boundary of the spans by the given amount (positive amount increases spans to right and vice versa)
    let extendRight amount (spans:ListValue<StringValue>) =
        spans
        |> toBoundaries
        |> fun (f,t) -> f, amount + t
        ||> fromBoundaries

    /// Extends the left boundary of the spans by the given amount (positive amount decreases the spans to left and vice versa)
    let extendLeft amount (spans:ListValue<StringValue>) =
        spans
        |> toBoundaries
        |> fun (f,t) -> f - amount, t
        ||> fromBoundaries

    /// Returns true if the column index of the reference is exceeds the right boundary of the spans
    let referenceExceedsSpansToRight reference spans = 
        (reference |> Reference.toIndices |> fst) 
            > (spans |> rightBoundary)
        
    /// Returns true if the column index of the reference is exceeds the left boundary of the spans
    let referenceExceedsSpansToLeft reference spans = 
        (reference |> Reference.toIndices |> fst) 
            < (spans |> leftBoundary)  
     
    /// Returns true if the column index of the reference does not lie in the boundary of the spans
    let referenceExceedsSpans reference spans = 
        referenceExceedsSpansToRight reference spans
        ||
        referenceExceedsSpansToLeft reference spans

/// Functions for working with rows (unmanaged: spans and cell references do not get automatically updated)
module Row =

    /// Empty Row
    let empty = Row()

    /// Returns a sequence of cells contained in the row
    let getCells (row:Row) = row.Descendants<Cell>()

    /// Returns true,if the row contains no cells
    let isEmpty (row:Row)= getCells row |> Seq.length |> (=) 0
    
    let mapCells (f : Cell -> Cell) (row:Row) = 
        row
        |> getCells
        |> Seq.iter (f >> ignore)
        row

    //let iterCells (f) (row:Row) = notImplemented()

    /// Returns the first cell in the row for which the predicate returns true
    let findCell (predicate : Cell -> bool) (row:Row) =
        getCells row
        |> Seq.find predicate

    /// Inserts a cell into the row before a reference cell
    let insertCellBefore newCell refCell (row:Row) = 
        row.InsertBefore(newCell, refCell) |> ignore
        row

    /// Returns the rowindex of the row
    let getIndex (row:Row) = row.RowIndex.Value

    /// Sets the row index of the row
    let setIndex (index) (row:Row) =
        row.RowIndex <- UInt32Value.FromUInt32 index
        row
    
    /// Returns true, if the row contains a cell with the given columnIndex
    let containsCellAt (columnIndex) (row:Row) =
        row
        |> getCells
        |> Seq.exists (Cell.getReference >> Reference.toIndices >> fst >> (=) columnIndex)

    /// Returns cell with the given columnIndex
    let getCellAt (columnIndex) (row:Row) =
        row
        |> getCells
        |> Seq.find (Cell.getReference >> Reference.toIndices >> fst >> (=) columnIndex)

    /// Returns cell with the given columnIndex if it exists, else returns none
    let tryGetCellAt (columnIndex) (row:Row) =
        row
        |> getCells
        |> Seq.tryFind (Cell.getReference >> Reference.toIndices >> fst >> (=) columnIndex)

    /// Returns cell matching or exceeding the given column index if it exists, else returns none      
    let tryGetCellAfter (columnIndex) (row:Row) =
        row
        |> getCells
        |> Seq.tryFind (Cell.getReference >> Reference.toIndices >> fst >> (<=) columnIndex)

    /// Returns the spans of the row
    let getSpan (row:Row) = row.Spans

    /// Sets the spans of the row
    let setSpan spans (row:Row) = 
        row.Spans <- spans
        row

    /// Extends the righ boundary of the spans of the row by the given amount (positive amount increases spans to right and vice versa)
    let extendSpanRight amount row = 
        getSpan row
        |> Spans.extendRight amount
        |> fun s -> setSpan s row

    /// Extends the left boundary of the spans of the row by the given amount (positive amount decreases the spans to left and vice versa)
    let extendSpanLeft amount row = 
        getSpan row
        |> Spans.extendLeft amount
        |> fun s -> setSpan s row

    /// Append cell to the end of the row
    let appendCell (cell:Cell) (row:Row) = 
        row.AppendChild(cell) |> ignore
        row

    /// Creates a row from the given rowIndex, columnSpans and cells
    let create index spans (cells:Cell seq) = 
        Row(childElements = (cells |> Seq.map (fun x -> x :> OpenXmlElement)))
        |> setIndex index
        |> setSpan spans

    /// Removes the cell at the given columnIndex from the row
    let removeCellAt index (row:Row) =
        getCellAt index row
        |> row.RemoveChild
        |> ignore
        row

    /// Removes the cell at the given columnIndex from the row
    let tryRemoveCellAt index (row:Row) =
        tryGetCellAt index row
        |> Option.map (fun cell -> 
            row.RemoveChild(cell) |> ignore
            row)

/// Functions for working with sheetdata (unmanaged: rowindices and cell references do not get automatically updated)
module SheetData = 

    /// Empty SheetData
    let empty = new SheetData()

    /// Inserts a row into the sheetdata before a reference row
    let insertBefore (row:Row) (refRow:Row) (sheetData:SheetData) = 
        sheetData.InsertBefore(row,refRow) |> ignore
        sheetData

    /// Append row to the end of the sheetData
    let appendRow (row:Row) (sheetData:SheetData) = 
        sheetData.AppendChild row |> ignore
        sheetData

    /// Returns a sequence of row contained in the sheetdata
    let getRows (sheetData:SheetData) = 
        sheetData.Descendants<Row>()

    /// Returns the number of rows contained in the sheetdata
    let countRows (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.length
    
    /// Returns row matching or exceeding the given row index if it exists, else returns none      
    let tryGetRowAfter index (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.tryFind (Row.getIndex >> (<=) index)

    /// Returns row with the given rowIndex if it exists, else returns none
    let tryGetRowAt index (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.tryFind (Row.getIndex >> (=) index)

    /// Returns row with the given rowIndex
    let getRowAt index (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.find (Row.getIndex >> (=) index)

    /// Returns true, if the sheetdata contains a row with the given rowindex
    let containsRowAt index (sheetData:SheetData) = 
        getRows sheetData
        |> Seq.exists (Row.getIndex >> (=) index)

    /// Removes the row at the given rowIndex
    let removeRowAt index (sheetData:SheetData) = 
        let r = sheetData |> getRowAt index
        sheetData.RemoveChild(r) |> ignore
        sheetData

    /// Removes the row at the given rowIndex
    let tryRemoveRowAt index (sheetData:SheetData) = 
        sheetData |> tryGetRowAt index
        |> Option.map (fun r ->
            sheetData.RemoveChild(r) |> ignore
            sheetData
        )



/// Part of the Workbook, stores name and other additional info of the sheet (Unmanaged: changing a sheet does not alter the associated worksheet which stores the data)
module Sheet = 
    
    /// Empty Sheet
    let empty = Sheet()

    /// Sets the name of the sheet (This is the name displayed in excel)
    let setName (name:string) (sheet : Sheet) = 
        sheet.Name <- StringValue.FromString name
        sheet

    /// Gets the name of the sheet (This is the name displayed in excel)
    let getName (sheet : Sheet) = sheet.Name.Value

    /// Sets the ID of the sheet (This ID associates the sheet with the worksheet)
    let setID id (sheet : Sheet) = 
        sheet.Id <- StringValue.FromString id
        sheet

    /// Gets the ID of the sheet (This ID associates the sheet with the worksheet)
    let getID (sheet : Sheet) = sheet.Id.Value

    /// Sets the SheetID of the sheet (This ID determines the position of the sheet tab in excel)
    let setSheetID id (sheet : Sheet) = 
        sheet.SheetId <- UInt32Value.FromUInt32 id
        sheet

    /// Gets the SheetID of the sheet (This ID determines the position of the sheet tab in excel)
    let getSheetID (sheet : Sheet) = sheet.SheetId.Value

    /// Create a sheet from the id, the name and the sheetID
    let create id name sheetID = 
        Sheet()
        |> setID id
        |> setName name
        |> setSheetID sheetID

/// Functions for working with Sheets (Unmanaged: changing a sheet does not alter the associated worksheet which stores the data)
module Sheets = 

    /// Empty sheets
    let empty = new Sheets()

    /// Returns the first child sheet of the sheets
    let getFirstSheet (sheets:Sheets) = sheets.GetFirstChild<Sheet>()

    /// Returns the sheets of the sheets
    let getSheets (sheets:Sheets) = sheets.Elements<Sheet>()

    /// Adds a list of sheets to the sheets
    let addSheets (newSheets:Sheet seq) (sheets:Sheets) = 
        newSheets |> Seq.iter (fun sheet -> sheets.Append sheet) 
        sheets

    /// Adds a new sheet to the sheets
    let addSheet (newSheet:Sheet) (sheets:Sheets) = 
        sheets.AppendChild(newSheet) |> ignore
        sheets

    /// Remove the given sheet from the sheets
    let removeSheet (sheetToDelete:Sheet) (sheets:Sheets)  = 
        sheets.RemoveChild(sheetToDelete) |> ignore
        sheets

    /// Returns the sheet for which the predicate returns true (Id Name SheetID -> bool)
    let tryFindSheet (predicate:string -> string -> uint32 -> bool) (sheets:Sheets) =
        getSheets sheets
        |> Seq.tryFind (fun sheet -> predicate sheet.Id.Value sheet.Name.Value sheet.SheetId.Value)

    /// Number of sheets in the sheets
    let countSheets (sheets:Sheets) = getSheets sheets |> Seq.length

    //let mapSheets f (sheets:Sheets) = getSheets sheets |> Seq.toArray |> Array.map f
    //let iterSheets f (sheets:Sheets) = getSheets sheets |> Seq.toArray |> Array.iter f
    //let filterSheets f (sheets:Sheets) = 
    //    getSheets sheets |> Seq.toArray |> Array.filter (f >> not)
    //    |> Array.fold (fun st sheet -> removeSheet sheet st) sheets


/// Functions for working with sharedstringitems
module SharedStringItem = 

    /// Gets the string contained in the sharedstringitem
    let getText (ssi:SharedStringItem) = ssi.InnerText

    /// Sets the string contained in the sharedstringitem
    let setText text (ssi:SharedStringItem) = 
        ssi.Text <- Text(text)
        ssi

    /// Creates a sharedstringitem containing the given string
    let create text = 
        SharedStringItem()
        |> setText text










/// Value based manipulation of sheets. Functions in this module automatically make the necessary adjustments  needed to change the excel sheet the way the function states.
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

    /// Matches the rowSpans to the cellreferences inside the row
    let updateRowSpans (row:Row) : Row=
        let columnIndices =
            Row.getCells row
            |> Seq.map (Cell.getReference >> Reference.toIndices >> fst)
        Row.setSpan (Spans.fromBoundaries (Seq.min columnIndices) (Seq.max columnIndices)) row

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
            if Spans.referenceExceedsSpansToRight newReference spans then
                Row.extendSpanRight 1u row
            else row
        | None -> row

    /// Moves all cells starting with from the given columnindex in the row to the right
    let moveValuesInRowToRight columnIndex (row:Row) : Row=
        row
        |> Row.mapCells (fun c -> 
            let cellCol,cellRow = Cell.getReference c |> Reference.toIndices
            if cellCol >= columnIndex then
                Cell.setReference (Reference.ofIndices (cellCol+1u) cellRow) c
            else c
        )
        |> Row.extendSpanRight 1u
       

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

    /// Returns the index of the last row in the sheet
    let maxRowIndex (sheet) =
        SheetData.getRows sheet
        |> fun s -> 
            if Seq.isEmpty s then 
                0u
            else 
                s
                |> Seq.map (Row.getIndex)
                |> Seq.max

    /// Gets the first sheet in the workbookpart
    let firstSheetOfWorkbookPart (workbookPart) = 
        workbookPart
        |> WorkbookPart.getWorkSheetParts
        |> Seq.head
        |> WorksheetPart.getWorksheet
        |> Worksheet.getSheetData

    /// Appends a new sheet to the excel document
    let addSheetToWorkbookPart (name : string) (data : SheetData) (workbookPart : WorkbookPart) =

        let workbook = WorkbookPart.getOrInitWorkbook  workbookPart

        let worksheetPart = WorkbookPart.initWorksheetPart workbookPart

        WorksheetPart.getWorksheet worksheetPart
        |> Worksheet.addSheetData data
        |> ignore
        
        let sheets = Workbook.getOrInitSheets workbook
        let id = WorkbookPart.getWorksheetPartID worksheetPart workbookPart
        let sheetID = 
            sheets |> Sheets.getSheets |> Seq.map Sheet.getSheetID
            |> fun s -> 
                if Seq.length s = 0 then 1u
                else s |> Seq.max |> (+) 1ul
        let sheet = Sheet.create id name sheetID

        Sheets.addSheet sheet sheets |> ignore
        workbookPart

  
    let createEmptySSTSpreadsheet sheetName (path:string) = 
        let doc = Spreadsheet.createSpreadsheet path
        let workbookPart = Spreadsheet.initWorkbookPart doc

        let sharedStringTablePart = WorkbookPart.getOrInitSharedStringTablePart workbookPart
        SharedStringTable.init sharedStringTablePart |> ignore

        let workbook = WorkbookPart.getOrInitWorkbook workbookPart
        addSheetToWorkbookPart sheetName (SheetData.empty) workbookPart |> ignore
        doc

    /// Sheet manipulation using a shared string table
    module SSTSheets =

        /// Maps a cell to the value string using a shared string table
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
       

        /// Maps the cells of the given row to the value strings using a shared string table
        let getValuesOfRowSST (workbookPart:WorkbookPart) (row:Row) =
            let sst = 
                workbookPart 
                |> WorkbookPart.getSharedStringTablePart 
                |> SharedStringTable.get
            row
            |> Row.getCells
            |> Seq.map (getValueSST sst)
        
        /// Maps the cells of the given row to tuples of 1 based column indices and the value strings using a shared string table
        let getIndexedValuesOfRowSST (workbookPart:WorkbookPart) (row:Row) =
            let sst = 
                workbookPart 
                |> WorkbookPart.getSharedStringTablePart 
                |> SharedStringTable.get
            row
            |> Row.getCells
            |> Seq.map (fun c -> c |> Cell.getReference |> Reference.toIndices |> fst, getValueSST sst c)
        
        /// Gets the string value of the cell at the given 1 based column and row index using a shared string table
        let getCellValueSSTAt (workbookPart:WorkbookPart) (columnIndex : uint32) rowIndex (sheet:SheetData) =
               let sst = 
                   workbookPart 
                   |> WorkbookPart.getSharedStringTablePart 
                   |> SharedStringTable.get
               SheetData.getRowAt rowIndex sheet
               |> Row.getCellAt columnIndex
               |> getValueSST sst

        /// Gets the string values of the row at the given 1 based rowindex using a shared string table
        let getRowValuesSSTAt (workbookPart:WorkbookPart) rowIndex (sheet:SheetData) =
            let sst = 
                workbookPart 
                |> WorkbookPart.getSharedStringTablePart 
                |> SharedStringTable.get
            sheet
            |> SheetData.getRowAt rowIndex
            |> Row.getCells
            |> Seq.map (getValueSST sst)

        /// Gets the string value of the cell at the given 1 based column and row index using a shared string table, if it exists, else returns None
        let tryGetRowValuesSSTAt (workbookPart:WorkbookPart) rowIndex (sheet:SheetData) =
            let sst = 
                workbookPart 
                |> WorkbookPart.getSharedStringTablePart 
                |> SharedStringTable.get
            sheet
            |> SheetData.tryGetRowAt rowIndex
            |> Option.map (
                Row.getCells
                >> Seq.map (getValueSST sst)
            )

        /// Maps the cells of the given row to tuples of 1 based column indices and the value strings using a shared string table, if it exists, else returns none
        let tryGetIndexedRowValuesSSTAt (workbookPart:WorkbookPart) rowIndex (sheet:SheetData) =
            let sst = 
                workbookPart 
                |> WorkbookPart.getSharedStringTablePart 
                |> SharedStringTable.get
            sheet
            |> SheetData.tryGetRowAt rowIndex
            |> Option.map (
                Row.getCells
                >> Seq.map (fun c -> Cell.getReference c |> Reference.toIndices |> fst,getValueSST sst c)
            )

        /// Create a cell using a shared string table 
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

        /// Adds values as a row to the sheet at the given rowindex with the given horizontal offset using a shared string table.
        ///
        /// If a row exists at the given rowindex, shoves it downwards
        let insertRowWithHorizontalOffsetSSTAt (workbookPart) (offset:int) (vals: 'T seq) rowIndex (sheet:SheetData) =
    
            let sst = workbookPart |> WorkbookPart.getSharedStringTablePart |> SharedStringTable.get

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
          
        /// Add a value as a cell to the row at the given columnindex using a shared string table
        ///
        /// If a cell exists at the given columnindex, shoves it to the right
        let insertValueIntoRowSST workbookPart index (value:'T) (row:Row) = 

            let sst = workbookPart |> WorkbookPart.getSharedStringTablePart |> SharedStringTable.get
            let refCell = Row.tryGetCellAfter index row

            let updatedSST,cell = createSSTCell sst index (Row.getIndex row) value

            match refCell with
            | Some ref -> 
                moveValuesInRowToRight index row
                |> Row.insertCellBefore cell ref
            | None ->
                let spans = Row.getSpan row
                let spanExceedance = (uint index) - (spans |> Spans.rightBoundary)
                                   
                Row.extendSpanRight spanExceedance row
                |> Row.appendCell cell

        /// Append the values as a row to the end of the sheet using a shared string table
        let appendRowSST workbookPart (vals: 'T seq) (sheet:SheetData) =
            let i = 
                sheet
                |> maxRowIndex
                |> (+) 1u
            insertRowWithHorizontalOffsetSSTAt workbookPart 0 vals i sheet

        /// Append the value as a cell to the end of the row using a shared string table
        let appendValueToRowSST workbookPart (value:'T) (row:Row) = 
            let sst = workbookPart |> WorkbookPart.getSharedStringTablePart |> SharedStringTable.get
            row
            |> Row.getSpan
            |> Spans.rightBoundary
            |> fun col -> createSSTCell sst (col + 1u) (row |> Row.getIndex) value
            |> fun (newSST,c) -> Row.appendCell c row
            |> Row.extendSpanRight 1u 

        /// Adds values as a row to the sheet at the given rowindex using a shared string table.
        ///
        /// If a row exists at the given rowindex, shoves it downwards
        let insertRowSSTAt workbookpart (vals: 'T seq) rowIndex (sheet:SheetData) =
            insertRowWithHorizontalOffsetSSTAt workbookpart 0 vals rowIndex sheet

        /// Add a value at the given row and columnindex to sheet using a shared string table.
        ///
        /// If a cell exists in the given postion, shoves it to the right
        let insertValueSST workbookPart columnIndex rowIndex (value:'T) (sheet:SheetData) =
            match SheetData.tryGetRowAt rowIndex sheet with
            | Some row -> 
                insertValueIntoRowSST workbookPart columnIndex value row |> ignore
                sheet
            | None -> insertRowWithHorizontalOffsetSSTAt workbookPart (columnIndex - 1u |> int) [value] rowIndex sheet

        /// Removes row
        let removeRowAt workbookPart rowIndex (sheet:SheetData) =
            H.notImplemented()

    /// Sheet manipulation without using a shared string table (directly writing the string into the worksheet file)
    module DirectSheets = 

        /// Gets the string value of the cell at the given 1 based column and row index
        let getCellValueAt columnIndex rowIndex (sheet:SheetData) =
            SheetData.getRowAt rowIndex sheet
            |> Row.getCellAt columnIndex
            |> Cell.getValue
            |> CellValue.getValue
 
        /// Gets the string values of the row at the given 1 based rowindex
        let getRowValuesAt rowIndex (sheet:SheetData) =
            sheet
            |> SheetData.getRowAt rowIndex
            |> Row.getCells
            |> Seq.map (Cell.getValue >> CellValue.getValue)

        /// Gets the string value of the cell at the given 1 based column and row index, if it exists, else returns None
        let tryGetRowValuesAt rowIndex (sheet:SheetData) =
            sheet 
            |> SheetData.tryGetRowAt rowIndex
            |> Option.map (
                Row.getCells
                >> Seq.map (Cell.getValue >> CellValue.getValue)
            )
     
        /// Adds values as a row to the sheet at the given rowindex with the given horizontal offset.
        ///
        /// If a row exists at the given rowindex, shoves it downwards
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

        /// Add a value as a cell to the row at the given columnindex.
        ///
        /// If a cell exists at the given columnindex, shoves it to the right
        let insertValueIntoRow index (value:'T) (row:Row) = 
            let refCell = Row.tryGetCellAfter index row

            let cell = Cell.createGeneric index (Row.getIndex row) value

            match refCell with
            | Some ref -> 
                moveValuesInRowToRight index row
                |> Row.insertCellBefore cell ref
            | None ->
                let spans = Row.getSpan row
                let spanExceedance = (uint index) - (spans |> Spans.rightBoundary)
                               
                Row.extendSpanRight spanExceedance row
                |> Row.appendCell cell
        
        /// Add a value as a cell to the row at the given columnindex.
        ///
        /// If a cell exists at the given columnindex, Overwrites it
        let setValueInRow index (value:'T) (row:Row) = 
            let refCell = Row.tryGetCellAfter index row

            let cell = Cell.createGeneric index (Row.getIndex row) value

            match refCell with
            | Some ref when Cell.getReference ref = Cell.getReference cell ->
                Cell.setType (Cell.getType cell) ref |> ignore
                Cell.setValue ((Cell.getValue cell).Clone() :?> CellValue) ref |> ignore
                row 
            | Some ref -> 
                Row.insertCellBefore cell ref row
            | None ->
                let spans = Row.getSpan row
                let spanExceedance = (uint index) - (spans |> Spans.rightBoundary)
                               
                Row.extendSpanRight spanExceedance row
                |> Row.appendCell cell

        /// Add a value as a cell to the end of the row.
        let appendValueToRow (value:'T) (row:Row) = 
            row
            |> Row.getSpan
            |> Spans.rightBoundary
            |> fun col -> Cell.createGeneric (col + 1u) (row |> Row.getIndex) value
            |> fun c -> Row.appendCell c row
            |> Row.extendSpanRight 1u 

        /// Adds values as a row to the sheet at the given rowindex.
        ///
        /// If a row exists at the given rowindex, shoves it downwards
        let insertRowAt (vals: 'T seq) rowIndex (sheet:SheetData) =
            insertRowWithHorizontalOffsetAt 0 vals rowIndex sheet


        /// Add a value at the given row and columnindex to sheet using a shared string table.
        ///
        /// If a cell exists in the given postion, shoves it to the right
        let insertValue columnIndex rowIndex (value:'T) (sheet:SheetData) =
            match SheetData.tryGetRowAt rowIndex sheet with
            | Some row -> 
                insertValueIntoRow columnIndex value row |> ignore
                sheet
            | None -> insertRowWithHorizontalOffsetAt (columnIndex - 1u |> int) [value] rowIndex sheet

        /// Add a value at the given row and columnindex to sheet using a shared string table.
        ///
        /// If a cell exists in the given postion, overwrites it
        let setValue columnIndex rowIndex (value:'T) (sheet:SheetData) =
            match SheetData.tryGetRowAt rowIndex sheet with
            | Some row -> 
                setValueInRow columnIndex value row |> ignore
                sheet
            | None -> insertRowWithHorizontalOffsetAt (columnIndex - 1u |> int) [value] rowIndex sheet

        /// Append the value as a cell to the end of the row
        let appendValueToRowAt rowIndex (value:'T) (sheet:SheetData) =
            match SheetData.tryGetRowAt rowIndex sheet with
            | Some row -> 
                appendValueToRow value row |> ignore
                sheet
            | None -> insertRowAt [value] rowIndex sheet    

        /// Append the values as a row to the end of the sheet
        let appendRow (vals: 'T seq) (sheet:SheetData) =
            let i = 
                sheet
                |> maxRowIndex
                |> (+) 1u
            insertRowAt vals i sheet
  
        /// Removes the value from the sheet
        let removeValueAt columnIndex rowIndex sheet : SheetData =
            let row = 
                sheet 
                |> SheetData.getRowAt rowIndex 
                |> Row.removeCellAt columnIndex
            if Row.isEmpty row then
                SheetData.removeRowAt rowIndex sheet
            else
                updateRowSpans row |> ignore
                sheet

        /// Removes the value from the sheet
        let tryRemoveValueAt columnIndex rowIndex sheet : SheetData Option=
            let row = 
                sheet 
                |> SheetData.getRowAt rowIndex 
                |> Row.tryRemoveCellAt columnIndex
            row
            |> Option.map (fun row ->
                if Row.isEmpty row then
                    SheetData.removeRowAt rowIndex sheet                   
                else
                    updateRowSpans row |> ignore
                    sheet
            )


        /// Removes row from sheet and move the following rows up
        let removeRowAt rowIndex (sheet:SheetData) : SheetData =
            sheet 
            |> SheetData.removeRowAt rowIndex
            |> SheetData.getRows
            |> Seq.filter (Row.getIndex >> (<) rowIndex)
            |> Seq.fold (fun sheetData row -> 
                moveRowVertical -1 (Row.getIndex row) sheetData           
            ) sheet
