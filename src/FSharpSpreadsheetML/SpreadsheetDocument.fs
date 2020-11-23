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

    /// Returns a sequence of rows containing the cells for the given sheetIndex of the given spreadsheetDocument. 
    /// Returns an empty list if the sheet of the given sheetIndex does not exist.
    let getRowsBySheetIndex (sheetIndex:uint) (spreadsheetDocument:SpreadsheetDocument) =

        match (Sheet.tryItem sheetIndex spreadsheetDocument) with
        | Some (sheet) ->
            let workbookPart = spreadsheetDocument.WorkbookPart
            let worksheetPart = workbookPart.GetPartById(sheet.Id.Value) :?> WorksheetPart      
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
        | None -> seq {[]} :?> seq<Row>

    /// Returns a 1D sequence of cells for the given sheetIndex of the given spreadsheetDocument. 
    /// Returns an empty list if the sheet of the given sheetIndex does not exist.
    let getCellsBySheetIndex (sheetIndex:uint) (spreadsheetDocument:SpreadsheetDocument) =

        match (Sheet.tryItem sheetIndex spreadsheetDocument) with
        | Some (sheet) ->
            let workbookPart = spreadsheetDocument.WorkbookPart
            let worksheetPart = workbookPart.GetPartById(sheet.Id.Value) :?> WorksheetPart      
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

    /// Returns the sheet for which the predicate returns true (Id Name SheetID -> bool)
    let tryFindSheet (predicate:string -> string -> uint32 -> bool) (spreadsheetDocument:SpreadsheetDocument) =
        let sheets = spreadsheetDocument.WorkbookPart.Workbook.Sheets
        Sheets.getSheets sheets
        |> Seq.tryFind (fun sheet -> predicate sheet.Id.Value sheet.Name.Value sheet.SheetId.Value)

    /// Count the number of sheets
    let countSheets (spreadsheetDocument:SpreadsheetDocument) =
        let sheets = spreadsheetDocument.WorkbookPart.Workbook.Sheets
        Sheets.getSheets sheets |> Seq.length


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






