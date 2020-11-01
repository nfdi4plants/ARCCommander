namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging

/// Stores data of the sheet and the index of the sheet
module Worksheet = 

    /// Empty Worksheet
    let empty = Worksheet()



    /// Associates a sheetData with the worksheet
    let addSheetData (sheetData:SheetData) (worksheet:Worksheet) = 
        worksheet.AppendChild sheetData |> ignore
        worksheet

    /// Returns true, if the worksheet contains sheetdata
    let containsSheetData (worksheet:Worksheet) = worksheet.HasChildren

    /// Creates a worksheet containing the given sheetdata
    let ofSheetData (sheetData:SheetData) = Worksheet(sheetData)

    /// Returns the sheetdata associated with the worksheet
    let getSheetData (worksheet:Worksheet) = worksheet.GetFirstChild<SheetData>()
      
    //let setSheetData (sheetData:SheetData) (worksheet:Worksheet) = worksheet.sh


    ///Convenience

    //let insertRow (rowIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let overWriteRow (rowIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let appendRow (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let getRow (rowIndex) (worksheet:Worksheet) = notImplemented()
    //let deleteRow rowIndex (worksheet:Worksheet) = notImplemented()

    //let insertColumn (columnIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let overWriteColumn (columnIndex) (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let appendColumn (values: 'T seq) (worksheet:Worksheet) = notImplemented()
    //let getColumn (columnIndex) (worksheet:Worksheet) = notImplemented()
    //let deleteColumn (columnIndex) (worksheet:Worksheet) = notImplemented()

    ////let setCellValue (rowIndex,columnIndex) value (worksheet:Worksheet) = notImplemented()
    //let setCellValue adress value (worksheet:Worksheet) = notImplemented()
    //let inferCellValue adress (worksheet:Worksheet) = notImplemented()
    //let deleteCellValue adress (worksheet:Worksheet) = notImplemented()

/// Functions for working with the worksheetpart (Unmanaged: changing a worksheet does not alter the sheet which links the worksheet to the excel workbook)
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
        setWorksheet (Worksheet.empty) worksheetPart

    /// Returns true, if the worksheetpart contains a worksheet
    let containsWorksheet (worksheetPart:WorksheetPart) = worksheetPart.Worksheet <> null  

    /// Returns the existing or a newly created worksheet associated with the worksheetpart
    let getOrInit (worksheetPart:WorksheetPart) =
        if containsWorksheet worksheetPart then
            getWorksheet worksheetPart
        else 
            initWorksheet worksheetPart
            |> getWorksheet

    //let setID id (worksheetPart : WorksheetPart) = notImplemented()
    //let getID (worksheetPart : WorksheetPart) = notImplemented()

