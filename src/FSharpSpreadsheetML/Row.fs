namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml


/// Functions for working with rows (unmanaged: spans and cell references do not get automatically updated)
module Row =
    
    //  Helper functions for working with "1:1" style row spans
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
            (reference |> CellReference.toIndices |> fst) 
                > (spans |> rightBoundary)
        
        /// Returns true if the column index of the reference is exceeds the left boundary of the spans
        let referenceExceedsSpansToLeft reference spans = 
            (reference |> CellReference.toIndices |> fst) 
                < (spans |> leftBoundary)  
     
        /// Returns true if the column index of the reference does not lie in the boundary of the spans
        let referenceExceedsSpans reference spans = 
            referenceExceedsSpansToRight reference spans
            ||
            referenceExceedsSpansToLeft reference spans


    /// Empty Row
    let empty = Row()

    /// Returns a sequence of cells contained in the row
    let toSeq (row:Row) : seq<Cell> = row.Descendants<Cell>() 

    /// Returns true,if the row contains no cells
    let isEmpty (row:Row)= toSeq row |> Seq.length |> (=) 0
    
    let mapCells (f : Cell -> Cell) (row:Row) = 
        row
        |> toSeq
        |> Seq.iter (f >> ignore)
        row

    //let iterCells (f) (row:Row) = notImplemented()

    /// Returns the first cell in the row for which the predicate returns true
    let findCell (predicate : Cell -> bool) (row:Row) =
        toSeq row
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
        |> toSeq
        |> Seq.exists (Cell.getReference >> CellReference.toIndices >> fst >> (=) columnIndex)

    /// Returns cell with the given columnIndex
    let getCellAt (columnIndex) (row:Row) =
        row
        |> toSeq
        |> Seq.find (Cell.getReference >> CellReference.toIndices >> fst >> (=) columnIndex)

    /// Returns cell with the given columnIndex if it exists, else returns none
    let tryGetCellAt (columnIndex) (row:Row) =
        row
        |> toSeq
        |> Seq.tryFind (Cell.getReference >> CellReference.toIndices >> fst >> (=) columnIndex)

    /// Returns cell matching or exceeding the given column index if it exists, else returns none      
    let tryGetCellAfter (columnIndex) (row:Row) =
        row
        |> toSeq
        |> Seq.tryFind (Cell.getReference >> CellReference.toIndices >> fst >> (<=) columnIndex)

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

