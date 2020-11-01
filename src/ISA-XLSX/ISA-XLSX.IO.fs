namespace ISA_XLSX.IO

open System.Collections.Generic

open ISA
open DataModel

open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet
open FSharpSpreadsheetML

/// The scope is used to specify the boundaries of a section inside the isa investigation file
type Scope =
    {
        /// Specifies the header name of the section
        Name: string
        /// Hierarchical Level of the header (e.g. STUDY = Level 1, STUDY CONTACTS = Level 2)
        Level: int
        /// Upper rowindex boundary of the section
        From: uint32
        /// Lower rowindex boundary of the section
        To: uint32
    }

module SheetIO = SheetTransformation.DirectSheets

/// Option helper functions
module Option = 

    /// Option monad
    let pipe (f : 'T -> 'U option) (v : 'T  Option) : 'U Option = 
        Option.map f v 
        |> Option.flatten

    /// Returns true, if the result of the funtion returns Some true
    let equals (f : 'T -> bool) (v : 'T  Option) = 
        Option.map f v
        |> (=) (Some true)

/// Functions for working with scopes 
module Scope = 

    /// Creates a scope
    let create name level f t = 
        {
            Name = name
            Level = level
            From = f
            To = t
        }
   
   /// 1. Level terms
    let private terms = 
        [
        "ONTOLOGY SOURCE REFERENCE"
        "INVESTIGATION"
        "STUDY"
        ]

    /// If the row is a section title, returns header and level
    let private trySplitTitle (s : string) = 
        if s.ToUpper() = s then
            let vals = s.Split ' ' |> Array.toList
            match vals with
            | ["ONTOLOGY"; "SOURCE"; "REFERENCE"] -> Some(s,1)
            | [v] when List.contains v terms -> Some(v,1)
            | v :: t when List.contains v terms  -> Some(List.reduce (fun a b -> a + " " + b) vals,2)
            | _ -> None

            else None

    /// Finds the scope of the section in which the row at rowIndex i is located
    let tryFindScopeAt workbookPart i sheet =

        let maxIndex = sheet |> SheetTransformation.maxRowIndex

        /// Find header above the row of interest
        let rec tryUpwards i = 
            let r = SheetTransformation.SSTSheets.tryGetRowValuesSSTAt workbookPart i sheet
            match r |> Option.map (Seq.head >> trySplitTitle) with
            | Some (Some (v,l)) -> Some (i,v,l)
            | _ when i = 0u -> None
            | _ -> tryUpwards (i - 1u)

        /// Find the end of the section with hierarchical level i
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
    
    /// Increment the lower boundary of the scope by one
    let extendScope (scope:Scope) =
        {scope with To = scope.To + 1u}


module internal ISA_Investigation_Helpers  = 
    
    /// If existing, returns the key of the row
    let tryParseKey workbookPart row =
        SheetTransformation.SSTSheets.getIndexedValuesOfRowSST workbookPart row       
        |> Seq.tryFind (fst >> (=) 1u)
        |> Option.map snd

    /// Returns the index of the first row where the key matches the input key
    let findIndexOfKey workbookPart key sheet = 
        sheet
        |> SheetData.getRows
        |> Seq.find (
            tryParseKey workbookPart
            >> Option.equals ((=) key) 
        )
        |> Row.getIndex

    /// If a row with the given key exists, returns its rowkey
    let tryFindIndexOfKey workbookPart key sheet = 
        sheet
        |> SheetData.getRows
        |> Seq.tryFind (
            tryParseKey workbookPart
            >> Option.equals ((=) key) 
        )
        |> Option.map Row.getIndex

    /// If a row with the given key exists between the given rowkey boundaries, returns its rowkey
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

    /// Returns true, if a row with the given key exists
    let keyExists workbookPart key sheet = 
        tryFindIndexOfKey workbookPart key sheet 
        |> Option.isSome   

    /// Functions for working with sections where each key can only have one value
    ///
    /// Investigation, Study
    module SingleTrait = 
        
        /// If the row contains a key and a value, returns them
        let tryParseKeyValue workbookPart row =
            SheetTransformation.SSTSheets.getIndexedValuesOfRowSST workbookPart row       
            |> fun s -> 
                match Seq.tryFind (fst >> (=) 1u) s, Seq.tryFind (fst >> (=) 2u) s with
                | Some (_,k), Some (_,v) -> KeyValuePair(k,v) |> Some
                | _ -> None

        /// Gets key and value of the row at the given row index
        let getKeyValueAt workbookPart rowIndex sheet : KeyValuePair<string,string> = 
            SheetData.getRowAt rowIndex sheet
            |> tryParseKeyValue workbookPart
            |> Option.get

        /// If the row at the given index contains a key and a value, returns them
        let tryGetKeyValueAt workbookPart rowIndex sheet: KeyValuePair<string,string> Option = 
            SheetData.tryGetRowAt rowIndex sheet
            |> Option.pipe (tryParseKeyValue workbookPart)
        
        /// If a row with the given key exists between the given rowkey boundaries, returns its rowkey
        let findIndexOfKeyValue workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.find (
                tryParseKeyValue workbookPart
                >> Option.equals ((=) kv)
            )
            |> Row.getIndex

        /// If a row with the given key and value exists, returns its rowkey
        let tryFindIndexOfKeyValue workbookPart (kv:KeyValuePair<string,string>) sheet = 
            sheet
            |> SheetData.getRows
            |> Seq.tryFind (
                tryParseKeyValue workbookPart
                >> Option.equals ((=) kv)
            )
            |> Option.map Row.getIndex

        /// If a row with the given key and value exists between the given rowkey boundaries, returns its rowkey
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

        /// Returns ture, if a row with the given key and value exists
        let keyValueExists workbookPart (kv:KeyValuePair<string,string>) sheet = 
            tryFindIndexOfKeyValue workbookPart kv sheet 
            |> Option.isSome   

    /// Functions for working with sections where each key can contain multiple values
    ///
    /// Assay, Person, Publication, TermSource, Design, Factor, Protocol
    module MultiTrait = 
         
        /// If the row contains a key and at least one value, returns them
        let tryParseKeyValues workbookPart row =
            SheetTransformation.SSTSheets.getIndexedValuesOfRowSST workbookPart row       
            |> fun s -> 
                match Seq.tryFind (fst >> (=) 1u) s with
                | Some (_,k) -> 
                    KeyValuePair(k,Seq.skip 1 s) |> Some                                     
                | _ -> None

        /// If the row at the given rowindex contains a key and at least one value, returns them
        let getKeyValuesAt workbookPart rowIndex sheet : KeyValuePair<string,(uint*string) seq> = 
            SheetData.getRowAt rowIndex sheet
            |> tryParseKeyValues workbookPart
            |> Option.get

        /// If the row at the given rowindex contains a key and at least one value, returns them
        let tryGetKeyValuesAt workbookPart rowIndex sheet: KeyValuePair<string,(uint*string) seq> Option = 
            SheetData.getRowAt rowIndex sheet
            |> tryParseKeyValues workbookPart

        /// If a row with the given key and value exists, returns its rowkey
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

        /// If a row with the given key and value exists, returns its rowkey
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


        /// If a row with the given key and value exists between the given rowkey boundaries, returns its rowkey
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

        /// If a row with the given key exists between the boundaries and the given value is at least once present in the values of this key, returns their column indices
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
            
        /// Returns true, if a row with the given key and value exists 
        let keyValueExists workbookPart (kv:KeyValuePair<string,string>) sheet = 
            tryFindIndexOfKeyValue workbookPart kv sheet 
            |> Option.isSome   


module ISA_Investigation  = 

    open DataModel.InvestigationFile
    open ISA_Investigation_Helpers

    /// Creates an empty ivestigation file at the given path
    let createEmpty path (investigation : InvestigationItem) = 

        let doc = SheetTransformation.createEmptySSTSpreadsheet "i_investigation" path
        try 
            let workbookPart = doc |> Spreadsheet.getWorkbookPart
            let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart
        
            SheetIO.appendRow ["INVESTIGATION"] sheet |> ignore
            getKeyValues investigation
            |> Array.map (fun (k,v) -> 
                let vs = [(investigation :> ISAItem).KeyPrefix + " " + k; string v]
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

    /// Study with only an identifier
    let internal emptyStudy id = StudyItem(Identifier = id)

    /// Returns true, if the study exists in the investigation file
    let studyExists studyIdentifier (spreadSheet:SpreadsheetDocument) =
        let doc = spreadSheet
        let workbookPart = doc |> Spreadsheet.getWorkbookPart
        let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

        let kv = KeyValuePair("Study Identifier",studyIdentifier)

        let res = SingleTrait.keyValueExists workbookPart kv sheet

        res

    /// Add a study to the investigation file
    let addStudy (study:StudyItem) (spreadSheet:SpreadsheetDocument) =
        let doc = spreadSheet
        let workbookPart = doc |> Spreadsheet.getWorkbookPart
        let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart
    
        SheetIO.appendRow ["STUDY"] sheet |> ignore
        
        getKeyValues study
        |> Array.map (fun (k,v) -> 
            let vs = [(study :> ISAItem).KeyPrefix + " " + k; string v]
            SheetIO.appendRow vs sheet
        )
        |> ignore

        doc
        |> Spreadsheet.saveChanges

    /// If a study with the given identifier exists in the investigation file, returns its scope
    let private tryGetStudyScope workbookPart studyIdentifier sheet =
        match SingleTrait.tryFindIndexOfKeyValue workbookPart (KeyValuePair("Study Identifier",studyIdentifier)) sheet with
        | Some studyIndex ->
            match Scope.tryFindScopeAt workbookPart studyIndex sheet with
            | Some scope -> Some scope
            | None -> failwith "Corrupt Investigation file: No Header could be found"
        | None -> 
            printfn "study %s does not exist in the spreadsheet" studyIdentifier
            None

    /// If the section of the itemtype exists in the studyscope, returns its scope
    let private tryGetItemScope workbookPart studyScope (item:#ISAItem) sheet =
            let itemHeader = studyScope.Name + " " + item.Header
            match tryFindIndexOfKeyBetween studyScope.From studyScope.To workbookPart itemHeader sheet with
            | Some itemIndex ->
                Scope.tryFindScopeAt workbookPart itemIndex sheet
            | None -> 
                SheetIO.insertRowAt [itemHeader] (studyScope.To + 1u) sheet |> ignore
                Scope.create itemHeader 2 (studyScope.To + 1u) (studyScope.To + 1u)
                |> Some
                //printfn "item does not exist in the study %s" study
                //None

    /// If the item exists in the studyscope, returns its column
    let private tryFindColumnInItemScope workbookPart prefix (scope:Scope) (item:#ISAItem) sheet =
        let keyValuesOfInterest = 
            getIdentificationKeyValues item
            |> Array.map (fun (key,value) ->
                prefix + " " + item.KeyPrefix + " " + key, string value
            )
        keyValuesOfInterest
        |> Array.map (fun (k,v) -> 
            let kv = KeyValuePair(k,v)
            match MultiTrait.tryFindColumnIndicesOfKeyValueBetween scope.From scope.To workbookPart kv sheet with
            | Some s -> set s
            | None -> Set.empty           
        )
        |> Array.reduce Set.intersect
        |> fun s ->
            if Set.isEmpty s then
                None
            else 
                seq s |> Seq.head |> Some
    
    /// Removes a section descirbed by the given scope, if it is empty
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
            
    /// Replaces the values of the item at the scope with the given values
    let private updateItemValuesInStudy workbookPart scope columnIndex (item:#ISAItem) sheet = 
        let keyValues = 
            getKeyValues item
            |> Array.map (fun (key,value) ->
                "Study" + " " + item.KeyPrefix + " " + key, string value
            )
            |> Array.filter (snd >> (<>) "")
        let itemCount = 
            [scope.From .. scope.To]
            |> Seq.map (fun i -> 
                SheetTransformation.DirectSheets.getRowValuesAt i sheet |> Seq.length 
            )            
            |> Seq.max 

        keyValues
        |> Array.fold (fun scope (key,value) -> 
            match tryFindIndexOfKeyBetween scope.From scope.To workbookPart key sheet with
            | Some i ->
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

    /// Adds the given values to the scope
    let private insertItemValuesIntoStudy workbookPart scope (item:#ISAItem) sheet =         
        //MAP assayItems FIELDS
        getKeyValues item
        |> Array.map (fun (key,value) ->
            "Study" + " " + item.KeyPrefix + " " + key, string value
        )
        |> Array.fold (fun scope (key,value) -> 
            match tryFindIndexOfKeyBetween scope.From scope.To workbookPart key sheet with
            | Some i ->
                //TODO/TO-DO: does the item only shove the other items to the right? If so another function should be used
                SheetIO.insertValue 2u i value  sheet |> ignore
                scope
            | None -> 
                SheetIO.insertRowAt [key;value] (scope.To + 1u) sheet |> ignore
                Scope.extendScope scope
        ) scope
        |> ignore

    /// If the item exists in the study, removes it from the investigation file
    let tryRemoveItemFromStudy (item:#ISAItem) (study:string) (spreadSheet:SpreadsheetDocument) = 
        let workbookPart = spreadSheet |> Spreadsheet.getWorkbookPart
        try
            let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

            let studyScope = tryGetStudyScope workbookPart study sheet
                       
            let itemScope = 
                studyScope
                |> Option.pipe (fun studyScope -> tryGetItemScope workbookPart studyScope item sheet) 
                
                
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

    /// If the study exists adds, the item to it
    let tryAddItemToStudy (item:#ISAItem) (study:string) (spreadSheet:SpreadsheetDocument) = 
        let workbookPart = spreadSheet |> Spreadsheet.getWorkbookPart
        try
            let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

            let studyScope = tryGetStudyScope workbookPart study sheet
                   
            let itemScope = 
                studyScope
                |> Option.pipe (fun studyScope -> tryGetItemScope workbookPart studyScope item sheet) 
            
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

    /// If the item exists in the study, replaces its value with the given ones
    let tryUpdateItemInStudy (item:#ISAItem) (study:string) (spreadSheet:SpreadsheetDocument) = 
        let workbookPart = spreadSheet |> Spreadsheet.getWorkbookPart
        try
            let sheet = SheetTransformation.firstSheetOfWorkbookPart workbookPart

            let studyScope = tryGetStudyScope workbookPart study sheet
           
            let itemScope = 
                studyScope
                |> Option.pipe (fun studyScope -> tryGetItemScope workbookPart studyScope item sheet) 
               
    
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

module ISA_Assay  = 

    let createAssayFile (filePath) =
        1