namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISA.DataModel.InvestigationFile


/// ArcCommander API functions that get executed by top level subcommand verbs
module ArcAPI = 

    /// Initializes the arc specific folder structure
    let init (globalArgs: Map<string,string>) (cliArgs : Map<string,string>) =

        let workDir = globalArgs.["WorkingDir"]
        printfn "init arc in %s" workDir
         
        let dir = System.IO.Directory.CreateDirectory workDir
        dir.CreateSubdirectory "assays"     |> ignore
        dir.CreateSubdirectory "codecaps"   |> ignore
        dir.CreateSubdirectory "externals"  |> ignore
        dir.CreateSubdirectory "runs"       |> ignore
        dir.CreateSubdirectory ".arc"       |> ignore

        Prompt.writeGlobalParams dir.FullName cliArgs            

    /// Returns true if called anywhere in an arc 
    let isArc (globalArgs: Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())