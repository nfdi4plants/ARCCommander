namespace ArcCommander.APIs

open System
open System.IO

open ArcCommander
open ArcCommander.ArgumentProcessing


/// ArcCommander API functions that get executed by top level subcommand verbs
module ArcAPI = 

    // TODO TO-DO TO DO: make use of args
    /// Initializes the arc specific folder structure
    let init (arcConfiguration:ArcConfiguration) (arcArgs : Map<string,Argument>) =

        ArcConfiguration.getRootFolderPaths arcConfiguration
        |> Array.iter (Directory.CreateDirectory >> ignore)

    /// Returns true if called anywhere in an arc 
    let isArc (arcConfiguration:ArcConfiguration) (arcArgs : Map<string,Argument>) = raise (NotImplementedException())