﻿module ArcCommander.Program

// Learn more about F# at http://fsharp.org
open Argu 

open ArcCommander
open ArcCommander.ArgumentProcessing
open ArcCommander.CLIArguments
open ArcCommander.Commands
open ArcCommander.APIs

let processCommand (arcConfiguration:ArcConfiguration) commandF (r : ParseResults<'T>) =

    let editor = GeneralConfiguration.getEditor arcConfiguration
    let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

    let parameterGroup =
        let g = groupArguments (r.GetAllResults())
        Prompt.createArgumentQueryIfNecessary editor workDir g  

    printfn "start processing command with the config"
    arcConfiguration 
    |> ArcConfiguration.flatten
    |> Seq.iter (fun (a,b) -> printfn "\t%s:%s" a b)

    printfn "\nand the parameters: \n" 
    parameterGroup|> Map.iter (printfn "\t%s:%O")

    try commandF arcConfiguration parameterGroup
    finally
        printfn "done processing command"

let processCommandWithoutArgs (arcConfiguration:ArcConfiguration) commandF =

    printfn "start processing parameterless command with the config"
    arcConfiguration 
    |> ArcConfiguration.flatten
    |> Seq.iter (fun (a,b) -> printfn "\t%s:%s" a b)

    try commandF arcConfiguration
    finally
        printfn "done processing command"

let handleInvestigationSubCommands arcConfiguration investigationVerb =
    match investigationVerb with
    | InvestigationCommand.Create r    -> processCommand arcConfiguration InvestigationAPI.create   r
    | InvestigationCommand.Update r    -> processCommand arcConfiguration InvestigationAPI.update   r
    | InvestigationCommand.Edit r      -> processCommand arcConfiguration InvestigationAPI.edit     r
    | InvestigationCommand.Delete r    -> processCommand arcConfiguration InvestigationAPI.delete   r

let handleStudySubCommands arcConfiguration studyVerb =
    match studyVerb with
    | StudyCommand.Init r      -> processCommand arcConfiguration StudyAPI.init     r
    | StudyCommand.Update r    -> processCommand arcConfiguration StudyAPI.update   r
    | StudyCommand.Edit r      -> processCommand arcConfiguration StudyAPI.edit     r
    | StudyCommand.Register r  -> processCommand arcConfiguration StudyAPI.register r
    | StudyCommand.Add r       -> processCommand arcConfiguration StudyAPI.add      r
    | StudyCommand.Remove r    -> processCommand arcConfiguration StudyAPI.remove   r
    | StudyCommand.List        -> processCommandWithoutArgs arcConfiguration StudyAPI.list

let handleAssaySubCommands arcConfiguration assayVerb =
    match assayVerb with
    | AssayCommand.Init     r -> processCommand arcConfiguration AssayAPI.init     r
    | AssayCommand.Update   r -> processCommand arcConfiguration AssayAPI.update   r
    | AssayCommand.Edit     r -> processCommand arcConfiguration AssayAPI.edit     r
    | AssayCommand.Register r -> processCommand arcConfiguration AssayAPI.register r
    | AssayCommand.Add      r -> processCommand arcConfiguration AssayAPI.add      r
    | AssayCommand.Remove   r -> processCommand arcConfiguration AssayAPI.remove   r
    | AssayCommand.Move     r -> processCommand arcConfiguration AssayAPI.move     r
    | AssayCommand.List       -> processCommandWithoutArgs arcConfiguration AssayAPI.list 

let handleConfigurationSubCommands arcConfiguration configurationVerb =
    match configurationVerb with
    | ConfigurationCommand.Edit     r -> processCommand arcConfiguration ConfigurationAPI.edit  r
    | ConfigurationCommand.List     r -> processCommand arcConfiguration ConfigurationAPI.list  r
    | ConfigurationCommand.Set      r -> processCommand arcConfiguration ConfigurationAPI.set   r
    | ConfigurationCommand.Unset    r -> processCommand arcConfiguration ConfigurationAPI.unset r

let handleCommand arcConfiguration command =
    match command with
    // Objects
    | Investigation subCommand  -> handleInvestigationSubCommands   arcConfiguration (subCommand.GetSubCommand())
    | Study subCommand          -> handleStudySubCommands           arcConfiguration (subCommand.GetSubCommand())
    | Assay subCommand          -> handleAssaySubCommands           arcConfiguration (subCommand.GetSubCommand())
    | Configuration subcommand  -> handleConfigurationSubCommands   arcConfiguration (subcommand.GetSubCommand())
    // Verbs
    | Init r                    -> processCommand   arcConfiguration ArcAPI.init r
    // Settings
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

        let arcConfiguration = 
            [
                "general.workdir",workingDir
                "general.silent",silent
            ]
            |> IniData.fromNameValuePairs
            |> ArcConfiguration.load
            
        //Testing the configuration reading (Delete when configuration functionality is setup)
        //printfn "load config:"    
        //Configuration.loadConfiguration workingDir
        //|> Configuration.flatten
        //|> Seq.iter (fun (a,b) -> printfn "%s=%s" a b)


        handleCommand arcConfiguration (results.GetSubCommand())

        1
    with e ->
        printfn "%s" e.Message
        0