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

module Option = 
    let pipe (f : 'T -> 'U option) (v : 'T  Option) : 'U Option = 
        Option.map f v 
        |> Option.flatten

    let equals (f : 'T -> bool) (v : 'T  Option) = 
        Option.map f v
        |> (=) (Some true)

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
    
    let tryParseKey workbookPart row =
        SheetTransformation.SSTSheets.getIndexedValuesOfRowSST workbookPart row       
        |> Seq.tryFind (fst >> (=) 1u)
        |> Option.map snd

    let findIndexOfKey workbookPart key sheet = 
        sheet
        |> SheetData.getRows
        |> Seq.find (
            tryParseKey workbookPart
            >> Option.equals ((=) key) 
        )
        |> Row.getIndex

    let tryFindIndexOfKey workbookPart key sheet = 
        sheet
        |> SheetData.getRows
        |> Seq.tryFind (
            tryParseKey workbookPart
            >> Option.equals ((=) key) 
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
            tryParseKey workbookPart
            >> Option.equals ((=) key)
        )
        |> Option.map Row.getIndex

    let keyExists workbookPart key sheet = 
        tryFindIndexOfKey workbookPart key sheet 
        |> Option.isSome   

    module SingleTrait = 
                
        let tryParseKeyValue workbookPart row =
            SheetTransformation.SSTSheets.getIndexedValuesOfRowSST workbookPart row       
            |> fun s -> 
                match Seq.tryFind (fst >> (=) 1u) s, Seq.tryFind (fst >> (=) 2u) s with
                | Some (_,k), Some (_,v) -> KeyValuePair(k,v) |> Some
                | _ -> None


        let getKeyValueAt workbookPart rowIndex sheet : KeyValuePair<string,string> = 
            SheetData.getRowAt rowIndex sheet
            |> tryParseKeyValue workbookPart
            |> Option.get

        let tryGetKeyValueAt workbookPart rowIndex sheet: KeyValuePair<string,string> Option = 
            SheetData.tryGetRowAt rowIndex sheet
            |> Option.pipe (tryParseKeyValue workbookPart)

        let findIndexOfKeyValue workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.find (
                tryParseKeyValue workbookPart
                >> Option.equals ((=) kv)
            )
            |> Row.getIndex

        let tryFindIndexOfKeyValue workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.tryFind (
                tryParseKeyValue workbookPart
                >> Option.equals ((=) kv)
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
            |> Seq.tryFind (
                tryParseKeyValue workbookPart
                >> Option.equals ((=) kv) 
            )
            |> Option.map Row.getIndex

        let keyValueExists workbookPart (kv:KeyValuePair<string,string>) sheet = 
            tryFindIndexOfKeyValue workbookPart kv sheet 
            |> Option.isSome   

    module MultiTrait = 
         

        let tryParseKeyValues workbookPart row =
            SheetTransformation.SSTSheets.getIndexedValuesOfRowSST workbookPart row       
            |> fun s -> 
                match Seq.tryFind (fst >> (=) 1u) s with
                | Some (_,k) -> 
                    KeyValuePair(k,Seq.skip 1 s) |> Some                                     
                | _ -> None

        let getKeyValuesAt workbookPart rowIndex sheet : KeyValuePair<string,(uint*string) seq> = 
            SheetData.getRowAt rowIndex sheet
            |> tryParseKeyValues workbookPart
            |> Option.get

        let tryGetKeyValuesAt workbookPart rowIndex sheet: KeyValuePair<string,(uint*string) seq> Option = 
            SheetData.getRowAt rowIndex sheet
            |> tryParseKeyValues workbookPart

        let findIndexOfKeyValue workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.find (fun r ->
                tryParseKeyValues workbookPart r 
                |> Option.equals (fun kvs' ->
                    kvs'.Key = kv.Key
                    &&
                    (kvs'.Value |> Seq.exists (snd >> (=) kv.Value))
                )
            )
            |> Row.getIndex

        let tryFindIndexOfKeyValue workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.tryFind (fun r ->
                tryParseKeyValues workbookPart r 
                |> Option.equals (fun kvs' ->
                    kvs'.Key = kv.Key
                    &&
                    (kvs'.Value |> Seq.exists (snd >> (=) kv.Value))
                )
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
                tryParseKeyValues workbookPart r 
                |> Option.equals (fun kvs' ->
                    kvs'.Key = kv.Key
                    &&
                    (kvs'.Value |> Seq.exists (snd >> (=) kv.Value))
                )
            )
            |> Option.map Row.getIndex

        let tryFindColumnIndicesOfKeyValueBetween startI endI workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.filter (fun r ->
                r
                |> Row.getIndex 
                |> fun i -> i >= startI && i <= endI
            )
            |> Seq.tryFind (fun r ->
                tryParseKey workbookPart r 
                |> Option.equals ((=) kv.Key)
            )
            |> Option.pipe (fun r -> 
                tryParseKeyValues workbookPart r
            )
            |> Option.pipe (fun kvs -> 
                match Seq.choose (fun (i,v) -> if v = kv.Value then Some i else None) kvs.Value with
                | s when Seq.isEmpty s -> None
                | s -> Some s
            )
            
            
           

        let keyValueExists workbookPart (kv:KeyValuePair<string,string>) sheet = 
            tryFindIndexOfKeyValue workbookPart kv sheet 
            |> Option.isSome   


module ISA_Investigation  = 

    open DataModel.InvestigationFile

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
            doc
            |> Spreadsheet.saveChanges
            |> Spreadsheet.close
        with 
        | err -> 
            doc |> Spreadsheet.close
            printfn "Could not create investigation file %s: %s" path err.Message

    let emptyStudy id = StudyItem(Identifier = id)

    let studyExists studyIdentifier (spreadSheet:SpreadsheetDocument) =
        let doc = spreadSheet
        let workbookPart = doc |> Spreadsheet.getWorkbookPart
        let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

        let kv = KeyValuePair("Study Identifier",studyIdentifier)

        let res = ISA_Sheet.SingleTrait.keyValueExists workbookPart kv sheet

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

    let tryGetStudyScope workbookPart study sheet =
        match ISA_Sheet.SingleTrait.tryFindIndexOfKeyValue workbookPart (KeyValuePair("Study Identifier",study)) sheet with
        | Some studyIndex ->
            match Scope.tryFindScopeAt workbookPart studyIndex sheet with
            | Some scope -> Some scope
            | None -> failwith "Corrupt Investigation file: No Header could be found"
        | None -> 
            printfn "study %s does not exist in the spreadsheet" study
            None

    let private tryGetItemScope workbookPart study studyScope (item:#ISAItem) sheet =
            let itemHeader = studyScope.Name + " " + item.Header
            match ISA_Sheet.tryFindIndexOfKeyBetween studyScope.From studyScope.To workbookPart itemHeader sheet with
            | Some assayIndex ->
                Scope.tryFindScopeAt workbookPart assayIndex sheet
            | None -> 
                SheetIO.insertRowAt [itemHeader] (studyScope.To + 1u) sheet |> ignore
                Scope.create itemHeader 2 (studyScope.To + 1u) (studyScope.To + 1u)
                |> Some
                //printfn "item does not exist in the study %s" study
                //None


    let private tryFindColumnInItemScope workbookPart prefix (scope:Scope) (item:#ISAItem) sheet =
        let keyValuesOfInterest = 
            item.KeyValuesOfInterest()
            |> List.map (fun (key,value) ->
                prefix + " " + item.KeyPrefix + " " + key, value
            )
        keyValuesOfInterest
        |> List.map (fun (k,v) -> 
            let kv = KeyValuePair(k,v)
            match ISA_Sheet.MultiTrait.tryFindColumnIndicesOfKeyValueBetween scope.From scope.To workbookPart kv sheet with
            | Some s -> set s
            | None -> Set.empty           
        )
        |> List.reduce Set.intersect
        |> fun s ->
            if Set.isEmpty s then
                None
            else 
                seq s |> Seq.head |> Some

    let private removeScopeIfEmpty workbookPart (scope:Scope) sheet =
        let rowWithValueExists = 
            [scope.From .. scope.To]
            |> List.exists (fun i -> 
                SheetTransformation.SSTSheets.getRowValuesSSTAt workbookPart i sheet |> Seq.length 
                |> (<) 1
            )
        if rowWithValueExists then
            sheet
        else
            [scope.From .. scope.To]
            |> List.rev
            |> List.fold (fun s i -> SheetTransformation.DirectSheets.removeRowAt i s) sheet
            

    let private updateItemValuesInStudy workbookPart scope columnIndex (item:#ISAItem) sheet = 
        let keyValues = 
            item.KeyValues()
            |> List.map (fun (key,value) ->
                "Study" + " " + item.KeyPrefix + " " + key, value
            )
            |> List.filter (snd >> (<>) "")
        let itemCount = 
            [scope.From .. scope.To]
            |> Seq.map (fun i -> 
                SheetTransformation.DirectSheets.getRowValuesAt i sheet |> Seq.length 
            )            
            |> Seq.max 

        keyValues
        |> List.fold (fun scope (key,value) -> 
            match ISA_Sheet.tryFindIndexOfKeyBetween scope.From scope.To workbookPart key sheet with
            | Some i ->
                //TODO/TO-DO: does the item only shove the other items to the right? If so another function should be used
                SheetIO.setValue columnIndex i value  sheet |> ignore
                scope
            | None -> 
                let rowValues = 
                    Array.init itemCount (fun i -> 
                        if i = 0 then key 
                        elif i = ((int columnIndex) - 1) then value 
                        else "")
                SheetIO.insertRowAt rowValues (scope.To + 1u) sheet |> ignore
                Scope.extendScope scope
        ) scope
        |> ignore

    let private insertItemValuesIntoStudy workbookPart scope (item:#ISAItem) sheet =         
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

    let tryRemoveItemFromStudy (item:#ISAItem) (study:string) (spreadSheet:SpreadsheetDocument) = 
        let workbookPart = spreadSheet |> Spreadsheet.getWorkbookPart
        try
            let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

            let studyScope = tryGetStudyScope workbookPart study sheet
                       
            let itemScope = 
                studyScope
                |> Option.pipe (fun studyScope -> tryGetItemScope workbookPart study studyScope item sheet) 
                
                
            itemScope
            |> Option.pipe (fun itemScope -> 
                tryFindColumnInItemScope workbookPart "Study" itemScope item sheet
                |> Option.map (fun colI ->
                    [itemScope.From .. itemScope.To]
                    |> List.rev
                    |> List.iter (fun rowI -> SheetTransformation.DirectSheets.tryRemoveValueAt colI rowI sheet |> ignore)                  
                    sheet
                    |> removeScopeIfEmpty workbookPart itemScope
                )
            )
            
            |> Option.map (fun x -> 
                spreadSheet.Save()
                spreadSheet               
            )


        with 
        | err -> 
            printfn "Could not remove %s from study %s: %s" item.KeyPrefix study err.Message
            None

    let tryAddItemToStudy (item:#ISAItem) (study:string) (spreadSheet:SpreadsheetDocument) = 
        let workbookPart = spreadSheet |> Spreadsheet.getWorkbookPart
        try
            let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

            let studyScope = tryGetStudyScope workbookPart study sheet
                   
            let itemScope = 
                studyScope
                |> Option.pipe (fun studyScope -> tryGetItemScope workbookPart study studyScope item sheet) 
            
            itemScope
            |> Option.map (fun itemScope -> 
                match tryFindColumnInItemScope workbookPart "Study" itemScope item sheet with
                | Some colI -> 
                    printfn "item %s already exists in study %s" item.KeyPrefix study
                    None
                | None -> 
                    insertItemValuesIntoStudy workbookPart itemScope item sheet
                    Some spreadSheet
            )

        with 
        | err -> 
            printfn "Could not add item %s to study %s: %s" item.KeyPrefix study err.Message
            None


    //let addItemToStudy (item:#ISAItem) (study:string) (spreadSheet:SpreadsheetDocument) = 
    //    let workbookPart = spreadSheet |> Spreadsheet.getWorkbookPart
    //    try
    //        let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

    //        let studyScope =
    //            match ISA_Sheet.SingleTrait.tryFindIndexOfKeyValue workbookPart (KeyValuePair("Study Identifier",study)) sheet with
    //            | Some studyIndex -> 
    //                match Scope.tryFindScopeAt workbookPart studyIndex sheet with
    //                | Some scope -> scope
    //                | None -> failwith "Corrupt Investigation file"
    //            | None -> 
    //                addStudy (emptyStudy study) spreadSheet 
    //                Scope.tryFindScopeAt workbookPart (SheetTransformation.maxRowIndex sheet) sheet |> Option.get

    //        let itemHeader = (studyScope.Name + " " + item.Header)

    //        let itemScope = 
    //            match ISA_Sheet.tryFindIndexOfKeyBetween studyScope.From studyScope.To workbookPart itemHeader sheet with
    //            | Some assayIndex ->
    //                Scope.tryFindScopeAt workbookPart assayIndex sheet |> Option.get
    //            | None -> 
    //                SheetIO.insertRowAt [itemHeader] (studyScope.To + 1u) sheet |> ignore
    //                Scope.create itemHeader 2 (studyScope.To + 1u) (studyScope.To + 1u)

    //        let itemIdentifier =
    //            ISA_Sheet.MultiTrait.tryFindIndexOfKeyValueBetween




    //        insertItemValuesIntoStudy workbookPart itemScope item sheet
    //        spreadSheet.Save()
    //    with 
    //    | err -> 
    //        printfn "Could not add %s to study %s: %s" item.KeyPrefix study err.Message

    let tryUpdateItemInStudy (item:#ISAItem) (study:string) (spreadSheet:SpreadsheetDocument) = 
        let workbookPart = spreadSheet |> Spreadsheet.getWorkbookPart
        try
            let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

            let studyScope = tryGetStudyScope workbookPart study sheet
           
            let itemScope = 
                studyScope
                |> Option.pipe (fun studyScope -> tryGetItemScope workbookPart study studyScope item sheet) 
               
    
            itemScope
            |> Option.pipe (fun itemScope -> 
                tryFindColumnInItemScope workbookPart "Study" itemScope item sheet
                |> Option.map (fun colI ->
                    updateItemValuesInStudy workbookPart itemScope colI item sheet
                )
            )
            |> Option.map (fun x -> 
                spreadSheet.Save()
                spreadSheet               
            )

        with 
        | err -> 
            printfn "Could not add %s to study %s: %s" item.KeyPrefix study err.Message
            None

    

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