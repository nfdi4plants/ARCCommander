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

        let editorPath = "notepad++"

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
                let a = 
                    a 
                    |> ArgumentQuery.askForFilloutIfNeeded editorPath workingDir 
                Arc.addAssay workingDir a.StudyIdentifier (a |> ArgumentQuery.toISAAssay)
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
    
