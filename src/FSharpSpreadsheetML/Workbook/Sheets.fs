namespace FSharpSpreadsheetML

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Spreadsheet
open DocumentFormat.OpenXml.Packaging


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
