module ArcCommander.Program

// Learn more about F# at http://fsharp.org
open Argu 

open ArcCommander
open ArcCommander.ArgumentProcessing
open ArcCommander.CLIArguments
open ArcCommander.Commands
open ArcCommander.APIs

open System
open System.Reflection
open FSharp.Reflection

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
    | InvestigationCommand.Create r    -> processCommand globalArgs InvestigationAPI.create r
    | InvestigationCommand.Update r    -> processCommand globalArgs InvestigationAPI.update r
    | InvestigationCommand.Edit r      -> processCommand globalArgs InvestigationAPI.edit r
    | InvestigationCommand.Delete r    -> processCommand globalArgs InvestigationAPI.delete r

let handleStudySubCommands globalArgs studyVerb =
    match studyVerb with
    | StudyCommand.Init r      -> processCommand globalArgs StudyAPI.init r
    | StudyCommand.Update r    -> processCommand globalArgs StudyAPI.update r
    | StudyCommand.Edit r      -> processCommand globalArgs StudyAPI.edit r
    | StudyCommand.Register r  -> processCommand globalArgs StudyAPI.register r
    | StudyCommand.Add r       -> processCommand globalArgs StudyAPI.add r
    | StudyCommand.Remove r    -> processCommand globalArgs StudyAPI.remove r
    | StudyCommand.List        -> processCommandWithoutArgs globalArgs StudyAPI.list

let handleAssaySubCommands globalArgs assayVerb =
    match assayVerb with
    | AssayCommand.Init r      -> processCommand globalArgs AssayAPI.init r
    | AssayCommand.Update r    -> processCommand globalArgs AssayAPI.update r
    | AssayCommand.Edit r      -> processCommand globalArgs AssayAPI.edit r
    | AssayCommand.Register r  -> processCommand globalArgs AssayAPI.register r
    | AssayCommand.Add r       -> processCommand globalArgs AssayAPI.add r
    | AssayCommand.Remove r    -> processCommand globalArgs AssayAPI.remove r
    | AssayCommand.Move r      -> processCommand globalArgs AssayAPI.move r
    | AssayCommand.List        -> processCommandWithoutArgs globalArgs AssayAPI.list 

let handleCommand globalArgs command =
    match command with
    | Investigation subCommand  -> handleInvestigationSubCommands globalArgs (subCommand.GetSubCommand())
    | Study subCommand          -> handleStudySubCommands globalArgs (subCommand.GetSubCommand())
    | Assay subCommand          -> handleAssaySubCommands globalArgs (subCommand.GetSubCommand())
    | Init r                    -> processCommand globalArgs ArcAPI.init r
    | WorkingDir _ | Silent     -> ()

[<EntryPoint>]
let main argv =
    try
        let parser = ArgumentParser.Create<ArcCommand>(programName = "arc")
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