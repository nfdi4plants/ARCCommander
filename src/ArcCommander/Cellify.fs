module Cellify

open FSharpSpreadsheetML
open Spectre.Console
open System.Collections.Generic

type ChangeType =
| Add
| Del
| Mod

/// Takes two cells and compares if their values differentiates.
let hasChange cell1 cell2 = cell1 <> cell2

/// Takes two cells and get the type of change between them.
let getChangeType cell1 (cell2 : string) =
    match (cell1,cell2) with
    | (c1,c2) when c1 = "" && c2.Length > 0 -> Add
    | (c1,c2) when c1.Length > 0 && c2 = "" -> Del
    | _                                     -> Mod

/// Takes the coordinates of two cells as well as their values, checks for changes between them and, if present, gives a tuple of the cell coordinates and the change type.
let getChange cellC (cellV1 : string) (cellV2 : string) =
    if hasChange cellV1 cellV2 then Some (cellC, getChangeType cellV1 cellV2)
    else None

let getChanges (cellMatrix1 : Dictionary<(int * int), string>) (cellMatrix2 : Dictionary<(int * int), string>) =
    [for k in cellMatrix1 do
        getChange cellMatrix2.[k.Key]
    ]





/// Converts numbers to letters like the column keys in MS Excel.
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

// besser erst später, für die changes weiterhin sparse matrix benutzen
/// Transforms a sparse matrix (as a dictionary) into a dense one (realized as a 2D array).
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