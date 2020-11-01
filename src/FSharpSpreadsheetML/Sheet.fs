namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging


/// Part of the Workbook, stores name and other additional info of the sheet (Unmanaged: changing a sheet does not alter the associated worksheet which stores the data)
module Sheet = 
    
    /// Functions for working with Sheets (Unmanaged: changing a sheet does not alter the associated worksheet which stores the data)
    module Sheets = 

        /// Empty sheets
        let empty = new Sheets()

        /// Returns the first child sheet of the sheets
        let getFirstSheet (sheets:Sheets) = sheets.GetFirstChild<Sheet>()

        /// Returns the sheets of the sheets
        let getSheets (sheets:Sheets) = sheets.Elements<Sheet>()

        /// Adds a list of sheets to the sheets
        let addSheets (newSheets:Sheet seq) (sheets:Sheets) = 
            newSheets |> Seq.iter (fun sheet -> sheets.Append sheet) 
            sheets


        //let mapSheets f (sheets:Sheets) = getSheets sheets |> Seq.toArray |> Array.map f
        //let iterSheets f (sheets:Sheets) = getSheets sheets |> Seq.toArray |> Array.iter f
        //let filterSheets f (sheets:Sheets) = 
        //    getSheets sheets |> Seq.toArray |> Array.filter (f >> not)
        //    |> Array.fold (fun st sheet -> removeSheet sheet st) sheets


        /// Gets the sheets of the workbook
        let get (workbook:Workbook) = workbook.Sheets

        /// Add an empty sheets elemtent to the workboot
        let init (workbook:Workbook) = 
            workbook.AppendChild<Sheets>(Sheets()) |> ignore
            workbook        

        /// Returns the existing or a newly created sheets associated with the worksheet
        let getOrInit (workbook:Workbook) =
            if  workbook.Sheets <> null then
                get workbook
            else 
                workbook
                |> init
                |> get        


    
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


    let tryItem (index:uint) (spreadsheetDocument:SpreadsheetDocument) : option<Sheet> = 
        let workbookPart = spreadsheetDocument.WorkbookPart    
        workbookPart.Workbook.Descendants<Sheet>()
        |> Seq.tryItem (int index) 


    let tryItemByName (name:string) (spreadsheetDocument:SpreadsheetDocument) : option<Sheet> = 
        let workbookPart = spreadsheetDocument.WorkbookPart    
        workbookPart.Workbook.Descendants<Sheet>()
        |> Seq.tryFind (fun s -> s.Name.HasValue && s.Name.Value = name)


    /// Adds a new sheet to spreadsheet document
    let add (spreadsheetDocument:SpreadsheetDocument) (sheet:Sheet) = 
        let sheets = spreadsheetDocument.WorkbookPart.Workbook.Sheets
        sheets.AppendChild(sheet) |> ignore
        spreadsheetDocument

    /// Remove the given sheet from the sheets
    let remove (spreadsheetDocument:SpreadsheetDocument) (sheet:Sheet) =
        let sheets = spreadsheetDocument.WorkbookPart.Workbook.Sheets
        sheets.RemoveChild(sheet) |> ignore
        spreadsheetDocument

    /// Returns the sheet for which the predicate returns true (Id Name SheetID -> bool)
    let tryFind (predicate:string -> string -> uint32 -> bool) (spreadsheetDocument:SpreadsheetDocument) =
        let sheets = spreadsheetDocument.WorkbookPart.Workbook.Sheets
        Sheets.getSheets sheets
        |> Seq.tryFind (fun sheet -> predicate sheet.Id.Value sheet.Name.Value sheet.SheetId.Value)

    /// Count the number of sheets
    let count (spreadsheetDocument:SpreadsheetDocument) =
        let sheets = spreadsheetDocument.WorkbookPart.Workbook.Sheets
        Sheets.getSheets sheets |> Seq.length


