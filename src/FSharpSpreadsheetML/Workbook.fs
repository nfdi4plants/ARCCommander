namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging

/// Functions for manipulating the workbook (Unmanaged: changing the sheets does not alter the associated worksheets which store the data)
module Workbook =

    /// Empty workbook
    let empty = new Workbook()
    
    /// Gets the sheets of the workbook
    let getSheets (workbook:Workbook) = workbook.Sheets

    /// Returns true, if the workbook contains a sheets element
    let isNotNull (workbook:Workbook) = 
        workbook.Sheets <> null

    /// Add an empty sheets elemtent to the workboot
    let initSheets (workbook:Workbook) = 
        workbook.AppendChild<Sheets>(Sheets()) |> ignore
        workbook

    /// Returns the existing or a newly created sheets associated with the worksheet
    let getOrInitSheets (workbook:Workbook) =
        if isNotNull workbook then
            getSheets workbook
        else 
            initSheets workbook
            |> getSheets

    /// Returns the existing or a newly created shetts associated with the worksheet
    let ofWorkbookPart (workbookPart:WorkbookPart) = workbookPart.Workbook 






/// Functions for manipulating the workbookpart (Unmanaged: changing the sheets does not alter the associated worksheets which store the data and vice versa)
module WorkbookPart = 

    /// Gets the workbook of the workbookpart
    let getWorkbook (workbookPart:WorkbookPart) = workbookPart.Workbook 

    /// Sets the workbook of the workbookpart
    let setWorkbook (workbook:Workbook) (workbookPart:WorkbookPart) = 
        workbookPart.Workbook <- workbook
        workbookPart

    /// Set an empty workbook
    let initWorkbook (workbookPart:WorkbookPart) = 
        setWorkbook (Workbook()) workbookPart

    /// Returns true, if the workbookpart contains a workbook
    let containsWorkbook (workbookPart:WorkbookPart) = workbookPart.Workbook <> null  

    /// Returns the existing or a newly created workbook associated with the workbookpart
    let getOrInitWorkbook (workbookPart:WorkbookPart) =
        if containsWorkbook workbookPart then
            getWorkbook workbookPart
        else 
            initWorkbook workbookPart
            |> getWorkbook

    /// Add a worksheetpart to the workbookpart
    let addWorksheetPart (worksheetPart : WorksheetPart) (workbookPart:WorkbookPart) = workbookPart.AddPart(worksheetPart)

    /// Add a empty worksheetpart to the workbookpart
    let initWorksheetPart (workbookPart:WorkbookPart) = workbookPart.AddNewPart<WorksheetPart>()

    /// Get the worksheetparts of the workbookpart
    let getWorkSheetParts (workbookPart:WorkbookPart) = workbookPart.WorksheetParts

    /// Returns true, if the workbookpart contains at least one worksheetpart
    let containsWorkSheetParts (workbookPart:WorkbookPart) = workbookPart.GetPartsOfType<WorksheetPart>() |> Seq.length |> (<>) 0

    /// Gets the worksheetpart of the workbookpart with the given id
    let getWorksheetPartById (id:string) (workbookPart:WorkbookPart) = workbookPart.GetPartById(id) :?> WorksheetPart 

    /// If the workbookpart contains the worksheetpart with the given id, returns it. Else returns none
    let tryGetWorksheetPartById (id:string) (workbookPart:WorkbookPart) = 
        try workbookPart.GetPartById(id) :?> WorksheetPart  |> Some with
        | _ -> None

    /// Gets the ID of the worksheetpart of the workbookpart
    let getWorksheetPartID (worksheetPart:WorksheetPart) (workbookPart:WorkbookPart) = workbookPart.GetIdOfPart worksheetPart
    //let addworkSheet (workbookPart:WorkbookPart) (worksheet : Worksheet) = 
    //    let emptySheet = (addNewWorksheetPart workbookPart)
    //    emptySheet.Worksheet <- worksheet

    /// Gets the sharedstringtablepart
    let getSharedStringTablePart (workbookPart:WorkbookPart) = workbookPart.SharedStringTablePart

    /// Sets an empty sharedstringtablepart
    let initSharedStringTablePart (workbookPart:WorkbookPart) = 
        workbookPart.AddNewPart<SharedStringTablePart>() |> ignore
        workbookPart

    /// Returns true, if the workbookpart contains a sharedstringtablepart
    let containsSharedStringTablePart (workbookPart:WorkbookPart) = workbookPart.SharedStringTablePart <> null

    /// Returns the existing or a newly created sharedstringtablepart associated with the workbookpart
    let getOrInitSharedStringTablePart (workbookPart:WorkbookPart) =
        if containsSharedStringTablePart workbookPart then
            getSharedStringTablePart workbookPart
        else 
            initSharedStringTablePart workbookPart
            |> getSharedStringTablePart

