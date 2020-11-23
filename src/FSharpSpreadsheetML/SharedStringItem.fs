namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml

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
