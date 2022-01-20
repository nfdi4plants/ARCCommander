module ArcCommander.Program

// Learn more about F# at http://fsharp.org
open ArcCommander
open ArcCommander.ArgumentProcessing
open ArcCommander.Commands
open ArcCommander.APIs
open ArcCommander.ExternalExecutables

open System
open System.IO
open System.Text
open System.Diagnostics
open Argu

/// Runs the given command with the given arguments and configuration. If mandatory arguments are missing, or the "forceEditor" flag is set, opens a prompt asking for additional input.
let processCommand (arcConfiguration : ArcConfiguration) commandF (r : ParseResults<'T>) =

    let log = Logging.createLogger "ProcessCommandLog"

    let editor = GeneralConfiguration.getEditor arcConfiguration
    let forceEditor = GeneralConfiguration.getForceEditor arcConfiguration

    // Create a collection of all arguments and flags of 'T, including information about whether they were given by the user or not
    let annotatedArguments = groupArguments (r.GetAllResults())

    // Try to collect additional informations
    let arguments = 
        // Opens a command line prompt asking for addtional information if a mandatory argument is missing. Fails if still not given
        if containsMissingMandatoryAttribute annotatedArguments then
            let stillMissingMandatoryArgs, arguments =
                Prompt.createMissingArgumentQuery editor annotatedArguments
            if stillMissingMandatoryArgs then
                log.Fatal("ERROR: Mandatory arguments were not given either via cli or editor prompt.")
                raise (Exception(""))
            arguments
        // Opens a command line prompt asking for addtional information if the "forceeditor" flag is set.
        elif forceEditor then
            Prompt.createArgumentQuery editor annotatedArguments

        else 
            Prompt.deannotateArguments annotatedArguments

    arguments |> Map.fold (fun acc k t -> acc + $"\t{k}:{t}\n") "Start processing command with the arguments:\n" |> log.Info

    arcConfiguration
    |> ArcConfiguration.flatten
    |> Seq.fold (fun acc (a,b) -> acc + $"\t{a}:{b}\n") "and the config:\n" |> log.Trace

    try commandF arcConfiguration arguments
    finally
        log.Info("Done processing command.")

/// Runs the given command with the given configuration.
let processCommandWithoutArgs (arcConfiguration : ArcConfiguration) commandF =

    let log = Logging.createLogger "ProcessCommandWithoutArgsLog"

    log.Info("Start processing parameterless command.")

    log.Trace("with the config")
    arcConfiguration
    |> ArcConfiguration.flatten
    |> Seq.iter (fun (a,b) -> log.Trace($"\t{a}:{b}"))
    Console.WriteLine()

    try commandF arcConfiguration
    finally
        log.Info("Done processing command.")

let handleStudyContactsSubCommands arcConfiguration contactsVerb =
    match contactsVerb with
    | StudyPersonCommand.Update r       -> processCommand arcConfiguration StudyAPI.Contacts.update     r
    | StudyPersonCommand.Edit r         -> processCommand arcConfiguration StudyAPI.Contacts.edit       r
    | StudyPersonCommand.Register r     -> processCommand arcConfiguration StudyAPI.Contacts.register   r
    | StudyPersonCommand.Unregister r   -> processCommand arcConfiguration StudyAPI.Contacts.unregister r
    | StudyPersonCommand.Show r         -> processCommand arcConfiguration StudyAPI.Contacts.show       r
    | StudyPersonCommand.List           -> processCommandWithoutArgs arcConfiguration StudyAPI.Contacts.list

let handleStudyPublicationsSubCommands arcConfiguration contactsVerb =
    match contactsVerb with
    | StudyPublicationCommand.Update r      -> processCommand arcConfiguration StudyAPI.Publications.update r
    | StudyPublicationCommand.Edit r        -> processCommand arcConfiguration StudyAPI.Publications.edit r
    | StudyPublicationCommand.Register r    -> processCommand arcConfiguration StudyAPI.Publications.register r
    | StudyPublicationCommand.Unregister r  -> processCommand arcConfiguration StudyAPI.Publications.unregister r
    | StudyPublicationCommand.Show r        -> processCommand arcConfiguration StudyAPI.Publications.show r
    | StudyPublicationCommand.List          -> processCommandWithoutArgs arcConfiguration StudyAPI.Publications.list

let handleStudyDesignSubCommands arcConfiguration designVerb =
    match designVerb with
    | StudyDesignCommand.Update r       -> processCommand arcConfiguration StudyAPI.Designs.update     r
    | StudyDesignCommand.Edit r         -> processCommand arcConfiguration StudyAPI.Designs.edit       r
    | StudyDesignCommand.Register r     -> processCommand arcConfiguration StudyAPI.Designs.register   r
    | StudyDesignCommand.Unregister r   -> processCommand arcConfiguration StudyAPI.Designs.unregister r
    | StudyDesignCommand.Show r         -> processCommand arcConfiguration StudyAPI.Designs.show    r
    | StudyDesignCommand.List           -> processCommandWithoutArgs arcConfiguration StudyAPI.Designs.list

let handleStudyFactorSubCommands arcConfiguration factorVerb =
    match factorVerb with
    | StudyFactorCommand.Update r       -> processCommand arcConfiguration StudyAPI.Factors.update     r
    | StudyFactorCommand.Edit r         -> processCommand arcConfiguration StudyAPI.Factors.edit       r
    | StudyFactorCommand.Register r     -> processCommand arcConfiguration StudyAPI.Factors.register   r
    | StudyFactorCommand.Unregister r   -> processCommand arcConfiguration StudyAPI.Factors.unregister r
    | StudyFactorCommand.Show r         -> processCommand arcConfiguration StudyAPI.Factors.show       r
    | StudyFactorCommand.List           -> processCommandWithoutArgs arcConfiguration StudyAPI.Factors.list

let handleStudyProtocolSubCommands arcConfiguration protocolVerb =
    match protocolVerb with
    | StudyProtocolCommand.Update r     -> processCommand arcConfiguration StudyAPI.Protocols.update     r
    | StudyProtocolCommand.Edit r       -> processCommand arcConfiguration StudyAPI.Protocols.edit       r
    | StudyProtocolCommand.Register r   -> processCommand arcConfiguration StudyAPI.Protocols.register   r
    | StudyProtocolCommand.Unregister r -> processCommand arcConfiguration StudyAPI.Protocols.unregister r
    | StudyProtocolCommand.Load r       -> processCommand arcConfiguration StudyAPI.Protocols.load       r
    | StudyProtocolCommand.Show r       -> processCommand arcConfiguration StudyAPI.Protocols.show      r
    | StudyProtocolCommand.List         -> processCommandWithoutArgs arcConfiguration StudyAPI.Protocols.list

let handleAssayContactsSubCommands arcConfiguration contactsVerb =
    match contactsVerb with
    | AssayPersonCommand.Update r       -> processCommand arcConfiguration AssayAPI.Contacts.update     r
    | AssayPersonCommand.Edit r         -> processCommand arcConfiguration AssayAPI.Contacts.edit       r
    | AssayPersonCommand.Register r     -> processCommand arcConfiguration AssayAPI.Contacts.register   r
    | AssayPersonCommand.Unregister r   -> processCommand arcConfiguration AssayAPI.Contacts.unregister r
    | AssayPersonCommand.Show r         -> processCommand arcConfiguration AssayAPI.Contacts.show       r
    | AssayPersonCommand.List           -> processCommandWithoutArgs arcConfiguration AssayAPI.Contacts.list

let handleInvestigationContactsSubCommands arcConfiguration contactsVerb =
    match contactsVerb with
    | InvestigationPersonCommand.Update r       -> processCommand arcConfiguration InvestigationAPI.Contacts.update     r
    | InvestigationPersonCommand.Edit r         -> processCommand arcConfiguration InvestigationAPI.Contacts.edit       r
    | InvestigationPersonCommand.Register r     -> processCommand arcConfiguration InvestigationAPI.Contacts.register   r
    | InvestigationPersonCommand.Unregister r   -> processCommand arcConfiguration InvestigationAPI.Contacts.unregister r
    | InvestigationPersonCommand.Show r         -> processCommand arcConfiguration InvestigationAPI.Contacts.show       r
    | InvestigationPersonCommand.List           -> processCommandWithoutArgs arcConfiguration InvestigationAPI.Contacts.list

let handleInvestigationPublicationsSubCommands arcConfiguration publicationVerb =
    match publicationVerb with
    | InvestigationPublicationCommand.Update r      -> processCommand arcConfiguration InvestigationAPI.Publications.update r
    | InvestigationPublicationCommand.Edit r        -> processCommand arcConfiguration InvestigationAPI.Publications.edit r
    | InvestigationPublicationCommand.Register r    -> processCommand arcConfiguration InvestigationAPI.Publications.register r
    | InvestigationPublicationCommand.Unregister r  -> processCommand arcConfiguration InvestigationAPI.Publications.unregister r
    | InvestigationPublicationCommand.Show r        -> processCommand arcConfiguration InvestigationAPI.Publications.show r
    | InvestigationPublicationCommand.List          -> processCommandWithoutArgs arcConfiguration InvestigationAPI.Publications.list

let handleInvestigationSubCommands arcConfiguration investigationVerb =
    match investigationVerb with
    | InvestigationCommand.Create r                 -> processCommand arcConfiguration InvestigationAPI.create   r
    | InvestigationCommand.Update r                 -> processCommand arcConfiguration InvestigationAPI.update   r
    | InvestigationCommand.Edit                     -> processCommandWithoutArgs arcConfiguration InvestigationAPI.edit
    | InvestigationCommand.Delete r                 -> processCommand arcConfiguration InvestigationAPI.delete   r
    | InvestigationCommand.Person subCommand        -> handleInvestigationContactsSubCommands arcConfiguration (subCommand.GetSubCommand())
    | InvestigationCommand.Publication subCommand   -> handleInvestigationPublicationsSubCommands arcConfiguration (subCommand.GetSubCommand())
    | InvestigationCommand.Show                     -> processCommandWithoutArgs arcConfiguration InvestigationAPI.show

let handleStudySubCommands arcConfiguration studyVerb =
    match studyVerb with
    | StudyCommand.Init r                   -> processCommand arcConfiguration StudyAPI.init        r
    | StudyCommand.Register r               -> processCommand arcConfiguration StudyAPI.register    r
    | StudyCommand.Add r                    -> processCommand arcConfiguration StudyAPI.add         r
    | StudyCommand.Remove r                 -> processCommand arcConfiguration StudyAPI.remove      r
    | StudyCommand.Unregister r             -> processCommand arcConfiguration StudyAPI.unregister  r
    | StudyCommand.Delete r                 -> processCommand arcConfiguration StudyAPI.delete      r
    | StudyCommand.Update r                 -> processCommand arcConfiguration StudyAPI.update      r
    | StudyCommand.Edit r                   -> processCommand arcConfiguration StudyAPI.edit        r
    | StudyCommand.Show r                   -> processCommand arcConfiguration StudyAPI.show        r
    | StudyCommand.List                     -> processCommandWithoutArgs arcConfiguration StudyAPI.list
    | StudyCommand.Person subCommand        -> handleStudyContactsSubCommands arcConfiguration (subCommand.GetSubCommand())
    | StudyCommand.Publication subCommand   -> handleStudyPublicationsSubCommands arcConfiguration (subCommand.GetSubCommand())
    | StudyCommand.Design subCommand        -> handleStudyDesignSubCommands arcConfiguration (subCommand.GetSubCommand())
    | StudyCommand.Factor subCommand        -> handleStudyFactorSubCommands arcConfiguration (subCommand.GetSubCommand())
    | StudyCommand.Protocol subCommand      -> handleStudyProtocolSubCommands arcConfiguration (subCommand.GetSubCommand())

let handleAssaySubCommands arcConfiguration assayVerb =
    match assayVerb with
    | AssayCommand.Init               r -> processCommand arcConfiguration AssayAPI.init          r
    | AssayCommand.Register           r -> processCommand arcConfiguration AssayAPI.register      r
    | AssayCommand.Add                r -> processCommand arcConfiguration AssayAPI.add           r
    | AssayCommand.Delete             r -> processCommand arcConfiguration AssayAPI.delete        r
    | AssayCommand.Unregister         r -> processCommand arcConfiguration AssayAPI.unregister    r
    | AssayCommand.Remove             r -> processCommand arcConfiguration AssayAPI.remove        r
    | AssayCommand.Update             r -> processCommand arcConfiguration AssayAPI.update        r
    | AssayCommand.Edit               r -> processCommand arcConfiguration AssayAPI.edit          r
    | AssayCommand.Move               r -> processCommand arcConfiguration AssayAPI.move          r
    | AssayCommand.Show               r -> processCommand arcConfiguration AssayAPI.show          r
    | AssayCommand.Export             r -> processCommand arcConfiguration AssayAPI.export        r
    | AssayCommand.List                 -> processCommandWithoutArgs arcConfiguration AssayAPI.list
    | AssayCommand.Person subCommand    -> handleAssayContactsSubCommands arcConfiguration (subCommand.GetSubCommand())

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
    | Init r                    -> processCommand                   arcConfiguration ArcAPI.init r
    | Export r                  -> processCommand                   arcConfiguration ArcAPI.export r
    | Update                    -> processCommandWithoutArgs        arcConfiguration ArcAPI.update
    // Git Verbs
    | Sync r                    -> processCommand                   arcConfiguration GitAPI.sync r
    | Get r                     -> processCommand                   arcConfiguration GitAPI.get r
    // Settings
    | WorkingDir _ | Verbosity _-> ()

/// Takes a logger and an exception and separates usage and error messages. Usage messages will be printed into the console while error messages will be logged.
let handleExceptionMessage (log : NLog.Logger) (exn : Exception) =
    // separate usage message (Argu) and error messages. Error messages shall be logged, usage messages shall not
    match exn.Message.Contains("USAGE") || exn.Message.Contains("SUBCOMMANDS"), exn.Message.Contains("ERROR") with
    | true,true -> // exception message contains usage AND error messages
        let eMsg, uMsg = 
            exn.Message.Split(Environment.NewLine) // '\n' leads to parsing problems
            |> fun arr ->
                arr |> Array.find (fun t -> t.Contains("ERROR")),
                arr |> Array.filter (fun t -> t.Contains("ERROR") |> not) |> String.concat "\n" // Argu usage instruction shall not be logged as error
        log.Error(eMsg)
        printfn "%s" uMsg
    | true,false -> printfn "%s" exn.Message // exception message contains usage message but NO error message
    | _ -> log.Error(exn.Message) // everything else will be an error message

/// Checks if a message (string) is empty and if it is not, applies a logging function to it.
let private checkNonLog s (logging : string -> unit) = if s <> "" then logging s

/// Deletes unwanted new lines at the end of an output.
let rec private reviseOutput (output : string) = 
    if output = null then ""
    elif output.EndsWith('\n') then reviseOutput (output.[0 .. output.Length - 2])
    else output

/// Checks if an error message coming from CMD not being able to call a program with the given name.
let private matchCmdErrMsg (errMsg : string) = errMsg.Contains("is not recognized as an internal or external command")

/// Checks if an error message coming from Bash not being able to call a program with the given name.
let private matchBashErrMsg (errMsg : string) = errMsg.Contains("bash: ") && errMsg.Contains("command not found") || errMsg.Contains("No such file or directory")

[<EntryPoint>]
let main argv =

    try
        let parser = ArgumentParser.Create<ArcCommand>()
        
        // Failsafe parsing of all correct argument information
        let safeParseResults = parser.ParseCommandLine(inputs = argv, ignoreMissing = true, ignoreUnrecognized = true)

        // Load configuration ---->
        let workingDir =
            match safeParseResults.TryGetResult(WorkingDir) with
            | Some s    -> s
            | None      -> System.IO.Directory.GetCurrentDirectory()

        let verbosity = safeParseResults.TryGetResult(Verbosity) |> Option.map string

        let arcConfiguration =
            [
                "general.workdir", Some workingDir
                "general.verbosity", verbosity
            ]
            |> List.choose (function | k, Some v -> Some (k,v) | _ -> None)
            |> IniData.fromNameValuePairs
            |> ArcConfiguration.load
        // <-----
        
        // here the logging config gets created
        let arcFolder = Path.Combine(arcConfiguration.General.Item "workdir", arcConfiguration.General.Item "rootfolder")
        Directory.CreateDirectory(arcFolder) |> ignore
        Logging.generateConfig arcFolder (GeneralConfiguration.getVerbosity arcConfiguration)
        let log = Logging.createLogger "ArcCommanderMainLog"

        // Try parse the command line arguments
        let parseResults = 
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
                    if executableName = "arc-ducklings" then StudyAPI.Protocols.playAllMyLittleDucklings()
                    let os = IniData.getOs ()
                    let pi = 
                        match os with
                        | Windows   -> ProcessStartInfo("cmd", String.concat " " ["/c"; executableName; yield! args; "-p"; workingDir])
                        | Unix      -> ProcessStartInfo("bash", String.concat " " ["-c"; "\""; executableName; yield! args; "-p"; workingDir; "\""])
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
                    let sbOutput = StringBuilder() // StringBuilder for TRACE output (verbosity 2)
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

        //Testing the configuration reading (Delete when configuration functionality is setup)
        //printfn "load config:"
        //Configuration.loadConfiguration workingDir
        //|> Configuration.flatten
        //|> Seq.iter (fun (a,b) -> printfn "%s=%s" a b)

        // Run the according command if command line args can be parsed
        match parseResults with
        | Some results ->
            handleCommand arcConfiguration (results.GetSubCommand())
            0
        | None -> 
            1

    with e1 ->
        
        // create logging config, create .arc folder if not already existing
        let currDir = Directory.GetCurrentDirectory()
        let arcFolder = Path.Combine(currDir, ".arc")
        Directory.CreateDirectory(arcFolder) |> ignore
        Logging.generateConfig arcFolder 0 
        let log = Logging.createLogger "ArcCommanderMainLog"

        handleExceptionMessage log e1

        1