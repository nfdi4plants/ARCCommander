module ArcCommander.Program

// Learn more about F# at http://fsharp.org
open System
open System.IO
open Argu

open ArcCommander
open ArcCommander.ArgumentProcessing
open ArcCommander.Commands
open ArcCommander.APIs

let processCommand (arcConfiguration : ArcConfiguration) commandF (r : ParseResults<'T>) =

    let log = Logging.createLogger "ProcessCommandLog"

    let editor = GeneralConfiguration.getEditor arcConfiguration
    let forceEditor = GeneralConfiguration.getForceEditor arcConfiguration

    let annotatedArguments = groupArguments (r.GetAllResults())

    let arguments = 

        if containsMissingMandatoryAttribute annotatedArguments then
            let stillMissingMandatoryArgs, arguments =
                Prompt.createMissingArgumentQuery editor annotatedArguments
            if stillMissingMandatoryArgs then
                log.Error("ERROR: Mandatory arguments were not given either via cli or editor prompt.")
                raise (Exception(""))
            arguments

        elif forceEditor then
            Prompt.createArgumentQuery editor annotatedArguments

        else 
            Prompt.deannotateArguments annotatedArguments

    log.Info("Start processing command with the arguments.")
    arguments |> Map.iter (fun k t -> log.Info($"\t{k}:{t}"))
    Console.WriteLine()

    log.Trace("and the config:")
    arcConfiguration
    |> ArcConfiguration.flatten
    |> Seq.iter (fun (a,b) -> log.Trace($"\t{a}:{b}"))
    Console.WriteLine()

    try commandF arcConfiguration arguments
    finally
        log.Info("Done processing command.")

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

[<EntryPoint>]
let main argv =

    try
        let parser = ArgumentParser.Create<ArcCommand>()
        let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

        let workingDir =
            match results.TryGetResult(WorkingDir) with
            | Some s    -> s
            | None      -> System.IO.Directory.GetCurrentDirectory()

        let arcFolder = System.IO.Path.Combine(workingDir, ".arc")

        let verbosity = results.TryGetResult(Verbosity) |> Option.map string

        let arcConfiguration =
            [
                "general.workdir",Some workingDir
                "general.verbosity",verbosity
            ]
            |> List.choose (function | k,Some v -> Some (k,v) | _ -> None)
            |> IniData.fromNameValuePairs
            |> ArcConfiguration.load

        if Directory.Exists(arcFolder) then 
            Logging.generateConfig arcFolder (GeneralConfiguration.getVerbosity arcConfiguration)
        else Logging.generateConfig workingDir (GeneralConfiguration.getVerbosity arcConfiguration)

        //Testing the configuration reading (Delete when configuration functionality is setup)
        //printfn "load config:"
        //Configuration.loadConfiguration workingDir
        //|> Configuration.flatten
        //|> Seq.iter (fun (a,b) -> printfn "%s=%s" a b)

        handleCommand arcConfiguration (results.GetSubCommand())

        0

    with e ->
        
        let currDir = Directory.GetCurrentDirectory()
        let arcFolder = Path.Combine(currDir, ".arc")
        if Directory.Exists(arcFolder) then 
            Logging.generateConfig arcFolder 0 
        else Logging.generateConfig currDir 0

        let log = Logging.createLogger "ArcCommanderMainLog"
        match e.Message.Contains("USAGE"), e.Message.Contains("ERROR") with
        | true,true ->
            let eMsg, uMsg = 
                e.Message.Split(Environment.NewLine) // '\n' leads to parsing problems
                |> fun arr ->
                    arr |> Array.find (fun t -> t.Contains("ERROR")),
                    arr |> Array.filter (fun t -> t.Contains("ERROR") |> not) |> String.concat "\n" // Argu usage instruction shall not be logged as error
            log.Error(eMsg)
            printfn "%s" uMsg
        | true,false -> printfn "%s" e.Message
        | _ -> log.Error(e.Message)

        1