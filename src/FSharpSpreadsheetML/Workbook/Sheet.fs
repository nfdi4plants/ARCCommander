namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging


/// Part of the Workbook, stores name and other additional info of the sheet (Unmanaged: changing a sheet does not alter the associated worksheet which stores the data)
module Sheet = 

    /// Empty Sheet
    let empty = Sheet()

    /// Sets the name of the sheet (This is the name displayed in excel)
    let setName (name:string) (sheet : Sheet) = 
        sheet.Name <- StringValue.FromString name
        sheet

    /// Gets the name of the sheet (This is the name displayed in excel)
    let getName (sheet : Sheet) = sheet.Name.Value

    /// Sets the ID of the sheet (This ID associates the sheet with the worksheet)
    let setID id (sheet : Sheet) = 
        sheet.Id <- StringValue.FromString id
        sheet

    /// Gets the ID of the sheet (This ID associates the sheet with the worksheet)
    let getID (sheet : Sheet) = sheet.Id.Value

    /// Sets the SheetID of the sheet (This ID determines the position of the sheet tab in excel)
    let setSheetID id (sheet : Sheet) = 
        sheet.SheetId <- UInt32Value.FromUInt32 id
        sheet

    /// Gets the SheetID of the sheet (This ID determines the position of the sheet tab in excel)
    let getSheetID (sheet : Sheet) = sheet.SheetId.Value

    /// Create a sheet from the id, the name and the sheetID
    let create id name sheetID = 
        Sheet()
        |> setID id
        |> setName name
        |> setSheetID sheetID



