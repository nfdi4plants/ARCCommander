namespace ArcCommander


open System
open Microsoft.FSharp.Reflection

open Argu 
open System
open ISA
open DataModel

open CLIArguments

module ArgumentMatching = 
 
    let private getRecordTypeFieldLabels<'T> ()=
        let props = FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>)
        props |> Array.map (fun prop -> prop.Name)
        
    let private recordTypeOfObjectArray<'a> (tmp: obj []) =
        let objectBuilder = FSharpValue.PreComputeRecordConstructor(typeof<'a>)
        objectBuilder tmp :?> 'a
    
    let private unionToString (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, v -> case.Name,v
    
    let private alignRecordOnUnions<'Record> (unionFields : (string * obj []) list) =
        let m = unionFields |> Map.ofList
        getRecordTypeFieldLabels<'Record>()
        |> Array.map (fun field -> 
            match Map.tryFind field m with
            | Some [|v|] -> v |> box
            | Some v -> v |> box
            | None -> null 
        )
        |> recordTypeOfObjectArray<'Record>
    

    module Assay =

        open ArgumentQuery.Assay

        let transformAssayParams<'U,'T when 'U :> IArgParserTemplate> (unionFlags : (string*obj []) list) (parseResults:ParseResults<'U>) = 
            parseResults.GetAllResults()
            |> List.map unionToString
            |> List.append unionFlags
            |> alignRecordOnUnions<'T>           

        let (|Add|_|) (unionFlags : (string*obj []) list) (results: ParseResults<Assay>) =
            match results.TryGetResult(Assay.Add) with
            | Some (r) -> 
                transformAssayParams<AssayParams,AssayFull> unionFlags r
                |> Some
            | _ -> None
        let (|Update|_|) (unionFlags : (string*obj []) list) (results: ParseResults<Assay>) =
            match results.TryGetResult(Assay.Update) with
            | Some (r) -> 
                transformAssayParams<AssayParams,AssayFull> unionFlags r
                |> Some
            | _ -> None
        let (|Register|_|) (unionFlags : (string*obj []) list) (results: ParseResults<Assay>) =
            match results.TryGetResult(Assay.Register) with
            | Some (r) -> 
                transformAssayParams<AssayParams,AssayFull> unionFlags r
                |> Some
            | _ -> None
        let (|Move|_|) (unionFlags : (string*obj []) list) (results: ParseResults<Assay>) =
            match results.TryGetResult(Assay.Move) with
            | Some (r) -> 
                transformAssayParams<TargetStudy,AssayMove> unionFlags r
                |> Some
            | _ -> None
        let (|Create|_|) (unionFlags : (string*obj []) list) (results: ParseResults<Assay>) =
            match results.TryGetResult(Assay.Create) with
            | Some (r) -> 
                unionFlags
                |> alignRecordOnUnions<AssayBasic>
                |> Some
            | _ -> None
        let (|Remove|_|) (unionFlags : (string*obj []) list) (results: ParseResults<Assay>) =
            match results.TryGetResult(Assay.Remove) with
            | Some (r) -> 
                unionFlags
                |> alignRecordOnUnions<AssayBasic>
                |> Some
            | _ -> None
        let (|Edit|_|) (unionFlags : (string*obj []) list) (results: ParseResults<Assay>) =
            match results.TryGetResult(Assay.Remove) with
            | Some (r) -> 
                unionFlags
                |> alignRecordOnUnions<AssayBasic>
                |> Some
            | _ -> None


    let (|AssayNoSubCommand|_|) (results: ParseResults<ArcArgs>) =
        match results.TryGetResult(ArcArgs.Assay) with
        | Some (r) -> 
            match r.TryGetSubCommand() with
            | Some _ -> None
            | None -> Some ()           
        | _ -> 
            None

    let (|Assay|_|) (results: ParseResults<ArcArgs>) =
        match results.TryGetResult(ArcArgs.Assay) with
        | Some (r) -> 
            let subCommand = r.GetSubCommand()
            r.GetAllResults()
            |> List.filter ((<>) subCommand)
            |> List.map unionToString
            |> fun fields -> Some(fields,r)
        | _ -> 
            None

    ////let x = 1
    //let (|AddAssay|_|) (results: ParseResults<ArcArgs>) =
    //    match results.TryGetResult(ArcArgs.Assay) with
    //    | Some (r) -> 
    //        (1,2)
    //        |> Some
    //    | _ -> None

    //let (|AddStudy|_|) (results: ParseResults<ArcArgs>) =
    //    match results.TryGetResult(ArcArgs.AddStudy) with
    //    | Some (r) -> 
    //        let identifier = 
    //            match r.TryGetResult(StudyArgs.Identifier) with 
    //            | Some x -> x 
    //            | None -> 
    //                printfn "Please Input assay identifier"
    //                Console.ReadLine()

    //        let study = InvestigationFile.StudyItem(identifier=identifier)
    //        r.GetAllResults()
    //        |> List.iter (fun x -> x.mapStudy study)

    //        study
    //        |> Some
    //    | _ -> None

    //let (|InitArc|_|) (results: ParseResults<ArcArgs>) =
    //    match results.TryGetResult(ArcArgs.InitArc) with
    //    | Some (r) -> 
    //        let identifier = 
    //            match r.TryGetResult(InvestigationArgs.Identifier) with 
    //            | Some x -> x 
    //            | None -> 
    //                printfn "Please Input investigation identifier"
    //                Console.ReadLine()
    //        let investigation = ISA.DataModel.InvestigationFile.InvestigationItem(identifier=identifier)
    //        r.GetAllResults()
    //        |> List.iter (fun x -> x.mapInvestigation investigation)

    //        investigation
    //        |> Some
    //    | _ -> None


