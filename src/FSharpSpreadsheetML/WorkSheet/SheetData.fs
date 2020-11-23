namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet

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

    //----------------------------------------------------------------------------------------------------------------------
    //                                              Row(s)                                                                  
    //----------------------------------------------------------------------------------------------------------------------

    /// Returns a sequence of row contained in the sheetdata
    let getRows (sheetData:SheetData) : seq<Row>= 
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

    ///If the row with index rowIndex exists in the sheet, moves it downwards by amount. Negative amounts will move the row upwards
    let moveRowVertical (amount:int) rowIndex (sheetData:SheetData) = 
        match sheetData |> tryGetRowAt rowIndex with
        | Some row -> 
            let shift = (int64 (Row.getIndex row)) + (int64 amount)
            
            row
            |> Row.setIndex (uint32 shift)
            |> Row.toCellSeq
            |> Seq.iter (fun cell -> 
                cell
                |> Cell.setReference (
                    Cell.getReference cell 
                    |> CellReference.moveVertical amount
                ) 
                |> ignore            
            )
            sheetData
        | None -> 
            printfn "Warning: Row with index %i does not exist" rowIndex
            sheetData
    
    ///If a row with the given rowIndex exists in the sheet, moves it one position downwards. 
    ///
    /// If there already was a row at the new postion, moves that one too. Repeats until a row is moved into a position previously unoccupied.
    let rec moveRowBlockDownward rowIndex (sheetData:SheetData) =
        if sheetData |> containsRowAt rowIndex then             
            sheetData
            |> moveRowBlockDownward (rowIndex+1u)
            |> moveRowVertical 1 rowIndex
        else
            sheetData

    /// Returns the index of the last row in the sheet
    let getMaxRowIndex (sheetData:SheetData) =
        getRows sheetData
        |> fun s -> 
            if Seq.isEmpty s then 
                0u
            else 
                s
                |> Seq.map (Row.getIndex)
                |> Seq.max

    
    /// Gets the string value of the cell at the given 1 based column and row index, if it exists, else returns None
    let tryGetRowValuesAt rowIndex (sheet:SheetData) =
        sheet 
        |> tryGetRowAt rowIndex
        |> Option.map (
            Row.toCellSeq
            >> Seq.map (Cell.getValue)
        )

    /// Gets the string value of the cell at the given 1 based column and row index using a shared string table, if it exists, else returns None
    let tryGetRowValuesWithSSTAt (sharedStringTable:SharedStringTable) rowIndex (sheet:SheetData) =
        sheet
        |> tryGetRowAt rowIndex
        |> Option.map (
            Row.toCellSeq
            >> Seq.map (Cell.getValueWithSST sharedStringTable)
        )

    /// Gets the string values of the row at the given 1 based rowindex
    let getRowValuesAt rowIndex (sheet:SheetData) =
        sheet
        |> getRowAt rowIndex
        |> Row.toCellSeq
        |> Seq.map (Cell.getValue)

    /// Gets the string values of the row at the given 1 based rowindex using a shared string table
    let getRowValuesWithSSTAt (sharedStringTable:SharedStringTable) rowIndex (sheet:SheetData) =
        sheet
        |> getRowAt rowIndex
        |> Row.toCellSeq
        |> Seq.map (Cell.getValueWithSST sharedStringTable)

    /// Maps the cells of the given row to tuples of 1 based column indices and the value strings using a shared string table, if it exists, else returns none
    let tryGetIndexedRowValuesWithSSTAt (sharedStringTable:SharedStringTable) rowIndex (sheet:SheetData) =
        sheet
        |> tryGetRowAt rowIndex
        |> Option.map (
            Row.toCellSeq
            >> Seq.map (fun cell -> 
                cell
                |> Cell.getReference  
                |> CellReference.toIndices 
                |> fst,

                cell |> Cell.getValueWithSST sharedStringTable)
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
        let refRow = tryGetRowAfter (uint rowIndex) sheet
        match refRow with
        | Some ref -> 
            sheet
            |> moveRowBlockDownward rowIndex 
            |> insertBefore newRow ref
        | None ->
            appendRow newRow sheet

    /// Adds values as a row to the sheet at the given rowindex with the given horizontal offset using a shared string table.
    ///
    /// If a row exists at the given rowindex, shoves it downwards
    let insertRowValuesWithHorizontalOffsetWithSSTAt (sharedStringTable:SharedStringTable) (offset:int) (vals: 'T seq) rowIndex (sheet:SheetData) =
    
        let uiO = uint32 offset
        let spans = Row.Spans.fromBoundaries (uiO + 1u) ((Seq.length vals |> uint32) + uiO )
        let newRow = 
            vals
            |> Seq.mapi (fun i value -> 
                value
                |> Cell.createWithSST sharedStringTable ((int64 i) + 1L + (int64 offset) |> uint32) rowIndex
                |> fun r -> r.CloneNode(true) :?> Cell
            )
            |> Row.create (uint32 rowIndex) spans
            |> fun r -> r.CloneNode(true) :?> Row
        let refRow = tryGetRowAfter (uint rowIndex) sheet
        match refRow with
        | Some ref -> 
            sheet
            |> moveRowBlockDownward rowIndex
            |> insertBefore newRow ref
        | None ->
            appendRow newRow sheet
  
    /// Adds values as a row to the sheet at the given rowindex.
    ///
    /// If a row exists at the given rowindex, shoves it downwards
    let insertRowValuesAt (vals: 'T seq) rowIndex (sheet:SheetData) =
        insertRowWithHorizontalOffsetAt 0 vals rowIndex sheet

    /// Adds values as a row to the sheet at the given rowindex using a shared string table.
    ///
    /// If a row exists at the given rowindex, shoves it downwards
    let insertRowValuesWithSSTAt (sharedStringTable:SharedStringTable) (vals: 'T seq) rowIndex (sheet:SheetData) =
        insertRowValuesWithHorizontalOffsetWithSSTAt sharedStringTable 0 vals rowIndex sheet
 
    /// Append the values as a row to the end of the sheet
    let appendRowValues (vals: 'T seq) (sheet:SheetData) =
        let i = (getMaxRowIndex sheet) + 1u
        insertRowValuesAt vals i sheet
  
    /// Append the values as a row to the end of the sheet using a shared string table
    let appendRowValuesWithSST workbookPart (vals: 'T seq) (sheet:SheetData) =
        let i = (getMaxRowIndex sheet) + 1u
        insertRowValuesWithHorizontalOffsetWithSSTAt workbookPart 0 vals i sheet

    /// Append the value as a cell to the end of the row
    // To-Do: Add version using a SharedStringTable
    let appendValueToRowAt rowIndex (value:'T) (sheet:SheetData) =
        match tryGetRowAt rowIndex sheet with
        | Some row -> 
            row
            |> Row.appendValue value
            |> ignore
            sheet
        | None -> insertRowValuesAt [value] rowIndex sheet    

    /// Removes row from sheet and move the following rows up
    // To-Do: Add version using a SharedStringTable
    let deleteRowAt rowIndex (sheet:SheetData) : SheetData =
        sheet 
        |> removeRowAt rowIndex
        |> getRows
        |> Seq.filter (Row.getIndex >> (<) rowIndex)
        |> Seq.fold (fun sheetData row -> 
            moveRowVertical -1 (Row.getIndex row) sheetData           
        ) sheet


//----------------------------------------------------------------------------------------------------------------------
//                                              Cell(s)                                                                 
//----------------------------------------------------------------------------------------------------------------------

    let getCellAt (rowIndex: uint32) (columnIndex : uint32) (sheetData:SheetData) = 
        sheetData
        |> getRowAt rowIndex 
        |> Row.getCellAt columnIndex

    let getCellValueAt (rowIndex: uint32) (columnIndex : uint32) (sheetData:SheetData) = 
        sheetData 
        |> getCellAt rowIndex columnIndex
        |> Cell.getValue

    /// Gets the string value of the cell at the given 1 based column and row index using a shared string table
    let getCellValueWithSSTAt (sharedStringTable:SharedStringTable) (rowIndex: uint32) (columnIndex : uint32) (sheetData:SheetData) =
           sheetData
           |> getCellAt rowIndex columnIndex
           |> Cell.getValueWithSST sharedStringTable

    /// Add a value at the given row and columnindex to sheet using a shared string table.
    ///
    /// If a cell exists in the given postion, shoves it to the right
    let insertValueAt rowIndex columnIndex (value:'T) (sheet:SheetData) =
        match tryGetRowAt rowIndex sheet with
        | Some row -> 
            row
            |> Row.insertValue columnIndex value  
            |> ignore
            sheet
        | None -> insertRowWithHorizontalOffsetAt (columnIndex - 1u |> int) [value] rowIndex sheet

    /// Add a value at the given row and columnindex to sheet using a shared string table.
    ///
    /// If a cell exists in the given postion, shoves it to the right
    let insertValueWithSSTAt (sharedStringTable:SharedStringTable) rowIndex columnIndex  (value:'T) (sheet:SheetData) =
        match tryGetRowAt rowIndex sheet with
        | Some row -> 
            row 
            |> Row.insertValueWithSSTAt sharedStringTable columnIndex value 
            |> ignore
            sheet
        | None -> insertRowValuesWithHorizontalOffsetWithSSTAt sharedStringTable (columnIndex - 1u |> int) [value] rowIndex sheet

    /// Add a value at the given row and columnindex to sheet using a shared string table.
    ///
    /// If a cell exists in the given postion, overwrites it
    // To-Do: Add version using a SharedStringTable
    let setValueAt rowIndex columnIndex (value:'T) (sheet:SheetData) =
        match tryGetRowAt rowIndex sheet with
        | Some row -> 
            row 
            |> Row.setValue columnIndex value 
            |> ignore
            sheet
        | None -> insertRowWithHorizontalOffsetAt (columnIndex - 1u |> int) [value] rowIndex sheet

    /// Removes the value from the sheet
    let tryRemoveValueAt rowIndex columnIndex sheet : SheetData Option=
        let row = 
            sheet 
            |> getRowAt rowIndex 
            |> Row.tryRemoveCellAt columnIndex
        row
        |> Option.map (fun row ->
            if Row.isEmpty row then
                sheet |> removeRowAt rowIndex
            else
                Row.updateRowSpan row |> ignore
                sheet
        )

    /// Removes the value from the sheet
    let removeValueAt rowIndex columnIndex sheet : SheetData =
        let row = 
            sheet 
            |> getRowAt rowIndex 
            |> Row.removeCellAt columnIndex
        if Row.isEmpty row then
            sheet |> removeRowAt rowIndex
        else
            Row.updateRowSpan row |> ignore
            sheet