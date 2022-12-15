module ArcCommander.Program

// Learn more about F# at http://fsharp.org
open ArcCommander
open ArcCommander.ArgumentProcessing
open ArcCommander.Commands
open ArcCommander.APIs
open ArcCommander.ExternalExecutables
open ArcCommander.IniData

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
                log.Fatal("Mandatory arguments were not given either via cli or editor prompt.")
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

let handleRemoteAccessAccessTokenSubCommands arcConfiguration accessTokenVerb =
    match accessTokenVerb with
    | AccessTokenCommand.Store r    -> processCommand arcConfiguration RemoteAccessAPI.AccessToken.store r
    | AccessTokenCommand.Get r      -> processCommand arcConfiguration RemoteAccessAPI.AccessToken.get r

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
    | ConfigurationCommand.Edit         r -> processCommand arcConfiguration ConfigurationAPI.edit  r
    | ConfigurationCommand.List         r -> processCommand arcConfiguration ConfigurationAPI.list  r
    | ConfigurationCommand.Set          r -> processCommand arcConfiguration ConfigurationAPI.set   r
    | ConfigurationCommand.Unset        r -> processCommand arcConfiguration ConfigurationAPI.unset r
    | ConfigurationCommand.SetGitUser   r -> processCommand arcConfiguration ConfigurationAPI.setGitUser r
    
let handleRemoteAccessSubCommands arcConfiguration remoteAccessVerb =
    match remoteAccessVerb with
    | RemoteAccessCommand.AccessToken subCommand -> handleRemoteAccessAccessTokenSubCommands arcConfiguration (subCommand.GetSubCommand())

let handleCommand arcConfiguration command =
    match command with
    // Objects
    | Investigation subCommand  -> handleInvestigationSubCommands   arcConfiguration (subCommand.GetSubCommand())
    | Study subCommand          -> handleStudySubCommands           arcConfiguration (subCommand.GetSubCommand())
    | Assay subCommand          -> handleAssaySubCommands           arcConfiguration (subCommand.GetSubCommand())
    | Configuration subcommand  -> handleConfigurationSubCommands   arcConfiguration (subcommand.GetSubCommand())
    | RemoteAccess subcommand   -> handleRemoteAccessSubCommands    arcConfiguration (subcommand.GetSubCommand())
    // Verbs
    | Init r                    -> processCommand                   arcConfiguration ArcAPI.init r
    | Export r                  -> processCommand                   arcConfiguration ArcAPI.export r
    | Import r                  -> processCommand                   arcConfiguration ArcAPI.import r
    | Update                    -> processCommandWithoutArgs        arcConfiguration ArcAPI.update
    | Version                   -> processCommandWithoutArgs        arcConfiguration ArcAPI.version
    // Git Verbs
    | Sync r                    -> processCommand                   arcConfiguration GitAPI.sync r
    | Get r                     -> processCommand                   arcConfiguration GitAPI.get r
    // Settings
    | WorkingDir _ | Verbosity _-> ()


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
            | None      -> Directory.GetCurrentDirectory()

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
        
        let arcCommanderDataFolder = IniData.createDataFolder ()
        let arcDataFolder = 
            tryGetArcDataFolderPath workingDir arcCommanderDataFolder
            |> Option.defaultValue arcCommanderDataFolder
        Logging.generateConfig arcDataFolder (GeneralConfiguration.getVerbosity arcConfiguration)
        let log = Logging.createLogger "ArcCommanderMainLog"

        log.Trace("Start ArcCommander")

        // Try parse the command line arguments
        let parseResults = tryExecuteExternalTool log parser argv workingDir

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
        
        let currDir = Directory.GetCurrentDirectory()

        // check for existence of an ARC-specific log file:
        try
            let arcCommanderDataFolder = IniData.createDataFolder ()
            let arcDataFolder = 
                tryGetArcDataFolderPath currDir arcCommanderDataFolder
                |> Option.defaultValue arcCommanderDataFolder
            Logging.generateConfig arcDataFolder 0

        // create logging config, create .arc folder if not already existing:
        with _ ->
            let arcFolder = Path.Combine(currDir, ".arc")
            Directory.CreateDirectory(arcFolder) |> ignore
            Logging.generateConfig arcFolder 0 
        
        let log = Logging.createLogger "ArcCommanderMainLog"

        Logging.handleExceptionMessage log e1

        1