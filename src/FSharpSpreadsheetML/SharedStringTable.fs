namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging

/// Functions for working with sharedstringtables
module SharedStringTable = 

    /// Empty sharedstringtable
    let empty = SharedStringTable() 

    /// Returns the sharedstringitems contained in the sharedstringtable
    let getItems (sst:SharedStringTable) = sst.Elements<SharedStringItem>()

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
    let count (sst:SharedStringTable) = sst.Count.Value


/// Functions for working with sharedstringtableparts
module SharedStringTablePart = 
    
    /// Sets an empty sharedstringtable
    let initSharedStringTable (sstPart:SharedStringTablePart) = 
        sstPart.SharedStringTable <- SharedStringTable.empty
        sstPart

    /// Gets the sharedstringtable of the sharedstringtablepart
    let getSharedStringTable (sstPart:SharedStringTablePart) = sstPart.SharedStringTable

    /// Sets the sharedstringtable of the sharedstringtablepart
    let setSharedStringTable (sst:SharedStringTable) (sstPart:SharedStringTablePart) = 
        sstPart.SharedStringTable <- sst
        sstPart


