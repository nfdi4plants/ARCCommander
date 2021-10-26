module Cellify

open FSharpSpreadsheetML
open Spectre.Console
open System.Collections.Generic
open XlsxCell
open Change

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



/// Adds an outer MS Excel-like frame to an Array2D-denseMatrix.
let addOuterFrame denseMatrix =
    let rowL = Array2D.length1 denseMatrix + 1
    let colL = Array2D.length2 denseMatrix + 1
    Array2D.init rowL colL (
        fun iR iC ->
            match (iR,iC) with
            | (0,0) -> ""
            | (0,_) -> toExcelLetters (iC)
            | (_,0) -> string iR
            | _     -> denseMatrix.[iR - 1,iC - 1]
    )

/// Takes a Spreadsheet document and returns an array of the names of all the sheets that it consists of.
let getSheetNames doc =
    Spreadsheet.getWorkbookPart doc
    |> Workbook.get
    |> Sheet.Sheets.get
    |> Sheet.Sheets.getSheets
    |> Seq.map Sheet.getName
    |> Array.ofSeq

/// Takes a Spreadsheet document and returns an array of its sheetNames and the Cell matrices it consits of.
let getCellMatrices doc =
    let sst = Spreadsheet.tryGetSharedStringTable doc |> Option.get
    let sheetNames = getSheetNames doc
    sheetNames
    |> Array.map (
        fun sn ->
            let sheetData = Spreadsheet.tryGetSheetBySheetName sn doc |> Option.get
            sn, getCellMatrix sst sheetData
    )

/// Transforms a sparse cell matrix into a dense matrix (2D array).
let getDenseMatrix (cellMatrix : Dictionary<int * int, Cell>) =
    let emptyCell = {
        Content     = Option<_>.None
        TextFormat  = Option<_>.None
        CellFormat  = Option<_>.None
        Comment     = Option<_>.None
        Note        = Option<_>.None
        Formula     = Option<_>.None
    }
    let noOfRows = cellMatrix.Keys |> Seq.map fst |> Seq.max
    let noOfCols = cellMatrix.Keys |> Seq.map snd |> Seq.max
    Array2D.init noOfRows noOfCols (
        fun iR iC ->
            let currkey = (iR + 1, iC + 1)
            if cellMatrix.ContainsKey(currkey) then cellMatrix.[currkey] else emptyCell
    )

/// <summary>Adds a dense matrix (2D array) to a Spectre Table.</summary>
/// <remarks>It is adviced to always use an empty Spectre Table.</remarks>
let addMatrixToTable (denseMatrix : string [,]) (spectreTable : Table) =
    spectreTable.AddColumns(denseMatrix.[0,0 ..]) |> ignore
    for i = 1 to Array2D.length1 denseMatrix - 1 do
        spectreTable.AddRow(denseMatrix.[i,0 ..]) |> ignore

/// Replaces '[' and ']' with '[[' and ']]' for correct Markup-parsing.
let escapeForSquareBrackets (s : string) = (s.Replace("[","[[")).Replace("]","]]")

/// Reads two .xlsx files and prints table representations of their cell differences into the console.
let parse infile1 infile2 =

    let doc1 = Spreadsheet.fromFile infile1 false
    let doc2 = Spreadsheet.fromFile infile2 false
    
    let sheetNames1 = getSheetNames doc1
    let sheetNames2 = getSheetNames doc2

    let allSheetNames = Array.append sheetNames1 sheetNames2 |> Array.distinct
    
    let spectreTables = Array.init allSheetNames.Length (fun _ -> Table())
    
    let cellMatrices1 = getCellMatrices doc1
    let cellMatrices2 = getCellMatrices doc2

    allSheetNames
    |> Seq.iteri (fun i sn ->                    
        match (Array.contains sn sheetNames1, Array.contains sn sheetNames2) with
        | (true,true) ->
            let sheet1 = Array.pick (fun t -> if fst t = sn then Some (snd t) else Option.None) cellMatrices1
            let sheet2 = Array.pick (fun t -> if fst t = sn then Some (snd t) else Option.None) cellMatrices2
            let tit = TableTitle(string sn)
            let chm = getChangeMatrix sheet1 sheet2
            let dm =
                chm
                |> Array2D.map (
                    fun v -> 
                        match v.Changes |> Seq.find (fun t -> fst t = Content) |> snd with
                        | Add -> 
                            (snd v.CellInformation).Content 
                            |> Option.get
                            |> escapeForSquareBrackets
                            |> sprintf "[bold green]%s[/]"
                        | Del -> 
                            (fst v.CellInformation).Content
                            |> Option.get
                            |> escapeForSquareBrackets
                            |> sprintf "[bold red]%s[/]"
                        | Mod -> 
                            (snd v.CellInformation).Content 
                            |> Option.get
                            |> escapeForSquareBrackets
                            |> sprintf "[bold yellow]%s[/]"
                        | No  -> 
                            match (snd v.CellInformation).Content with
                            | Some a -> escapeForSquareBrackets a
                            | Option.None -> ""
                )
                |> addOuterFrame
            addMatrixToTable dm spectreTables.[i]
            spectreTables.[i].Title <- tit
        | (false,true) ->
            let tit = TableTitle(sprintf "%s -- HAS BEEN ADDED" sn)
            let sheet2 = Array.pick (fun t -> if fst t = sn then Some (snd t) else Option.None) cellMatrices2
            spectreTables.[i].Title <- tit
            let dm = 
                getDenseMatrix sheet2 
                |> Array2D.map (fun v -> sprintf "[bold green]%s[/]" (Option.get v.Content |> escapeForSquareBrackets))
                |> addOuterFrame
            addMatrixToTable dm spectreTables.[i]
        | (_,false) -> 
            let tit = TableTitle(sprintf "%s -- HAS BEEN DELETED" sn)
            let sheet1 = Array.pick (fun t -> if fst t = sn then Some (snd t) else Option.None) cellMatrices1
            spectreTables.[i].Title <- tit
            let dm = 
                getDenseMatrix sheet1 
                |> Array2D.map (fun v -> sprintf "[bold red]%s[/]" (Option.get v.Content |> escapeForSquareBrackets))
                |> addOuterFrame
            addMatrixToTable dm spectreTables.[i]
    )

    Spreadsheet.close doc1
    Spreadsheet.close doc2

    spectreTables |> Array.iter AnsiConsole.Render