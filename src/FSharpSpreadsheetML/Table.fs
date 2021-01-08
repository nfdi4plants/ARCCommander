namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml


/// Functions for working with tables 
module Table =
    
    //  Helper functions for working with "A1:A1" style table areas
    /// The areas marks the area in which the table lies. 
    module Area =

        /// Given A1 based column start and end indices, returns a "A1:A1" style area
        let fromBoundaries fromCellReference toCellReference = 
            sprintf "%s:%s" fromCellReference toCellReference
            |> StringValue.FromString

        /// Given a "A1:A1" style area , returns A1 based cell start and end cellReferences
        let toBoundaries (area:StringValue) = 
            area.Value.Split ':'
            |> fun a -> a.[0], a.[1]

        /// Gets the right boundary of the area
        let rightBoundary (area:StringValue) = 
            toBoundaries area
            |> snd
            |> CellReference.toIndices
            |> fst

        /// Gets the left boundary of the area
        let leftBoundary (area:StringValue) = 
            toBoundaries area
            |> fst
            |> CellReference.toIndices
            |> snd

        /// Gets the Upper boundary of the area
        let upperBoundary (area:StringValue) = 
            toBoundaries area
            |> fst
            |> CellReference.toIndices
            |> fst

        /// Gets the lower boundary of the area
        let lowerBoundary (area:StringValue) = 
            toBoundaries area
            |> snd
            |> CellReference.toIndices
            |> snd

        /// Moves both start and end of the area by the given amount (positive amount moves area to right and vice versa)
        let moveHorizontal amount (area:StringValue) =
            area
            |> toBoundaries
            |> fun (f,t) -> CellReference.moveHorizontal amount f, CellReference.moveHorizontal amount t
            ||> fromBoundaries

        /// Moves both start and end of the area by the given amount (positive amount moves area to right and vice versa)
        let moveVertical amount (area:StringValue) =
            area
            |> toBoundaries
            |> fun (f,t) -> CellReference.moveHorizontal amount f, CellReference.moveHorizontal amount t
            ||> fromBoundaries

        /// Extends the righ boundary of the area by the given amount (positive amount increases area to right and vice versa)
        let extendRight amount (area:StringValue) =
            area
            |> toBoundaries
            |> fun (f,t) -> f, CellReference.moveHorizontal amount t
            ||> fromBoundaries

        /// Extends the left boundary of the area by the given amount (positive amount decreases the area to left and vice versa)
        let extendLeft amount (area:StringValue) =
            area
            |> toBoundaries
            |> fun (f,t) -> CellReference.moveHorizontal amount f, t
            ||> fromBoundaries

        /// Returns true if the column index of the reference exceeds the right boundary of the area
        let referenceExceedsAreaRight reference area = 
            (reference |> CellReference.toIndices |> fst) 
                > (area |> rightBoundary)
        
        /// Returns true if the column index of the reference exceeds the left boundary of the area
        let referenceExceedsAreaLeft reference area = 
            (reference |> CellReference.toIndices |> fst) 
                < (area |> leftBoundary)  
     
        /// Returns true if the column index of the reference exceeds the upper boundary of the area
        let referenceExceedsAreaAbove reference area = 
            (reference |> CellReference.toIndices |> snd) 
                > (area |> upperBoundary)
        
        /// Returns true if the column index of the reference exceeds the lower boundary of the area
        let referenceExceedsAreaBelow reference area = 
            (reference |> CellReference.toIndices |> snd) 
                < (area |> lowerBoundary )  

        /// Returns true if the reference does not lie in the boundary of the area
        let referenceExceedsArea reference area = 
            referenceExceedsAreaRight reference area
            ||
            referenceExceedsAreaLeft reference area
            ||
            referenceExceedsAreaAbove reference area
            ||
            referenceExceedsAreaBelow reference area
 

    module TableColumns = 

        /// Gets the tableColumns from a table
        let get (table : Table) =
            table.TableColumns

        /// Gets the columns from a tableColumns element
        let getTableColumns (tableColumns:TableColumns) =
            tableColumns.Elements<TableColumn>()

        /// Retruns the number of columns in a tableColumns element
        let count (tableColumns:TableColumns) =
            getTableColumns tableColumns
            |> Seq.length

    module TableColumn =
        
        /// Gets Name of TableColumn
        let getName (tableColumn:TableColumn) =
            tableColumn.Name.Value

        /// Gets 1 based column index of TableColumn
        let getId (tableColumn:TableColumn) =
            tableColumn.Id.Value

    /// If a table exists, for which the predicate applied to its name returns true, gets it
    let tryGetByNameBy (predicate : string -> bool) (worksheetPart : WorksheetPart) =
        worksheetPart.TableDefinitionParts
        |> Seq.tryPick (fun t -> if predicate t.Table.Name.Value then Some t.Table else None)

    /// Gets the name of the table
    let getName (table:Table) =
        table.Name.Value

    /// Sets the name of the table
    let setName (name:string) (table:Table) =
        table.Name <- (StringValue(name))
        table

    /// Gets the area of the table
    let getArea (table:Table) =
        table.AutoFilter.Reference

    /// Sets the area of the table
    let setArea area (table:Table) =
        table.AutoFilter.Reference <- area

    /// Returns the headers of the columns
    let getColumnHeaders (table:Table) = 
        table.TableColumns
        |> TableColumns.getTableColumns
        |> Seq.map (TableColumn.getName)

    /// Returns the tableColumn for which the predicate returns true
    let tryGetTableColumnBy (predicate : TableColumn -> bool) (table:Table) =
        table.TableColumns
        |> TableColumns.getTableColumns
        |> Seq.tryFind predicate

    /// If a tableColumn with the given name exists in the table, returns it
    let tryGetTableColumnByName name (table:Table) =
        table.TableColumns
        |> TableColumns.getTableColumns
        |> Seq.tryFind (TableColumn.getName >> (=) name)

    /// If a column with the given header exists in the table, returns its values
    let tryGetColumnValuesByColumnHeaderWithSST sst sheetData columnHeader (table:Table) =
        let area = getArea table
        table.TableColumns
        |> TableColumns.getTableColumns
        |> Seq.tryFindIndex (TableColumn.getName >> (=) columnHeader)
        |> Option.map (fun i ->
            let columnIndex =  (Area.leftBoundary area) + (uint i)
            [Area.upperBoundary area .. Area.lowerBoundary area]
            |> List.choose (fun r -> SheetData.tryGetCellValueWithSSTAt sst r columnIndex sheetData)           
        )
        
    /// Reads a complete table. Values are stored sparsely in a dictionary, with the key being a column header and row index tuple
    let toSparseValueMatrixWithSST sst sheetData (table:Table) =
        let area = getArea table
        let dictionary = System.Collections.Generic.Dictionary<string*int,string>()
        [Area.leftBoundary area .. Area.rightBoundary area]
        |> List.iter (fun c ->
            let upperBoundary = Area.upperBoundary area
            let lowerBoundary = Area.lowerBoundary area
            let header = SheetData.tryGetCellValueWithSSTAt sst upperBoundary c sheetData |> Option.get
            List.init (lowerBoundary - upperBoundary |> int |> (+) -1) (fun i ->
                let r = uint i + upperBoundary + 1u
                match SheetData.tryGetCellValueWithSSTAt sst r c sheetData with
                | Some v -> dictionary.Add((header,i),v)
                | None -> ()                              
            )
            |> ignore
        )
        dictionary
        