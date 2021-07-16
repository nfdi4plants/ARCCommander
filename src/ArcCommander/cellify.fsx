#r "nuget: FSharpSpreadsheetML"

#nowarn "NU1605"

open FSharpSpreadsheetML

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

    let res =
        sheetNames
        |> Seq.iter (fun sheetName ->                    
            match Spreadsheet.tryGetWorksheetPartBySheetName sheetName doc with
            | Some wsp ->
                let sheet = Worksheet.getSheetData wsp.Worksheet
                printfn "[%s]" sheetName
                printfn "================================="
                SheetData.getRows sheet
                |> Seq.iter (fun row -> 
                    row 
                    |> Row.toCellSeq
                    |> Seq.iter (fun c -> printfn "%s : %s" c.CellReference.Value (Cell.getValue sst c))
                    )
            
                printfn "================================="
            | None -> ()    
        )

    Spreadsheet.close doc

    res

let main () =
    let argsNo = System.Environment.GetCommandLineArgs()
    parse (Array.last argsNo)

main ()