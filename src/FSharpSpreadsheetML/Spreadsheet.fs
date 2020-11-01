namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet



/// Functions for working the spreadsheet document
module Spreadsheet = 

    /// Opens the spreadsheet located at the given path
    let fromFile (path:string) isEditable = SpreadsheetDocument.Open(path,isEditable)

    /// Creates a new spreadsheet at the given path
    let createSpreadsheet (path:string) = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook)

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

    
    // Get the SharedStringTablePart. If it does not exist, create a new one.
    let getOrInitSharedStringTablePart (spreadsheetDocument:SpreadsheetDocument) =
        let workbookPart = spreadsheetDocument.WorkbookPart    
        let sstp = workbookPart.GetPartsOfType<SharedStringTablePart>()
        match sstp |> Seq.tryHead with
        | Some sst -> sst
        | None -> workbookPart.AddNewPart<SharedStringTablePart>()
            





    type Seq =

        static member fromSpreadsheet (spreadsheetDocument:SpreadsheetDocument,?sheetIndex:uint) =
            let sheetIndex' = defaultArg sheetIndex 0u
            match (Sheet.tryItem sheetIndex' spreadsheetDocument) with
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


        static member fromSpreadsheetbyRow (spreadsheetDocument:SpreadsheetDocument,?sheetIndex:uint) =
            //let worksheetPart = sheet.Value.Parent.Parent.Ancestors<Worksheet>()
            let sheetIndex' = defaultArg sheetIndex 0u
            match (Sheet.tryItem sheetIndex' spreadsheetDocument) with
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



