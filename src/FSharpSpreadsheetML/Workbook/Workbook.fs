namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging

/// Functions for manipulating the workbook (Unmanaged: changing the sheets does not alter the associated worksheets which store the data)
module Workbook =

    /// Empty workbook
    let empty = new Workbook()

    /// Gets the sheets of the workbook
    let getSheets (workbook:Workbook) = workbook.Sheets

    /// Add an empty sheets elemtent to the workboot
    let initSheets (workbook:Workbook) = 
        workbook.AppendChild<Sheets>(Sheets()) |> ignore
        workbook        

    /// Returns the existing or a newly created sheets associated with the worksheet
    let getOrInitSheets (workbook:Workbook) =
        if workbook.Sheets <> null then
            getSheets workbook
        else 
            workbook
            |> initSheets
            |> getSheets

    //// Adds sheet to workbook
    //let addSheet (sheet : Sheet) (workbook:Workbook) =
    //    let sheets = Sheet.Sheets.getOrInit workbook
    //    Sheet.Sheets.addSheet sheet sheets |> ignore
    //    workbook