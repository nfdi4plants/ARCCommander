namespace ArcCommander

open System
open System.IO
open System.Diagnostics
open Argu

/// Functions for trying to run external tools, given the command line arguments can not be parsed.
module ExternalExecutables =

    /// Checks if there are unknown arguments that Argu cannot resolve.
    let tryGetUnknownArguments (parser : ArgumentParser<'T>) (args : string []) = 
        let ignR = parser.Parse(args, ignoreUnrecognized = true)
        Array.init args.Length (fun i ->

            try 
                let r = parser.Parse(Array.take i args)
                if ignR = r then Some (Array.take (i + 1) args, Array.skip (i + 1) args)
                else None
            with 
            | _ -> None
        )
        |> Array.tryPick id

    /// Returns the possible name of an executable given the input.
    let makeExecutableName (args : string []) =
        Array.append [|"arc"|] args
        |> Array.reduce (fun a b -> a + "-" + b)

    /// Returns ARC folders where an external executable might be present.
    let getArcFoldersForExtExe root = // arc folder: Externals, .arc, root; 
        root :: (
            ["externals"; ".arc"]
            |> List.map (fun p -> Path.Combine(root, p))
        )

    /// Adds a extra directory to the PATH variable.
    let addExtraDirToPath os extraDir =
        let oldpath = Environment.GetEnvironmentVariable("PATH") 
        let sep = match os with | Windows -> ";" | Unix -> ":"
        Environment.SetEnvironmentVariable("PATH", oldpath + sep + extraDir, EnvironmentVariableTarget.Process) // use `EnvironmentVariableTarget.Process` for temporary changes