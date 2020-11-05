module ArcCommander.Program

// Learn more about F# at http://fsharp.org
open ArcCommander
open Argu 

open CLIArguments
//open ArgumentMatching
open Assay

open System
open System.Reflection
open FSharp.Reflection

open ArgumentQuery

let processCommand (globalParams:Map<string,string>) commandF (r : ParseResults<'T>) =
    printfn "\nstart process with the global parameters: \n" 
    globalParams |> Map.iter (printfn "\t%s:%s")

    let parameterGroup =
        let g = groupArguments (r.GetAllResults())
        createParameterQueryIfNecessary globalParams.["EditorPath"] globalParams.["WorkingDir"] g  
    printfn "\nand the parameters: \n" 
    parameterGroup|> Map.iter (printfn "\t%s:%s")

    try commandF globalParams parameterGroup
    finally
        printfn "done processing command"
 

let handleInvestigation globalParams investigation =
    match investigation with
    | Investigation.Init r      -> processCommand globalParams Investigation.init r
    | Investigation.Update r    -> processCommand globalParams (fun _ _ -> printfn "not yet implemented") r
    | Investigation.Edit r      -> processCommand globalParams (fun _ _ -> printfn "not yet implemented") r
    | Investigation.Remove r    -> processCommand globalParams (fun _ _ -> printfn "not yet implemented") r


let handleStudy globalParams study =
    match study with
    | Study.Update r    -> processCommand globalParams (fun _ _ -> printfn "not yet implemented") r
    | Study.Register r  -> processCommand globalParams Study.register r
    | Study.Edit r      -> processCommand globalParams (fun _ _ -> printfn "not yet implemented") r
    | Study.Remove r    -> processCommand globalParams (fun _ _ -> printfn "not yet implemented") r
    | Study.List r      -> processCommand globalParams Study.list r


let handleAssay globalParams assay =
    match assay with
    | Assay.Add r       -> processCommand globalParams Assay.add r
    | Assay.Create r    -> processCommand globalParams Assay.create r
    | Assay.Register r  -> processCommand globalParams Assay.register r
    | Assay.Update r    -> processCommand globalParams Assay.update r
    | Assay.Move r      -> processCommand globalParams Assay.move r
    | Assay.Remove r    -> processCommand globalParams Assay.remove r
    | Assay.Edit r      -> processCommand globalParams Assay.edit r
    | Assay.List r      -> processCommand globalParams Assay.list r 

let handleCommand globalParams command =
    match command with
    | Investigation subCommand  -> handleInvestigation globalParams (subCommand.GetSubCommand())
    | Study subCommand          -> handleStudy globalParams (subCommand.GetSubCommand())
    | Assay subCommand          -> handleAssay globalParams (subCommand.GetSubCommand())
    
    | Init r                    -> processCommand globalParams Arc.init r
    | WorkingDir _ | Silent     -> ()

[<EntryPoint>]
let main argv =
    try
        let parser = ArgumentParser.Create<Arc>(programName = "arc")
        let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true) 

        let workingDir = 
            match results.TryGetResult(WorkingDir) with
            | Some s    -> s
            | None      -> System.IO.Directory.GetCurrentDirectory()

        let silent = results.Contains(Silent) |> string

        let globalParams = 
            match tryReadGlobalParams workingDir with
            | Some gp -> gp
            | None -> ["EditorPath","notepad"] |> Map.ofList
            |> Map.add "WorkingDir" workingDir
            |> Map.add "Silent" silent

        printfn "WorkDir: %s" workingDir

        handleCommand globalParams (results.GetSubCommand())

        1
    with e ->
        printfn "%s" e.Message
        0