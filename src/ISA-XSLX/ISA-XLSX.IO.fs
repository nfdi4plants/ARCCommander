namespace ISA_XLSX.IO

open System.Collections.Generic

open ISA
open DataModel

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet
open FSharpSpreadsheetML


type Scope =
    {
        Name: string
        Level: int
        From: uint32
        To: uint32
    }

module Scope = 

    let create name level f t = 
        {
            Name = name
            Level = level
            From = f
            To = t
        }
   
    let private terms = 
        [
        "ONTOLOGY SOURCE REFERENCE"
        "INVESTIGATION"
        "STUDY"
        ]

    let trySplitTitle (s : string) = 
        if s.ToUpper() = s then
            let vals = s.Split ' ' |> Array.toList
            match vals with
            | [v] when List.contains v terms -> Some(v,1)
            | v :: t when List.contains v terms  -> Some(List.reduce (fun a b -> a + " " + b) vals,2)
            | _ -> None

        else None

    let tryFindScopeAt workbookPart i sheet =

        let maxIndex = sheet |> SheetTransformation.maxRowIndex

        let rec tryUpwards i = 
            let r = SheetTransformation.SSTSheets.tryGetRowValuesSSTAt workbookPart i sheet
            match r |> Option.map (Seq.head >> trySplitTitle) with
            | Some (Some (v,l)) -> Some (i,v,l)
            | Some (None) | None-> tryUpwards (i - 1u)
            | _ when i = 0u -> None

        let rec downWards level lastIndex i = 
            let r = SheetTransformation.SSTSheets.tryGetRowValuesSSTAt workbookPart i sheet
            match r |> Option.map (Seq.head >> trySplitTitle) with
            | Some (Some (v,l)) when l >= level -> lastIndex
            | Some (_) -> downWards level i (i + 1u)
            | None -> downWards level lastIndex (i + 1u)            
            | _ when i > maxIndex -> lastIndex

        tryUpwards i
        |> Option.map (fun (f,v,l) -> 
            downWards l (f + 1u) (f + 1u)
            |> create v l f)

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

    let tryFindIndexOfKeyBetween startI endI workbookPart key sheet = 
        sheet
        |> SheetData.getRows
        |> Seq.filter (fun r ->
            r
            |> Row.getIndex 
            |> fun i -> i >= startI && i <= endI
        )
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

        let tryFindIndexOfKeyValueBetween startI endI workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.filter (fun r ->
                r
                |> Row.getIndex 
                |> fun i -> i >= startI && i <= endI
            )
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

        let tryFindIndexOfKeyValueBetween startI endI workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.filter (fun r ->
                r
                |> Row.getIndex 
                |> fun i -> i >= startI && i <= endI
            )
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


module ISA_Investigation  = 

    open DataModel.InvestigationFile

    let emptyStudy id = StudyItem(Identifier = id)

    let studyExists studyIdentifier (investigationFilePath:string) =
        let doc = Spreadsheet.openSpreadsheet investigationFilePath true
        let workbookPart = doc |> Spreadsheet.getWorkbookPart
        let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

        let kv = KeyValuePair.Create("Study Identifier",studyIdentifier)

        let res = ISA_Sheet.SingleTrait.keyValueExists workbookPart kv sheet

        doc
        |> Spreadsheet.saveChanges
        doc
        |> Spreadsheet.close

        res

    let addStudy (study:Study) (investigationFilePath:string) =
        let doc = Spreadsheet.openSpreadsheet investigationFilePath true
        let workbookPart = doc |> Spreadsheet.getWorkbookPart
        let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart
    
        SheetTransformation.SSTSheets.appendRowSST workbookPart ["STUDY"] sheet |> ignore
        //MAP study.Info FIELDS
        [
            ["Study Identifier";study.Info.Identifier]
        ]
        |> List.map (fun v -> SheetTransformation.SSTSheets.appendRowSST workbookPart v sheet)
        |> ignore

        doc
        |> Spreadsheet.saveChanges
        doc
        |> Spreadsheet.close

    //let addAssayToStudy (assay:Assay) (study:string) (investigationFilePath:string) = 
    //    let doc = Spreadsheet.openSpreadsheet investigationFilePath true
    //    let workbookPart = doc |> Spreadsheet.getWorkbookPart
    //    let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart


    //    match ISA_Sheet.SingleTrait.tryFindIndexOfKeyValue workbookPart (KeyValuePair.Create("Study Identifier",study)) sheet with
    //    | Some i -> 
    //        let scope = Scope.tryFindScopeAt workbookPart i sheet
    //        1

    //    | None -> addStudy emptyStudy investigationFilePath