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
