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

let processCommand silent workingDir commandF (r : ParseResults<'T>) =
    let parameterGroup =
        let g = groupArguments (r.GetAllResults())
        createParameterQueryIfNecessary @"C:\Program Files\Notepad++\notepad++.exe" workingDir g
    printfn "start process with the parameters: \n%O" parameterGroup
    //commandF silent workingDir parameterGroup
    try commandF workingDir parameterGroup
    finally
        printfn "done processing command"
 

let handleInvestigation silent workingDir investigation =
    match investigation with
    | Investigation.Init r      -> processCommand silent workingDir (fun _ _ -> ()) r
    | Investigation.Update r    -> processCommand silent workingDir (fun _ _ -> ()) r
    //| Investigation.Edit        -> processCommand silent workingDir id (ParseResults.)
    //| Investigation.Remove      -> processCommand silent workingDir id r


let handleStudy silent workingDir study =
    match study with
    | Study.Update r    -> processCommand silent workingDir (fun _ _ -> ()) r
    | Study.Register r  -> processCommand silent workingDir (fun _ _ -> ()) r
    | Study.Edit r      -> processCommand silent workingDir (fun _ _ -> ()) r
    | Study.Remove r    -> processCommand silent workingDir (fun _ _ -> ()) r


let handleAssay silent workingDir assay =
    match assay with
    | Assay.Add r       -> processCommand silent workingDir ArcCommander.Assay.add r
    | Assay.Create r    -> processCommand silent workingDir ArcCommander.Assay.create r
    | Assay.Register r  -> processCommand silent workingDir ArcCommander.Assay.register r
    | Assay.Update r    -> processCommand silent workingDir ArcCommander.Assay.update r
    | Assay.Move r      -> processCommand silent workingDir ArcCommander.Assay.move r
    | Assay.Remove r    -> processCommand silent workingDir ArcCommander.Assay.remove r
    | Assay.Edit r      -> processCommand silent workingDir (ArcCommander.Assay.edit "notepad") r
    | Assay.List r      -> processCommand silent workingDir ArcCommander.Assay.list r 

let handleCommand silent workingDir command =
    match command with
    | Investigation subCommand  -> handleInvestigation silent workingDir (subCommand.GetSubCommand())
    | Study subCommand          -> handleStudy silent workingDir (subCommand.GetSubCommand())
    | Assay subCommand          -> handleAssay silent workingDir (subCommand.GetSubCommand())
    
    | Init r                    -> processCommand silent workingDir (fun _ _ -> ()) r
    | WorkingDir    _           -> ()

    //let path = 
    //match argv    |> List.ofArray with
    //| InitARC :: args ->
    //    let parser = ArgumentParser.Create<ArcArgs>
    //    let results = parser.Parse args
        
    //| AddAssay :: args -> 
    //    let parser = ArgumentParser.Create<AssayArgs>
    //    let results = parser.Parse args
    
[<EntryPoint>]
let main argv =
    try
        let parser = ArgumentParser.Create<Arc>(programName = "arc")
        let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true) 

        let editorPath = "notepad"

        let workingDir = 
            match results.TryGetResult(WorkingDir) with
            | Some s    -> s
            | None      -> System.IO.Directory.GetCurrentDirectory()

        let silent = false

        printfn "WorkDir: %s" workingDir

        handleCommand silent workingDir (results.GetSubCommand())

        1
    with e ->
        printfn "%s" e.Message
        0