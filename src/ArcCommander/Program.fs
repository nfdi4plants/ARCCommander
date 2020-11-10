module ArcCommander.Program

// Learn more about F# at http://fsharp.org
open ArcCommander
open Argu 

open ArcCommander.CLIArguments
//open ArgumentMatching
open Assay

open System
open System.Reflection
open FSharp.Reflection

open ParameterProcessing


let processCommand (globalArgs:Map<string,string>) commandF (r : ParseResults<'T>) =
    printfn "\nstart process with the global parameters: \n" 
    globalArgs |> Map.iter (printfn "\t%s:%s")

    let parameterGroup =
        let g = groupArguments (r.GetAllResults())
        Prompt.createParameterQueryIfNecessary globalArgs.["EditorPath"] globalArgs.["WorkingDir"] g  
    printfn "\nand the parameters: \n" 
    parameterGroup|> Map.iter (printfn "\t%s:%s")

    try commandF globalArgs parameterGroup
    finally
        printfn "done processing command"

let processCommandWithoutArgs (globalParams:Map<string,string>) commandF =
    printfn "\nstart process with the global parameters: \n" 
    globalParams |> Map.iter (printfn "\t%s:%s")
    try commandF globalParams
    finally
        printfn "done processing command"

let handleInvestigationSubCommands globalArgs investigationVerb =
    match investigationVerb with
    | Investigation.Create r    -> processCommand globalArgs Investigation.create r
    | Investigation.Update r    -> processCommand globalArgs Investigation.update r
    | Investigation.Edit r      -> processCommand globalArgs Investigation.edit r
    | Investigation.Delete r    -> processCommand globalArgs Investigation.delete r

let handleStudySubCommands globalArgs studyVerb =
    match studyVerb with
    | Study.Init r      -> processCommand globalArgs Study.init r
    | Study.Update r    -> processCommand globalArgs Study.update r
    | Study.Edit r      -> processCommand globalArgs Study.edit r
    | Study.Register r  -> processCommand globalArgs Study.register r
    | Study.Add r       -> processCommand globalArgs Study.add r
    | Study.Remove r    -> processCommand globalArgs Study.remove r
    | Study.List        -> processCommandWithoutArgs globalArgs Study.list

let handleAssaySubCommands globalArgs assayVerb =
    match assayVerb with
    | Assay.Init r      -> processCommand globalArgs Assay.init r
    | Assay.Update r    -> processCommand globalArgs Assay.update r
    | Assay.Edit r      -> processCommand globalArgs Assay.edit r
    | Assay.Register r  -> processCommand globalArgs Assay.register r
    | Assay.Add r       -> processCommand globalArgs Assay.add r
    | Assay.Remove r    -> processCommand globalArgs Assay.remove r
    | Assay.Move r      -> processCommand globalArgs Assay.move r
    | Assay.List        -> processCommandWithoutArgs globalArgs Assay.list 

let handleCommand globalArgs command =
    match command with
    | Investigation subCommand  -> handleInvestigationSubCommands globalArgs (subCommand.GetSubCommand())
    | Study subCommand          -> handleStudySubCommands globalArgs (subCommand.GetSubCommand())
    | Assay subCommand          -> handleAssaySubCommands globalArgs (subCommand.GetSubCommand())
    | Init r                    -> processCommand globalArgs Arc.init r
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
            match Prompt.tryReadGlobalParams workingDir with
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