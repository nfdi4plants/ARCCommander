namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging

/// Stores data of the sheet and the index of the sheet and
/// functions for working with the worksheetpart (Unmanaged: changing a worksheet does not alter the sheet which links the worksheet to the excel workbook)
module Worksheet = 

    /// Empty Worksheet
    let empty = Worksheet()

    /// Associates a sheetData with the worksheet
    let addSheetData (sheetData:SheetData) (worksheet:Worksheet) = 
        worksheet.AppendChild sheetData |> ignore
        worksheet

    /// Returns true, if the worksheet contains sheetdata
    let hasSheetData (worksheet:Worksheet) = 
        worksheet.HasChildren

    /// Creates a worksheet containing the given sheetdata
    let ofSheetData (sheetData:SheetData) = 
        Worksheet(sheetData)

    /// Returns the sheetdata associated with the worksheet
    let getSheetData (worksheet:Worksheet) = 
        worksheet.GetFirstChild<SheetData>()
      
    //let setSheetData (sheetData:SheetData) (worksheet:Worksheet) = worksheet.sh


    // Returns the worksheet associated with the worksheetpart
    let get (worksheetPart : WorksheetPart) = 
        worksheetPart.Worksheet

    /// Sets the given worksheet with the worksheetpart
    let setWorksheet (worksheet : Worksheet) (worksheetPart : WorksheetPart) = 
        worksheetPart.Worksheet <- worksheet
        worksheetPart

    /// Associates an empty worksheet with the worksheetpart
    let init (worksheetPart:WorksheetPart) = 
        worksheetPart
        |> setWorksheet empty

    /// Returns the existing or a newly created worksheet associated with the worksheetpart
    let getOrInit (worksheetPart:WorksheetPart) =
        if worksheetPart.Worksheet <> null then
            get worksheetPart
        else 
            worksheetPart
            |> init
            |> get

    module WorksheetPart = 

        /// Returns the worksheetpart matching the given id
        let getByID sheetID (workbookPart : WorkbookPart) = 
            workbookPart.GetPartById(sheetID) :?> WorksheetPart  
            
        /// Returns the sheetData associated witht the worksheetpart
        let getSheetData (worksheetPart : WorksheetPart) =
            get worksheetPart |> getSheetData


    //let insertCellData (cell:CellData.CellDataValue) (worksheet : Worksheet) =
        
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



    //let setID id (worksheetPart : WorksheetPart) = notImplemented()
    //let getID (worksheetPart : WorksheetPart) = notImplemented()

