namespace ISA_XLSX.IO

open System.Collections.Generic

open ISA

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet
open FSharpSpreadsheetML


module ISA_Sheet  = 
   
    let findIndexOfKey workbookPart key sheet = 
        sheet
        |> SheetData.getRows
        |> Seq.find (
            SheetTransformation.SSTSheets.getValuesOfRowSST workbookPart
            >> Seq.head
            >> (=) key
        )
        |> Row.getIndex

    let tryFindIndexOfKey workbookPart key sheet = 
        sheet
        |> SheetData.getRows
        |> Seq.tryFind (
            SheetTransformation.SSTSheets.getValuesOfRowSST workbookPart
            >> Seq.head
            >> (=) key
        )
        |> Option.map Row.getIndex

    let keyExists workbookPart key sheet = 
        tryFindIndexOfKey workbookPart key sheet 
        |> Option.isSome   

    module SingleTrait = 
        
        let getKeyValueAt workbookPart rowIndex sheet : KeyValuePair<string,string> = 
            SheetTransformation.SSTSheets.getRowValuesSSTAt workbookPart rowIndex sheet
            |> fun s -> 
                KeyValuePair.Create(Seq.item 0 s, Seq.item 1 s)

        let tryGetKeyValueAt workbookPart rowIndex sheet: KeyValuePair<string,string> Option = 
            SheetTransformation.SSTSheets.tryGetRowValuesSSTAt workbookPart rowIndex sheet
            |> Option.map (
                fun s -> KeyValuePair.Create(Seq.item 0 s, Seq.item 1 s)
            )

        let findIndexOfKeyValue workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.find (fun r ->
                SheetTransformation.SSTSheets.getValuesOfRowSST workbookPart r
                |> fun s -> 
                    Seq.head s = kv.Key
                    &&
                    Seq.item 1 s = kv.Value
            )
            |> Row.getIndex

        let tryFindIndexOfKeyValue workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.tryFind (fun r ->
                SheetTransformation.SSTSheets.getValuesOfRowSST workbookPart r
                |> fun s -> 
                    Seq.head s = kv.Key
                    &&
                    Seq.item 1 s = kv.Value
            )
            |> Option.map Row.getIndex

        let keyValueExists workbookPart (kv:KeyValuePair<string,string>) sheet = 
            tryFindIndexOfKeyValue workbookPart kv sheet 
            |> Option.isSome   

    module MultiTrait = 
         
        let getKeyValuesAt workbookPart rowIndex sheet : KeyValuePair<string,string seq> = 
            SheetTransformation.SSTSheets.getRowValuesSSTAt workbookPart rowIndex sheet
            |> fun s -> 
                KeyValuePair.Create(Seq.item 0 s, Seq.skip 1 s)

        let tryGetKeyValueAt workbookPart rowIndex sheet: KeyValuePair<string,string seq> Option = 
            SheetTransformation.SSTSheets.tryGetRowValuesSSTAt workbookPart rowIndex sheet
            |> Option.map (
                fun s -> KeyValuePair.Create(Seq.item 0 s, Seq.skip 1 s)
            )

        let findIndexOfKeyValue workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.find (fun r ->
                let (k,vs) = SheetTransformation.SSTSheets.getValuesOfRowSST workbookPart r |> fun s -> Seq.head s, Seq.skip 1 s
                k = kv.Key
                &&
                Seq.contains kv.Value vs
            )
            |> Row.getIndex

        let tryFindIndexOfKeyValue workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.tryFind (fun r ->
                let (k,vs) = SheetTransformation.SSTSheets.getValuesOfRowSST workbookPart r |> fun s -> Seq.head s, Seq.skip 1 s
                k = kv.Key
                &&
                Seq.contains kv.Value vs
            )
            |> Option.map Row.getIndex

        let keyValueExists workbookPart (kv:KeyValuePair<string,string>) sheet = 
            tryFindIndexOfKeyValue workbookPart kv sheet 
            |> Option.isSome   