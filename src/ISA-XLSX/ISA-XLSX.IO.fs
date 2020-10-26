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

module SheetIO = SheetTransformation.DirectSheets

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
            | ["ONTOLOGY"; "SOURCE"; "REFERENCE"] -> Some(s,1)
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
            | _ when i = 0u -> None
            | _ -> tryUpwards (i - 1u)

        let rec downWards level lastIndex i = 
            let r = SheetTransformation.SSTSheets.tryGetRowValuesSSTAt workbookPart i sheet
            match r |> Option.map (Seq.head >> trySplitTitle) with
            | Some (Some (v,l)) when l <= level -> lastIndex
            | _ when i > maxIndex -> lastIndex
            | Some (_) -> downWards level i (i + 1u)
            | None -> downWards level lastIndex (i + 1u)            

        tryUpwards i
        |> Option.map (fun (f,v,l) -> 
            downWards l (f + 1u) (f + 1u)
            |> create v l f)

    let extendScope (scope:Scope) =
        {scope with To = scope.To + 1u}

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
                KeyValuePair(Seq.item 0 s, Seq.item 1 s)

        let tryGetKeyValueAt workbookPart rowIndex sheet: KeyValuePair<string,string> Option = 
            SheetTransformation.SSTSheets.tryGetRowValuesSSTAt workbookPart rowIndex sheet
            |> Option.map (
                fun s -> KeyValuePair(Seq.item 0 s, Seq.item 1 s)
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
                KeyValuePair(Seq.item 0 s, Seq.skip 1 s)

        let tryGetKeyValueAt workbookPart rowIndex sheet: KeyValuePair<string,string seq> Option = 
            SheetTransformation.SSTSheets.tryGetRowValuesSSTAt workbookPart rowIndex sheet
            |> Option.map (
                fun s -> KeyValuePair(Seq.item 0 s, Seq.skip 1 s)
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

    let createInvestigationFile = 
        1

    let createEmpty path (investigation : InvestigationItem) = 

        let doc = SheetTransformation.createEmptySSTSpreadsheet "i_investigation" path
        try 
            let workbookPart = doc |> Spreadsheet.getWorkbookPart
            let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart
        
            SheetIO.appendRow ["INVESTIGATION"] sheet |> ignore
            (investigation :> ISAItem).KeyValues()
            |> List.map (fun (k,v) -> 
                let vs = [(investigation :> ISAItem).KeyPrefix + " " + k; v]
                SheetIO.appendRow vs sheet
            )
            |> ignore
            doc.Save()
            doc.Close()
        with 
        | err -> 
            doc.Close()
            printfn "Could not create investigation file %s: %s" path err.Message


    let emptyStudy id = StudyItem(Identifier = id)

    let studyExists studyIdentifier (spreadSheet:SpreadsheetDocument) =
        let doc = spreadSheet
        let workbookPart = doc |> Spreadsheet.getWorkbookPart
        let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

        let kv = KeyValuePair("Study Identifier",studyIdentifier)

        let res = ISA_Sheet.SingleTrait.keyValueExists workbookPart kv sheet

        doc
        |> Spreadsheet.saveChanges

        res

    let addStudy (study:StudyItem) (spreadSheet:SpreadsheetDocument) =
        let doc = spreadSheet
        let workbookPart = doc |> Spreadsheet.getWorkbookPart
        let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart
    
        SheetIO.appendRow ["STUDY"] sheet |> ignore
        
        (study :> ISAItem).KeyValues()
        |> List.map (fun (k,v) -> 
            let vs = [(study :> ISAItem).KeyPrefix + " " + k; v]
            SheetIO.appendRow vs sheet
        )
        |> ignore

        doc
        |> Spreadsheet.saveChanges


    let insertItemValuesIntoStudy workbookPart scope (item:#ISAItem) sheet =         
        //MAP assayItems FIELDS
        item.KeyValues()
        |> List.map (fun (key,value) ->
            "Study" + " " + item.KeyPrefix + " " + key, value
        )
        |> List.fold (fun scope (key,value) -> 
            match ISA_Sheet.tryFindIndexOfKeyBetween scope.From scope.To workbookPart key sheet with
            | Some i ->
                //TODO/TO-DO: does the item only shove the other items to the right? If so another function should be used
                SheetIO.insertValue 2u i value  sheet |> ignore
                scope
            | None -> 
                SheetIO.insertRowAt [key;value] (scope.To + 1u) sheet |> ignore
                Scope.extendScope scope
        ) scope
        |> ignore

    let addItemToStudy (item:#ISAItem) (study:string) (spreadSheet:SpreadsheetDocument) = 
        let doc = spreadSheet
        let workbookPart = doc |> Spreadsheet.getWorkbookPart
        try
            let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

            let studyScope =
                match ISA_Sheet.SingleTrait.tryFindIndexOfKeyValue workbookPart (KeyValuePair("Study Identifier",study)) sheet with
                | Some studyIndex -> 
                    match Scope.tryFindScopeAt workbookPart studyIndex sheet with
                    | Some scope -> scope
                    | None -> failwith "Corrupt Investigation file"
                | None -> 
                    addStudy (emptyStudy study) spreadSheet 
                    Scope.tryFindScopeAt workbookPart (SheetTransformation.maxRowIndex sheet) sheet |> Option.get

            let itemHeader = (studyScope.Name + " " + item.Header)

            let itemScope = 
                match ISA_Sheet.tryFindIndexOfKeyBetween studyScope.From studyScope.To workbookPart itemHeader sheet with
                | Some assayIndex ->
                    Scope.tryFindScopeAt workbookPart assayIndex sheet |> Option.get
                | None -> 
                    SheetIO.insertRowAt [itemHeader] (studyScope.To + 1u) sheet |> ignore
                    Scope.create itemHeader 2 (studyScope.To + 1u) (studyScope.To + 1u)

            insertItemValuesIntoStudy workbookPart itemScope item sheet
            doc.Save()
        with 
        | err -> 
            printfn "Could not add %s to study %s: %s" item.KeyPrefix study err.Message
    //let addAssayToStudy (assay:Assay) (study:string) (investigationFilePath:string) = 
    //    let doc = Spreadsheet.openSpreadsheet investigationFilePath true
    //    let workbookPart = doc |> Spreadsheet.getWorkbookPart
    //    let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

    //    let studyScope = 
    //        match ISA_Sheet.SingleTrait.tryFindIndexOfKeyValue workbookPart (KeyValuePair.Create("Study Identifier",study)) sheet with
    //        | Some studyIndex -> 
    //            match Scope.tryFindScopeAt workbookPart studyIndex sheet with
    //            | Some scope -> scope
    //            | None -> failwith "Corrupt Investigation file"
    //        | None -> 
    //            addStudy (emptyStudy study) investigationFilePath
    //            Scope.tryFindScopeAt workbookPart (SheetTransformation.maxRowIndex sheet) sheet |> Option.get

    //    let assayScope = 
    //        match ISA_Sheet.tryFindIndexOfKeyBetween studyScope.From studyScope.To workbookPart ("STUDY ASSAYS") sheet with
    //        | Some assayIndex ->
    //            Scope.tryFindScopeAt workbookPart assayIndex sheet |> Option.get
    //        | None -> 
    //            SheetTransformation.SSTSheets.insertRowSSTAt workbookPart ["STUDY ASSAYS"] (studyScope.To + 1u)
    //            Scope.create "STUDY ASSAYS" 2 (studyScope.To + 1u) (studyScope.To + 1u)

    //    addAssayInto workbookPart assayScope assay sheet


module ISA_Assay  = 

    let createAssayFile (filePath) =
        1