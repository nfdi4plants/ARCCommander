namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging

/// Functions for working with sharedstringtables
module SharedStringTable = 

    /// Empty sharedstringtable
    let empty = SharedStringTable() 

    /// Returns the sharedstringitems contained in the sharedstringtable
    let getItems (sst:SharedStringTable) = 
        sst.Elements<SharedStringItem>()

    /// If the string is contained in the sharedstringtable, contains the index of its position
    let tryGetIndexByString (s:string) (sst:SharedStringTable) = 
        getItems sst 
        |> Seq.tryFindIndex (fun x -> x.Text.Text = s)

    /// Returns the sharedstringitem at the given index
    let getText i (sst:SharedStringTable) = 
        getItems sst
        |> Seq.item i

    /// Adds the sharedstringitem to the sharedstringtable
    let addItem (sharedStringItem:SharedStringItem) (sst:SharedStringTable) = 
        sst.Append(sharedStringItem.CloneNode(false) :?> SharedStringItem)
        sst

    /// Number of sharedstringitems in the sharedstringtable
    let count (sst:SharedStringTable) = 
        if sst.Count.HasValue then sst.Count.Value else 0u


    /// Sets an empty sharedstringtable
    let init (sstPart:SharedStringTablePart) = 
        sstPart.SharedStringTable <- empty
        sstPart

    /// Gets the sharedstringtable of the sharedstringtablepart
    let get (sstPart:SharedStringTablePart) = 
        sstPart.SharedStringTable

    /// Sets the sharedstringtable of the sharedstringtablepart
    let set (sst:SharedStringTable) (sstPart:SharedStringTablePart) = 
        sstPart.SharedStringTable <- sst
        sstPart


