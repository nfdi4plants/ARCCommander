// Learn more about F# at http://fsharp.org
open ArcCommander
open System
open Argu 
open ArgumentMatching

[<EntryPoint>]
let main argv =
    try
        let parser = ArgumentParser.Create<ArcArgs>(programName = "ArcCommander.exe")
        let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true) 

        let workingDir = 
            match results.TryGetResult(WorkingDir) with
            | Some s    -> s
            | None      -> System.IO.Directory.GetCurrentDirectory()
        printfn "WorkDir: %s" workingDir

        match results with
        | InitArc investigation -> Arc.init workingDir investigation
        | AddAssay (studyID,assay) -> Arc.addAssay workingDir studyID assay
        | AddStudy r -> ()
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
    
    