module ArcCommander.Program

// Learn more about F# at http://fsharp.org
open ArcCommander
open System
open Argu 

open CLIArguments
//open ArgumentMatching
open Assay

open System
open System.Reflection
open FSharp.Reflection


let processCommand silent workingDir commandF r =
    /// let parameterGroup =
    ///    groupArguments r
    ///    |> createParameterQuery
    /// try commandF silent workingDir parameterGroup
    /// finally
    ///     validate
    ///
    ///
    ()

let handleInvestigation silent workingDir investigation =
    match investigation with
    | Investigation.Init r      -> processCommand silent workingDir id r
    | Investigation.Update r    -> processCommand silent workingDir id r
    //| Investigation.Edit        -> processCommand silent workingDir id (ParseResults.)
    //| Investigation.Remove      -> processCommand silent workingDir id r


let handleStudy silent workingDir study =
    match study with
    | Study.Update r    -> processCommand silent workingDir id r
    | Study.Register r  -> processCommand silent workingDir id r
    | Study.Edit r      -> processCommand silent workingDir id r
    | Study.Remove r    -> processCommand silent workingDir id r


let handleAssay silent workingDir assay =
    match assay with
    | Assay.Add r       -> processCommand silent workingDir ArcCommander.Assay.add r
    | Assay.Create r    -> processCommand silent workingDir ArcCommander.Assay.create r
    | Assay.Register r  -> processCommand silent workingDir ArcCommander.Assay.register r
    | Assay.Update r    -> processCommand silent workingDir ArcCommander.Assay.update r
    | Assay.Move r      -> processCommand silent workingDir ArcCommander.Assay.move r
    | Assay.Remove r    -> processCommand silent workingDir ArcCommander.Assay.remove r
    | Assay.Edit r      -> processCommand silent workingDir ArcCommander.Assay.edit r

    //match subCommand with
    //| Add fields a -> 
    //    let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
    //    ArcCommander.Assay.add workingDir a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
    //    ()
    //| Register fields a -> 
    //    let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
    //    ArcCommander.Assay.register workingDir a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
    //    ()
    //| Update fields a -> 
    //    let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
    //    ArcCommander.Assay.update workingDir a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
    //    ()
    //| Create fields a -> 
    //    let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
    //    ArcCommander.Assay.create workingDir a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
    //    ()
    //| Remove fields a -> 
    //    let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
    //    ArcCommander.Assay.remove workingDir a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
    //    ()
    //| Edit fields a -> 
    //    let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
    //    ArcCommander.Assay.edit workingDir editorPath a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
    //    ()
    //| Move fields a -> 
    //    let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
    //    ArcCommander.Assay.move workingDir a.StudyIdentifier a.TargetStudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
    //    ()

let handleCommand silent workingDir command =
    match command with
    | Investigation subCommand  -> handleInvestigation silent workingDir (subCommand.GetSubCommand())
    | Study subCommand          -> handleStudy silent workingDir (subCommand.GetSubCommand())
    | Assay subCommand          -> handleAssay silent workingDir (subCommand.GetSubCommand())
    | Init r                    -> processCommand silent workingDir id r


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
        let parser = ArgumentParser.Create<Arc>(programName = "ArcCommander.exe")
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