namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging

module WorksheetPart =

    // Returns the worksheet associated with the worksheetpart
    let getWorksheet (worksheetPart : WorksheetPart) = 
        worksheetPart.Worksheet
    
    /// Sets the given worksheet with the worksheetpart
    let setWorksheet (worksheet : Worksheet) (worksheetPart : WorksheetPart) = 
        worksheetPart.Worksheet <- worksheet
        worksheetPart
    
    /// Associates an empty worksheet with the worksheetpart
    let initWorksheet (worksheetPart:WorksheetPart) = 
        worksheetPart
        |> setWorksheet Worksheet.empty
    
    /// Returns the existing or a newly created worksheet associated with the worksheetpart
    let getOrInitWorksheet (worksheetPart:WorksheetPart) =
        if worksheetPart.Worksheet <> null then
            getWorksheet worksheetPart
        else 
            worksheetPart
            |> initWorksheet
            |> getWorksheet