namespace ArcCommander

open Logging

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

    let playAllMyLittleDucklings () = Console.Beep(262, 500); Console.Beep(294, 500); Console.Beep(330, 500); Console.Beep(349, 500); Console.Beep(392, 1000); Console.Beep(392, 1000); Console.Beep(440, 500); Console.Beep(440, 500); Console.Beep(440, 500); Console.Beep(440, 500); Console.Beep(392, 2000); Console.Beep(440, 500); Console.Beep(440, 500); Console.Beep(440, 500); Console.Beep(440, 500); Console.Beep(392, 2000); Console.Beep(349, 500); Console.Beep(349, 500); Console.Beep(349, 500); Console.Beep(349, 500); Console.Beep(330, 1000); Console.Beep(330, 1000); Console.Beep(294, 500); Console.Beep(294, 500); Console.Beep(294, 500); Console.Beep(294, 500); Console.Beep(262, 2000); 

    let tryExecuteExternalTool (log : NLog.Logger) (parser : ArgumentParser<Commands.ArcCommand>) argv (workingDir : string) =
        try
            // Correctly parsed arguments will be threaded into the command handler below
            parser.ParseCommandLine(inputs = argv, raiseOnUsage = true) 
            |> Some
        with
        | e2 -> 
            // Incorrectly parsed arguments will be threaded into external executable tool handler
            // Here for the first unknown argument in the argument chain, an executable of the same name will be called
            // If this tool exists, try executing it with all the following arguments
            log.Info("Could not parse given commands.")
            match tryGetUnknownArguments parser argv with
            | Some (executableNameArgs, args) ->
                let executableName = (makeExecutableName executableNameArgs)
                if executableName = "arc-ducklings" then playAllMyLittleDucklings()
                let os = IniData.getOs ()
                let pi = 
                    match os with
                    | Windows   -> ProcessStartInfo("cmd", String.concat " " ["/c"; executableName; yield! args; "-p"; workingDir])
                    | Unix      -> ProcessStartInfo("bash", String.concat " " ["-c"; "\""; executableName; yield! args; "-p"; workingDir; "\""])
                // TO DO: parse args "--noredirect". If given, redirections are set to false instead (or not touched, respectively)
                pi.RedirectStandardOutput <- true // is needed for logging the tool's console output
                pi.RedirectStandardError <- true // dito
                log.Info($"Try checking if executable with given argument name \"{executableName}\" exists.")
                // temporarily add extra directories to PATH
                let folderToAddToPath = getArcFoldersForExtExe workingDir
                List.iter (addExtraDirToPath os) folderToAddToPath
                // call external tool
                let p = new Process()
                p.StartInfo <- pi
                p.ErrorDataReceived.Add( // use event listener for error outputs since they should always have line feeds
                    fun ev -> 
                        let roev = reviseOutput ev.Data
                        if matchCmdErrMsg roev || matchBashErrMsg roev then 
                            log.Error("ERROR: No executable, command or script file with given argument name known.") 
                            handleExceptionMessage log e2
                            raise (Exception())
                        else checkNonLog roev (sprintf "External Tool ERROR: %s" >> log.Error)
                )
                let sbOutput = Text.StringBuilder() // StringBuilder for TRACE output (verbosity 2)
                sbOutput.Append("External tool: ") |> ignore
                try 
                    p.Start() |> ignore
                    p.BeginErrorReadLine() // starts the event listener
                    while not p.HasExited do
                        let charAsInt = p.StandardOutput.Read() // use this method instead because event listeners ONLY get triggered when line feeds occur
                        if charAsInt >= 0 then // -1 can be an exit character and would get parsed as line feed
                            printf "%c" (char charAsInt)
                            sbOutput.Append(char charAsInt) |> ignore
                    p.WaitForExit()
                    log.Trace(sbOutput.ToString()) // it is fine that the logging occurs after the external tool has done its job
                with e3 -> handleExceptionMessage log e3
                None
            // If neither parsing, nor external executable tool search led to success, just return the error message
            | None -> 
                handleExceptionMessage log e2
                None