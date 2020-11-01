namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet

open Cell


module internal H = let notImplemented() = failwith "function not yet implemented"
open H


/// Value based manipulation of sheets. Functions in this module automatically make the necessary adjustments  needed to change the excel sheet the way the function states.
module SheetTransformation =
       
    ///If the row with index rowIndex exists in the sheet, moves it downwards by amount. Negative amounts will move the row upwards
    let moveRowVertical (amount:int) rowIndex (sheet:SheetData) = 
        match sheet |> SheetData.tryGetRowAt (rowIndex |> uint32) with
        | Some row -> 
            Row.setIndex (Row.getIndex row |> int64 |> (+) (int64 amount) |> uint32) row
            |> Row.toSeq
            |> Seq.iter (fun cell -> 
                Cell.setReference (Cell.getReference cell |> CellReference.moveVertical amount) cell
                |> ignore            
            )
            sheet
        | None -> 
            printfn "Warning: Row with index %i does not exist" rowIndex
            sheet

    /// Matches the rowSpans to the cellreferences inside the row
    let updateRowSpans (row:Row) : Row=
        let columnIndices =
            Row.toSeq row
            |> Seq.map (Cell.getReference >> CellReference.toIndices >> fst)
        Row.setSpan (Row.Spans.fromBoundaries (Seq.min columnIndices) (Seq.max columnIndices)) row

    ///If a cell with the given columnIndex exists in the row. Moves it one column to the right. 
    ///
    /// If there already was a cell at the new postion, moves that one too. Repeats until a value is moved into a position previously unoccupied.
    let rec shoveValuesInRowToRight columnIndex (row:Row) : Row=
        let spans = Row.getSpan row
        match Row.tryGetCellAt columnIndex row with
        | Some cell ->
            shoveValuesInRowToRight (columnIndex+1u) row |> ignore
            let newReference = (Cell.getReference cell |> CellReference.moveHorizontal 1)            
            Cell.setReference newReference cell |> ignore
            if Row.Spans.referenceExceedsSpansToRight newReference spans then
                Row.extendSpanRight 1u row
            else row
        | None -> row

    /// Moves all cells starting with from the given columnindex in the row to the right
    let moveValuesInRowToRight columnIndex (row:Row) : Row=
        row
        |> Row.mapCells (fun c -> 
            let cellCol,cellRow = Cell.getReference c |> CellReference.toIndices
            if cellCol >= columnIndex then
                Cell.setReference (CellReference.ofIndices (cellCol+1u) cellRow) c
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
            |> Row.toSeq
            |> Seq.iter (fun cell -> 
                Cell.setReference (Cell.getReference cell |> CellReference.moveVertical amount) cell
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
        |> Worksheet.get
        |> Worksheet.getSheetData

    /// Appends a new sheet to the excel document
    let addSheetToWorkbookPart (name : string) (data : SheetData) (workbookPart : WorkbookPart) =

        let workbook = Workbook.getOrInit  workbookPart

        let worksheetPart = WorkbookPart.initWorksheetPart workbookPart

        Worksheet.get worksheetPart
        |> Worksheet.addSheetData data
        |> ignore
        
        let sheets = Sheet.Sheets.getOrInit workbook
        let id = WorkbookPart.getWorksheetPartID worksheetPart workbookPart
        let sheetID = 
            sheets |> Sheet.Sheets.getSheets |> Seq.map Sheet.getSheetID
            |> fun s -> 
                if Seq.length s = 0 then 1u
                else s |> Seq.max |> (+) 1ul
        let sheet = Sheet.create id name sheetID

        sheets.AppendChild(sheet) |> ignore
        workbookPart

  
    let createEmptySSTSpreadsheet sheetName (path:string) = 
        let doc = Spreadsheet.createSpreadsheet path
        let workbookPart = Spreadsheet.initWorkbookPart doc

        let sharedStringTablePart = WorkbookPart.getOrInitSharedStringTablePart workbookPart
        SharedStringTable.init sharedStringTablePart |> ignore

        let workbook = Workbook.getOrInit workbookPart
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
                |> SharedStringTable.SharedStringItem.getText
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
            |> Row.toSeq
            |> Seq.map (getValueSST sst)
        
        /// Maps the cells of the given row to tuples of 1 based column indices and the value strings using a shared string table
        let getIndexedValuesOfRowSST (workbookPart:WorkbookPart) (row:Row) =
            let sst = 
                workbookPart 
                |> WorkbookPart.getSharedStringTablePart 
                |> SharedStringTable.get
            row
            |> Row.toSeq
            |> Seq.map (fun c -> c |> Cell.getReference |> CellReference.toIndices |> fst, getValueSST sst c)
        
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
            |> Row.toSeq
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
                Row.toSeq
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
                Row.toSeq
                >> Seq.map (fun c -> Cell.getReference c |> CellReference.toIndices |> fst,getValueSST sst c)
            )

        /// Create a cell using a shared string table 
        let createSSTCell (sst:SharedStringTable) columnIndex rowIndex  (value:'T) = 
            let value = box value
            match value with
            | :? string as s -> 
                let reference = CellReference.ofIndices columnIndex (rowIndex)
                match SharedStringTable.tryGetIndexByString s sst with
                | Some i -> 
                    sst,Cell.create CellValues.SharedString reference (i |> string |> CellValue.create)
                | None ->
                    let sst = SharedStringTable.SharedStringItem.add (SharedStringTable.SharedStringItem.create s) sst
                    sst, Cell.create CellValues.SharedString reference (SharedStringTable.count sst |> (+) 1u |> string |> CellValue.create)
            | _  -> 
                sst,Cell.createGeneric columnIndex rowIndex (value.ToString())

        /// Adds values as a row to the sheet at the given rowindex with the given horizontal offset using a shared string table.
        ///
        /// If a row exists at the given rowindex, shoves it downwards
        let insertRowWithHorizontalOffsetSSTAt (workbookPart) (offset:int) (vals: 'T seq) rowIndex (sheet:SheetData) =
    
            let sst = workbookPart |> WorkbookPart.getSharedStringTablePart |> SharedStringTable.get

            let uiO = uint32 offset
            let spans = Row.Spans.fromBoundaries (uiO + 1u) (Seq.length vals |> uint32 |> (+) uiO )
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
                let spanExceedance = (uint index) - (spans |> Row.Spans.rightBoundary)
                                   
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
            |> Row.Spans.rightBoundary
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
            |> Row.toSeq
            |> Seq.map (Cell.getValue >> CellValue.getValue)

        /// Gets the string value of the cell at the given 1 based column and row index, if it exists, else returns None
        let tryGetRowValuesAt rowIndex (sheet:SheetData) =
            sheet 
            |> SheetData.tryGetRowAt rowIndex
            |> Option.map (
                Row.toSeq
                >> Seq.map (Cell.getValue >> CellValue.getValue)
            )
     
        /// Adds values as a row to the sheet at the given rowindex with the given horizontal offset.
        ///
        /// If a row exists at the given rowindex, shoves it downwards
        let insertRowWithHorizontalOffsetAt (offset:int) (vals: 'T seq) rowIndex (sheet:SheetData) =
        
            let uiO = uint32 offset
            let spans = Row.Spans.fromBoundaries (uiO + 1u) (Seq.length vals |> uint32 |> (+) uiO )
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
                let spanExceedance = (uint index) - (spans |> Row.Spans.rightBoundary)
                               
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
                let spanExceedance = (uint index) - (spans |> Row.Spans.rightBoundary)
                               
                Row.extendSpanRight spanExceedance row
                |> Row.appendCell cell

        /// Add a value as a cell to the end of the row.
        let appendValueToRow (value:'T) (row:Row) = 
            row
            |> Row.getSpan
            |> Row.Spans.rightBoundary
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
