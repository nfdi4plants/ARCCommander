namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml


/// Functions for working with sharedstringtables
module SharedStringTable = 

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
            new SharedStringItem(new DocumentFormat.OpenXml.Spreadsheet.Text(text))

        /// Adds the sharedstringitem to the sharedstringtable
        let add (sharedStringItem:SharedStringItem) (sst:SharedStringTable) = 
            sst.Append(sharedStringItem.CloneNode(false) :?> SharedStringItem)
            sst

    /// Empty sharedstringtable
    let empty = SharedStringTable() 

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

    /// Returns the sharedstringitems contained in the sharedstringtable
    let toSeq (sst:SharedStringTable) : seq<SharedStringItem> = 
        sst.Elements<SharedStringItem>()
    
    /// Returns the index of the String, or the invers of the element count
    let getIndexByString (text:string) (sst:SharedStringTable) = 
        let rec loop (en:System.Collections.Generic.IEnumerator<OpenXmlElement>) i =
            match en.MoveNext() with
            | true -> match (en.Current.InnerText = text) with
                      | true -> i
                      | false -> loop en (i+1)
            | false -> ~~~i // invers count
            
        loop (sst.GetEnumerator()) 0

    /// If the string is contained in the sharedstringtable, contains the index of its position
    let tryGetIndexByString (s:string) (sst:SharedStringTable) = 
        toSeq sst 
        |> Seq.tryFindIndex (fun x -> x.Text.Text = s)

    /// Returns the sharedstringitem at the given index
    let getText i (sst:SharedStringTable) = 
        toSeq sst
        |> Seq.item i

 

    /// Number of sharedstringitems in the sharedstringtable
    let count (sst:SharedStringTable) = 
        if sst.Count.HasValue then sst.Count.Value else 0u

    /// Appends the SharedStringItem to the end of the SharedStringTable
    let append (sharedStringItem:SharedStringItem) (sharedStringTable:SharedStringTable) = 
        sharedStringTable.AppendChild(sharedStringItem) |> ignore
        sharedStringTable.Save()
        sharedStringTable

    /// Inserts text into the SharedStringTable. If the item already exists, returns its index.
    let insertText (text:string) (sharedStringTable:SharedStringTable) = 
        let index = getIndexByString text sharedStringTable
        if index < 0 then 
            let ssi = SharedStringItem.create text
            append ssi sharedStringTable |> ignore
            ~~~index
        else
            index
            
            



