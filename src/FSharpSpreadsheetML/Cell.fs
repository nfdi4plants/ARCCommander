namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Spreadsheet


/// Functions for creating and manipulating cells
module Cell =


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
        let reference = CellReference.ofIndices columnIndex (rowIndex)
        create valType reference (CellValue.create value)
    
    /// Create a cell using a shared string table, also returns the updated shared string table.
    let createWithSST (sharedStringTable: SharedStringTable) columnIndex rowIndex (value:'T) = 
        let value = box value
        match value with
        | :? string as s -> 
            let reference = CellReference.ofIndices columnIndex (rowIndex)
            match SharedStringTable.tryGetIndexByString s sharedStringTable with
            | Some i -> 
                i
                |> string
                |> CellValue.create
                |> create CellValues.SharedString reference
            | None ->
                let updatedSharedStringTable = 
                    sharedStringTable
                    |> SharedStringTable.SharedStringItem.add (SharedStringTable.SharedStringItem.create s) 

                updatedSharedStringTable
                |> SharedStringTable.count
                |> string
                |> CellValue.create
                |> create CellValues.SharedString reference 
        | _  -> 
           createGeneric columnIndex rowIndex (value.ToString())

    /// Gets "A1"-Style cell reference
    let getReference (cell:Cell) = cell.CellReference.Value

    /// Sets "A1"-Style cell reference
    let setReference (reference) (cell:Cell) = 
        cell.CellReference <- StringValue.FromString reference
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

    /// Gets Some cellValue if cellValue is existent. Else returns None
    let tryGetValue (cell:Cell) = 
        if cell.CellValue <> null then
            Some cell.CellValue
        else
            None

    /// Gets cellValue
    let getValue (cell:Cell) = cell.CellValue
    
    /// Maps a cell to the value string using a shared string table
    let tryGetValueWithSST (sharedStringTable:SharedStringTable) (cell:Cell) =
        try 
            match cell |> tryGetType with
            | Some (CellValues.SharedString) ->
    
                let sharedStringTableIndex = 
                    cell
                    |> getValue
                    |> CellValue.getValue
                    |> int
    
                sharedStringTable
                |> SharedStringTable.getText sharedStringTableIndex
                |> SharedStringTable.SharedStringItem.getText
            | _ ->
                cell
                |> getValue
                |> CellValue.getValue   
            |> Some
        with
        | _ -> None



    /// Maps a cell to the value string using a shared string table
    let getValueWithSST (sharedStringTable:SharedStringTable) (cell:Cell) =
        match cell |> tryGetType with
        | Some (CellValues.SharedString) ->

            let sharedStringTableIndex = 
                cell
                |> getValue
                |> CellValue.getValue
                |> int

            sharedStringTable
            |> SharedStringTable.getText sharedStringTableIndex
            |> SharedStringTable.SharedStringItem.getText
        | _ ->
            cell
            |> getValue
            |> CellValue.getValue   

    /// Sets cellValue
    let setValue (value:CellValue) (cell:Cell) = 
        cell.CellValue <- value
        cell

    /// Includes value from SharedStringTable in Cell.CellValue.Text
    let includeSharedStringValue (sharedStringTable:SharedStringTable) (cell:Cell) =
        if not (isNull cell.DataType) then  
            match cell |> tryGetType with
            | Some (CellValues.SharedString) ->
                let index = int cell.InnerText
                match sharedStringTable |> Seq.tryItem index with 
                | Some value -> 
                    cell.DataType <- EnumValue(CellValues.String)
                    cell.CellValue.Text <- value.InnerText
                | None ->
                    cell.CellValue.Text <- cell.InnerText
                cell  

            | _ -> cell
        else        
            cell.CellValue.Text <- cell.InnerText
            cell

