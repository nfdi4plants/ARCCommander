module Cellify

open FSharpSpreadsheetML
open Spectre.Console

let toExcelLetters number =
    if int number < 1 then failwith "ERROR: Only numbers > 0 can be converted to Excel letters."
    let rec loop no list =
        if no > 26. then
            loop 
                (if no % 26. = 0. then no / 26. - 0.1 else no / 26.) 
                (if no % 26. = 0. then 'Z'::list else (no % 26. + 64. |> char)::list)
        else
            if no % 26. = 0. then 'Z'::list else (no % 26. + 64. |> char)::list
    loop (float number) []
    |> System.String.Concat

let transformSparseMatrixToDense (dict : System.Collections.Generic.Dictionary<int * int, string>) =
    let noOfCols = dict.Keys |> Seq.map fst |> Seq.max
    let noOfRows = dict.Keys |> Seq.map snd |> Seq.max
    Array2D.init noOfRows noOfCols (
        fun iR iC ->
            match dict.TryGetValue((iC + 1,iR + 1)) with
            | (true,v) -> v
            | (false,_) -> ""
    )

/// Adds an outer Excel-like frame to an Array2D-denseMatrix.
let addOuterFrame denseMatrix =
    let rowL = Array2D.length1 denseMatrix + 1
    let colL = Array2D.length2 denseMatrix + 1
    Array2D.init rowL colL (
        fun iR iC ->
            match (iR,iC) with
            | (0,0) -> ""
            | (0,_) -> toExcelLetters (iC)
            | (_,0) -> string iR
            | _ -> denseMatrix.[iR - 1,iC - 1]
    )


/// Reads an .xlsx file and prints the coordinates and values of the cells of its sheets.
let parse infile =

    let doc = Spreadsheet.fromFile infile false
    
    let sst = Spreadsheet.tryGetSharedStringTable doc

    let sheetNames = 
        Spreadsheet.getWorkbookPart doc
        |> Workbook.get
        |> Sheet.Sheets.get
        |> Sheet.Sheets.getSheets
        |> Seq.map Sheet.getName
        |> Array.ofSeq

    //let spectreTables = Array.init sheetNames.Length (fun _ -> new Table())
    let spectreTable = new Table()
    
    let res =
        sheetNames
        |> Seq.iteri (fun i sheetName ->                    
            match Spreadsheet.tryGetWorksheetPartBySheetName sheetName doc with
            | Some wsp ->
                let sheet = Worksheet.getSheetData wsp.Worksheet
                SheetData.toSparseValueMatrix (Option.get sst) sheet
                |> transformSparseMatrixToDense
                |> addOuterFrame
                |> fun fullMatrix ->
                    let tt = TableTitle(sheetName)
                    //spectreTables.[i].Title <- tt
                    //spectreTables.[i].AddColumns(fullMatrix.[0,0 ..]) |> ignore
                    //for i = 1 to Array2D.length1 fullMatrix - 1 do
                    //    spectreTables.[i].AddRow(fullMatrix.[i,0 ..]) |> ignore
                    spectreTable.Title <- tt
                    spectreTable.AddColumns(fullMatrix.[0,0 ..]) |> ignore
                    for i = 1 to Array2D.length1 fullMatrix - 1 do
                        spectreTable.AddRow(fullMatrix.[i,0 ..]) |> ignore
            | None -> ()
        )

    Spreadsheet.close doc

    AnsiConsole.Render(spectreTable)

let main () =
    let argsNo = System.Environment.GetCommandLineArgs()
    parse (Array.last argsNo)