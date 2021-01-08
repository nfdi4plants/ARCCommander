namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet



/// Functions for working the spreadsheet document
module Spreadsheet = 

    /// Opens the spreadsheet located at the given path
    let fromFile (path:string) isEditable = SpreadsheetDocument.Open(path,isEditable)

    /// Initializes a new empty spreadsheet at the given path
    let init (path:string) = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook)

    // Gets the workbookpart of the spreadsheet
    let getWorkbookPart (spreadsheet:SpreadsheetDocument) = spreadsheet.WorkbookPart

    // Only if none there
    let initWorkbookPart (spreadsheet:SpreadsheetDocument) = spreadsheet.AddWorkbookPart()

    /// Save changes made to the spreadsheet
    let saveChanges (spreadsheet:SpreadsheetDocument) = 
        spreadsheet.Save() 
        spreadsheet

    /// Closes the stream to the spreadsheet
    let close (spreadsheet:SpreadsheetDocument) = spreadsheet.Close()

    /// Save changes made to the spreadsheet to the given path
    let saveAs path (spreadsheet:SpreadsheetDocument) = 
        spreadsheet.SaveAs(path) :?> SpreadsheetDocument
        |> close
        spreadsheet

    /// Initializes a new empty spreadsheet at the given path
    let initWithSST sheetName (path:string) = 
        let doc = init path
        let workbookPart = initWorkbookPart doc

        let sharedStringTablePart = WorkbookPart.getOrInitSharedStringTablePart workbookPart
        SharedStringTable.init sharedStringTablePart |> ignore

        WorkbookPart.appendSheet sheetName (SheetData.empty) workbookPart |> ignore
        doc

    // Get the SharedStringTablePart. If it does not exist, create a new one.
    let getOrInitSharedStringTablePart (spreadsheetDocument:SpreadsheetDocument) =
        let workbookPart = spreadsheetDocument.WorkbookPart    
        let sstp = workbookPart.GetPartsOfType<SharedStringTablePart>()
        match sstp |> Seq.tryHead with
        | Some sst -> sst
        | None -> workbookPart.AddNewPart<SharedStringTablePart>()

    /// Returns the worksheetPart associated to the sheet with the given name
    let tryGetWorksheetPartBySheetName (name:string) (spreadsheetDocument:SpreadsheetDocument) =
        Sheet.tryItemByName name spreadsheetDocument
        |> Option.map (fun sheet -> 
            spreadsheetDocument.WorkbookPart
            |> Worksheet.WorksheetPart.getByID sheet.Id.Value 
        )      

    /// Returns the sheetData for the given 0 based sheetIndex of the given spreadsheetDocument. 
    let tryGetSheetBySheetIndex (sheetIndex:uint) (spreadsheetDocument:SpreadsheetDocument) =
        Sheet.tryItem sheetIndex spreadsheetDocument
        |> Option.map (fun sheet -> 
            spreadsheetDocument.WorkbookPart
            |> Worksheet.WorksheetPart.getByID sheet.Id.Value 
            |> Worksheet.get
            |> Worksheet.getSheetData
        )        

    /// Returns a sequence of rows containing the cells for the given 0 based sheetIndex of the given spreadsheetDocument. 
    let getRowsBySheetIndex (sheetIndex:uint) (spreadsheetDocument:SpreadsheetDocument) =

        match (Sheet.tryItem sheetIndex spreadsheetDocument) with
        | Some (sheet) ->
            let workbookPart = spreadsheetDocument.WorkbookPart
            let worksheetPart = Worksheet.WorksheetPart.getByID sheet.Id.Value workbookPart     
            let stringTablePart = getOrInitSharedStringTablePart spreadsheetDocument
            seq {
            use reader = OpenXmlReader.Create(worksheetPart)
      
            while reader.Read() do
                if (reader.ElementType = typeof<Row>) then 
                    let row = reader.LoadCurrentElement() :?> Row
                    row.Elements()
                    |> Seq.iter (fun item -> 
                        let cell = item :?> Cell
                        Cell.includeSharedStringValue stringTablePart.SharedStringTable cell |> ignore
                        )
                    yield row 
            }
        | None -> Seq.empty

    /// Returns a 1D sequence of cells for the given sheetIndex of the given spreadsheetDocument. 
    let getCellsBySheetIndex (sheetIndex:uint) (spreadsheetDocument:SpreadsheetDocument) =

        match (Sheet.tryItem sheetIndex spreadsheetDocument) with
        | Some (sheet) ->
            let workbookPart = spreadsheetDocument.WorkbookPart
            let worksheetPart = Worksheet.WorksheetPart.getByID sheet.Id.Value workbookPart
            let stringTablePart = getOrInitSharedStringTablePart spreadsheetDocument
            seq {
            use reader = OpenXmlReader.Create(worksheetPart)
        
            while reader.Read() do
                if (reader.ElementType = typeof<Cell>) then 
                    let cell    = reader.LoadCurrentElement() :?> Cell 
                    let cellRef = if cell.CellReference.HasValue then cell.CellReference.Value else ""
                    yield Cell.includeSharedStringValue stringTablePart.SharedStringTable cell
            }
        | None -> seq {()}

    //----------------------------------------------------------------------------------------------------------------------
    //                                      High level functions                                                            
    //----------------------------------------------------------------------------------------------------------------------

    //Rows

    let mapRowOfSheet (sheetId) (rowId) (rowF: Row -> Row) : SpreadsheetDocument = 
        //get workbook part
        //get sheet data by sheetId
        //get row at rowId
        //apply rowF to row and update 
        //return updated doc
        raise (System.NotImplementedException())

    let mapRowsOfSheet (sheetId) (rowF: Row -> Row) : SpreadsheetDocument = raise (System.NotImplementedException())

    let appendRowValuesToSheet (sheetId) (rowValues: seq<'T>) : SpreadsheetDocument = raise (System.NotImplementedException())

    let insertRowValuesIntoSheetAt (sheetId) (rowId) (rowValues: seq<'T>) : SpreadsheetDocument = raise (System.NotImplementedException())

    let insertValueIntoSheetAt (sheetId) (rowId) (colId) (value: 'T) : SpreadsheetDocument = raise (System.NotImplementedException())

    let setValueInSheetAt (sheetId) (rowId) (colId) (value: 'T) : SpreadsheetDocument = raise (System.NotImplementedException())

    let deleteRowFromSheet (sheetId) (rowId) : SpreadsheetDocument = raise (System.NotImplementedException())

    //...






