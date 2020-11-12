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