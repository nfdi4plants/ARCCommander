namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging

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

    /// Returns the existing or a newly created workbook associated with the workbookpart
    let getOrInitWorkbook (workbookPart:WorkbookPart) =
        if workbookPart.Workbook <> null then
            getWorkbook workbookPart
        else 
            workbookPart
            |> initWorkbook
            |> getWorkbook

    /// Add a worksheetpart to the workbookpart
    let addWorksheetPart (worksheetPart : WorksheetPart) (workbookPart:WorkbookPart) = 
        workbookPart.AddPart(worksheetPart)

    /// Add a empty worksheetpart to the workbookpart
    let initWorksheetPart (workbookPart:WorkbookPart) = workbookPart.AddNewPart<WorksheetPart>()

    /// Get the worksheetparts of the workbookpart
    let getWorkSheetParts (workbookPart:WorkbookPart) = workbookPart.WorksheetParts

    /// Returns true, if the workbookpart contains at least one worksheetpart
    let containsWorkSheetParts (workbookPart:WorkbookPart) = workbookPart.GetPartsOfType<WorksheetPart>() |> Seq.length |> (<>) 0

    /// Gets the worksheetpart of the workbookpart with the given id
    let getWorksheetPartByRelationshipId (id:string) (workbookPart:WorkbookPart) = workbookPart.GetPartById(id) :?> WorksheetPart 

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
    
    let getSharedStringTable (workbookPart:WorkbookPart) =
        workbookPart 
        |> getSharedStringTablePart 
        |> SharedStringTable.get

    /// Returns the data of the first sheet of the given workbookpart
    let getDataOfFirstSheet (workbookPart:WorkbookPart) = 
        workbookPart
        |> getWorkSheetParts
        |> Seq.head
        |> WorksheetPart.getWorksheet
        |> Worksheet.getSheetData

    /// Appends a new sheet with the given sheet data to the excel document
    // to-do: guard if sheet of name already exists
    let appendSheet (sheetName : string) (data : SheetData) (workbookPart : WorkbookPart) =

        let workbook = getOrInitWorkbook workbookPart 

        let worksheetPart = initWorksheetPart workbookPart

        WorksheetPart.getOrInitWorksheet worksheetPart
        |> Worksheet.addSheetData data
        |> ignore
        
        let sheets = Workbook.getOrInitSheets workbook
        let id = getWorksheetPartID worksheetPart workbookPart
        let sheetID = 
            sheets |> Sheets.getSheets |> Seq.map Sheet.getSheetID
            |> fun s -> 
                if Seq.length s = 0 then 1u
                else s |> Seq.max |> (+) 1ul

        let sheet = Sheet.create id sheetName sheetID

        sheets.AppendChild(sheet) |> ignore
        workbookPart
