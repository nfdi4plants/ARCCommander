// Learn more about F# at http://fsharp.org
open ArcCommander
open System
open Argu 

open CLIArguments
open ArgumentMatching
open Assay


[<EntryPoint>]
let main argv =
    try
        let parser = ArgumentParser.Create<ArcArgs>(programName = "ArcCommander.exe")
        let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true) 

        let editorPath = "notepad"

        let workingDir = 
            match results.TryGetResult(WorkingDir) with
            | Some s    -> s
            | None      -> System.IO.Directory.GetCurrentDirectory()
        printfn "WorkDir: %s" workingDir
        ()

        match results with
        | AssayNoSubCommand -> ()
        | Assay (fields,subCommand) ->            
            match subCommand with
            | Add fields a -> 
                let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
                ArcCommander.Assay.add workingDir a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
                ()
            | Register fields a -> 
                let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
                ArcCommander.Assay.register workingDir a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
                ()
            | Update fields a -> 
                let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
                ArcCommander.Assay.update workingDir a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
                ()
            | Create fields a -> 
                let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
                ArcCommander.Assay.create workingDir a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
                ()
            | Remove fields a -> 
                let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
                ArcCommander.Assay.remove workingDir a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
                ()
            | Edit fields a -> 
                let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
                ArcCommander.Assay.edit workingDir editorPath a.StudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
                ()
            | Move fields a -> 
                let assayParams = a |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
                ArcCommander.Assay.move workingDir a.StudyIdentifier a.TargetStudyIdentifier (assayParams |> ArgumentQuery.Assay.toISAAssay)
                ()

            | _ -> ()
        | _ -> ()

    with e ->
        printfn "%s" e.Message
    0
    //let path = 
    //match argv    |> List.ofArray with
    //| InitARC :: args ->
    //    let parser = ArgumentParser.Create<ArcArgs>
    //    let results = parser.Parse args
        
    //| AddAssay :: args -> 
    //    let parser = ArgumentParser.Create<AssayArgs>
    //    let results = parser.Parse args
    
