module Cellify

open FSharpSpreadsheetML
open Spectre.Console
open System.Collections.Generic
open ExcelCell
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
            | _ -> denseMatrix.[iR - 1,iC - 1]
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


/// Reads an .xlsx file and prints the coordinates and values of the cells of its sheets.
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
            chm
            |> Array2D.map (
                fun v -> 
                    match v.Changes |> Seq.find (fun t -> fst t = Content) |> snd with
                    | Add -> v.CellInformation.Content
                    | Del -> ""
                    | 
            )
            spectreTables.[i].Title <- tit
            //let sheet = Worksheet.getSheetData wsp.Worksheet
            //SheetData.toSparseValueMatrix (Option.get sst) sheet
            //|> transformSparseMatrixToDense
            //|> addOuterFrame
            //|> fun fullMatrix ->
            //    let tt = TableTitle(sheetName)
            //    spectreTable.Title <- tt
            //    spectreTable.AddColumns(fullMatrix.[0,0 ..]) |> ignore
            //    for i = 1 to Array2D.length1 fullMatrix - 1 do
            //        spectreTable.AddRow(fullMatrix.[i,0 ..]) |> ignore
        | (false,true) ->
            let tit = TableTitle(sprintf "%s -- HAS BEEN ADDED" sn)

            spectreTables.[i].Title <- tit
        | (_,false) -> 
            let tit = TableTitle(sprintf "%s -- HAS BEEN DELETED" sn)
            spectreTables.[i].Title <- tit
    )

    Spreadsheet.close doc1
    Spreadsheet.close doc2

    spectreTables |> Array.iter AnsiConsole.Render

let main () =
    let argsNo = System.Environment.GetCommandLineArgs()
    parse (Array.last argsNo)