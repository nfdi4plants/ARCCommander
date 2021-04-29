module ArcCommander.Program

// Learn more about F# at http://fsharp.org
open Argu

open ArcCommander
open ArcCommander.ArgumentProcessing
open ArcCommander.Commands
open ArcCommander.APIs
open System

let processCommand (arcConfiguration:ArcConfiguration) commandF (r : ParseResults<'T>) =

    let editor = GeneralConfiguration.getEditor arcConfiguration
    let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
    let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
    let forceEditor = GeneralConfiguration.getForceEditor arcConfiguration


    let annotatedArguments = groupArguments (r.GetAllResults())

    let arguments = 

        if containsMissingMandatoryAttribute annotatedArguments then
            let stillMissingMandatoryArgs,arguments =
                Prompt.createMissingArgumentQuery editor workDir annotatedArguments
            if stillMissingMandatoryArgs then
                failwith "Mandatory arguments were not given either via cli or editor prompt."
            arguments

        elif forceEditor then
            Prompt.createArgumentQuery editor workDir annotatedArguments

        else 
            Prompt.deannotateArguments annotatedArguments

    if verbosity >= 1 then

        printfn "Start processing command with the arguments"
        arguments |> Map.iter (printfn "\t%s:%O")
        printfn "" 

    if verbosity >= 2 then

        printfn "and the config:"
        arcConfiguration
        |> ArcConfiguration.flatten
        |> Seq.iter (fun (a,b) -> printfn "\t%s:%s" a b)
        printfn "" 

    try commandF arcConfiguration arguments
    finally
        if verbosity >= 1 then printfn "Done processing command"

let processCommandWithoutArgs (arcConfiguration:ArcConfiguration) commandF =

    let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

    if verbosity >= 1 then

        printf "start processing parameterless command"

    if verbosity >= 2 then
        printfn "with the config"
        arcConfiguration
        |> ArcConfiguration.flatten
        |> Seq.iter (fun (a,b) -> printfn "\t%s:%s" a b)

    else printfn ""

    try commandF arcConfiguration
    finally
        if verbosity >= 1 then printfn "done processing command"

let handleStudyContactsSubCommands arcConfiguration contactsVerb =
    match contactsVerb with
    | StudyPersonCommand.Update r       -> processCommand arcConfiguration StudyAPI.Contacts.update     r
    | StudyPersonCommand.Edit r         -> processCommand arcConfiguration StudyAPI.Contacts.edit       r
    | StudyPersonCommand.Register r     -> processCommand arcConfiguration StudyAPI.Contacts.register   r
    | StudyPersonCommand.Unregister r   -> processCommand arcConfiguration StudyAPI.Contacts.unregister r
    | StudyPersonCommand.Get r          -> processCommand arcConfiguration StudyAPI.Contacts.get        r
    | StudyPersonCommand.List           -> processCommandWithoutArgs arcConfiguration StudyAPI.Contacts.list

let handleStudyPublicationsSubCommands arcConfiguration contactsVerb =
    match contactsVerb with
    | StudyPublicationCommand.Update r      -> processCommand arcConfiguration StudyAPI.Publications.update r
    | StudyPublicationCommand.Edit r        -> processCommand arcConfiguration StudyAPI.Publications.edit r
    | StudyPublicationCommand.Register r    -> processCommand arcConfiguration StudyAPI.Publications.register r
    | StudyPublicationCommand.Unregister r  -> processCommand arcConfiguration StudyAPI.Publications.unregister r
    | StudyPublicationCommand.Get r         -> processCommand arcConfiguration StudyAPI.Publications.get r
    | StudyPublicationCommand.List          -> processCommandWithoutArgs arcConfiguration StudyAPI.Publications.list

let handleStudyDesignSubCommands arcConfiguration designVerb =
    match designVerb with
    | StudyDesignCommand.Update r       -> processCommand arcConfiguration StudyAPI.Designs.update     r
    | StudyDesignCommand.Edit r         -> processCommand arcConfiguration StudyAPI.Designs.edit       r
    | StudyDesignCommand.Register r     -> processCommand arcConfiguration StudyAPI.Designs.register   r
    | StudyDesignCommand.Unregister r   -> processCommand arcConfiguration StudyAPI.Designs.unregister r
    | StudyDesignCommand.Get r          -> processCommand arcConfiguration StudyAPI.Designs.get        r
    | StudyDesignCommand.List           -> processCommandWithoutArgs arcConfiguration StudyAPI.Designs.list

let handleStudyFactorSubCommands arcConfiguration factorVerb =
    match factorVerb with
    | StudyFactorCommand.Update r       -> processCommand arcConfiguration StudyAPI.Factors.update     r
    | StudyFactorCommand.Edit r         -> processCommand arcConfiguration StudyAPI.Factors.edit       r
    | StudyFactorCommand.Register r     -> processCommand arcConfiguration StudyAPI.Factors.register   r
    | StudyFactorCommand.Unregister r   -> processCommand arcConfiguration StudyAPI.Factors.unregister r
    | StudyFactorCommand.Get r          -> processCommand arcConfiguration StudyAPI.Factors.get        r
    | StudyFactorCommand.List           -> processCommandWithoutArgs arcConfiguration StudyAPI.Factors.list

let handleStudyProtocolSubCommands arcConfiguration protocolVerb =
    match protocolVerb with
    | StudyProtocolCommand.Update r       -> processCommand arcConfiguration StudyAPI.Protocols.update     r
    | StudyProtocolCommand.Edit r         -> processCommand arcConfiguration StudyAPI.Protocols.edit       r
    | StudyProtocolCommand.Register r     -> processCommand arcConfiguration StudyAPI.Protocols.register   r
    | StudyProtocolCommand.Unregister r   -> processCommand arcConfiguration StudyAPI.Protocols.unregister r
    | StudyProtocolCommand.Load r         -> processCommand arcConfiguration StudyAPI.Protocols.load       r
    | StudyProtocolCommand.Get r          -> processCommand arcConfiguration StudyAPI.Protocols.get        r
    | StudyProtocolCommand.List           -> processCommandWithoutArgs arcConfiguration StudyAPI.Protocols.list

let handleInvestigationContactsSubCommands arcConfiguration contactsVerb =
    match contactsVerb with
    | InvestigationPersonCommand.Update r       -> processCommand arcConfiguration InvestigationAPI.Contacts.update     r
    | InvestigationPersonCommand.Edit r         -> processCommand arcConfiguration InvestigationAPI.Contacts.edit       r
    | InvestigationPersonCommand.Register r     -> processCommand arcConfiguration InvestigationAPI.Contacts.register   r
    | InvestigationPersonCommand.Unregister r   -> processCommand arcConfiguration InvestigationAPI.Contacts.unregister r
    | InvestigationPersonCommand.Get r          -> processCommand arcConfiguration InvestigationAPI.Contacts.get        r
    | InvestigationPersonCommand.List           -> processCommandWithoutArgs arcConfiguration InvestigationAPI.Contacts.list

let handleInvestigationPublicationsSubCommands arcConfiguration publicationVerb =
    match publicationVerb with
    | InvestigationPublicationCommand.Update r      -> processCommand arcConfiguration InvestigationAPI.Publications.update r
    | InvestigationPublicationCommand.Edit r        -> processCommand arcConfiguration InvestigationAPI.Publications.edit r
    | InvestigationPublicationCommand.Register r    -> processCommand arcConfiguration InvestigationAPI.Publications.register r
    | InvestigationPublicationCommand.Unregister r  -> processCommand arcConfiguration InvestigationAPI.Publications.unregister r
    | InvestigationPublicationCommand.Get r         -> processCommand arcConfiguration InvestigationAPI.Publications.get r
    | InvestigationPublicationCommand.List          -> processCommandWithoutArgs arcConfiguration InvestigationAPI.Publications.list

let handleInvestigationSubCommands arcConfiguration investigationVerb =
    match investigationVerb with
    | InvestigationCommand.Create r                 -> processCommand arcConfiguration InvestigationAPI.create   r
    | InvestigationCommand.Update r                 -> processCommand arcConfiguration InvestigationAPI.update   r
    | InvestigationCommand.Edit                     -> processCommandWithoutArgs arcConfiguration InvestigationAPI.edit
    | InvestigationCommand.Delete r                 -> processCommand arcConfiguration InvestigationAPI.delete   r
    | InvestigationCommand.Person subCommand        -> handleInvestigationContactsSubCommands arcConfiguration (subCommand.GetSubCommand())
    | InvestigationCommand.Publication subCommand   -> handleInvestigationPublicationsSubCommands arcConfiguration (subCommand.GetSubCommand())

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
    | StudyCommand.Get r                    -> processCommand arcConfiguration StudyAPI.get         r
    | StudyCommand.List                     -> processCommandWithoutArgs arcConfiguration StudyAPI.list
    | StudyCommand.Person subCommand        -> handleStudyContactsSubCommands arcConfiguration (subCommand.GetSubCommand())
    | StudyCommand.Publication subCommand   -> handleStudyPublicationsSubCommands arcConfiguration (subCommand.GetSubCommand())
    | StudyCommand.Design subCommand        -> handleStudyDesignSubCommands arcConfiguration (subCommand.GetSubCommand())
    | StudyCommand.Factor subCommand        -> handleStudyFactorSubCommands arcConfiguration (subCommand.GetSubCommand())
    | StudyCommand.Protocol subCommand      -> handleStudyProtocolSubCommands arcConfiguration (subCommand.GetSubCommand())

let handleAssaySubCommands arcConfiguration assayVerb =
    match assayVerb with
    | AssayCommand.Init         r -> processCommand arcConfiguration AssayAPI.init          r
    | AssayCommand.Register     r -> processCommand arcConfiguration AssayAPI.register      r
    | AssayCommand.Add          r -> processCommand arcConfiguration AssayAPI.add           r
    | AssayCommand.Delete       r -> processCommand arcConfiguration AssayAPI.delete        r
    | AssayCommand.Unregister   r -> processCommand arcConfiguration AssayAPI.unregister    r
    | AssayCommand.Remove       r -> processCommand arcConfiguration AssayAPI.remove        r
    | AssayCommand.Update       r -> processCommand arcConfiguration AssayAPI.update        r
    | AssayCommand.Edit         r -> processCommand arcConfiguration AssayAPI.edit          r
    | AssayCommand.Move         r -> processCommand arcConfiguration AssayAPI.move          r
    | AssayCommand.Get          r -> processCommand arcConfiguration AssayAPI.get           r
    | AssayCommand.List           -> processCommandWithoutArgs arcConfiguration AssayAPI.list

let handleConfigurationSubCommands arcConfiguration configurationVerb =
    match configurationVerb with
    | ConfigurationCommand.Edit     r -> processCommand arcConfiguration ConfigurationAPI.edit  r
    | ConfigurationCommand.List     r -> processCommand arcConfiguration ConfigurationAPI.list  r
    | ConfigurationCommand.Set      r -> processCommand arcConfiguration ConfigurationAPI.set   r
    | ConfigurationCommand.Unset    r -> processCommand arcConfiguration ConfigurationAPI.unset r

let handleGitSubCommands arcConfiguration gitVerb =
    match gitVerb with
    | GitCommand.Update     r -> processCommand arcConfiguration GitAPI.update  r

let handleCommand arcConfiguration command =
    match command with
    // Objects
    | Investigation subCommand  -> handleInvestigationSubCommands   arcConfiguration (subCommand.GetSubCommand())
    | Study subCommand          -> handleStudySubCommands           arcConfiguration (subCommand.GetSubCommand())
    | Assay subCommand          -> handleAssaySubCommands           arcConfiguration (subCommand.GetSubCommand())
    | Configuration subcommand  -> handleConfigurationSubCommands   arcConfiguration (subcommand.GetSubCommand())
    | Git subcommand            -> handleGitSubCommands             arcConfiguration (subcommand.GetSubCommand())
    // Verbs
    | Init r                    -> processCommand                   arcConfiguration ArcAPI.init r
    | Synchronize               -> processCommandWithoutArgs        arcConfiguration ArcAPI.synchronize
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

        let verbosity = results.TryGetResult(Verbosity) |> Option.map string


        let arcConfiguration =
            [
                "general.workdir",Some workingDir
                "general.verbosity",verbosity
            ]
            |> List.choose (function | k,Some v -> Some (k,v) | _ -> None)
            |> IniData.fromNameValuePairs
            |> ArcConfiguration.load

        //Testing the configuration reading (Delete when configuration functionality is setup)
        //printfn "load config:"
        //Configuration.loadConfiguration workingDir
        //|> Configuration.flatten
        //|> Seq.iter (fun (a,b) -> printfn "%s=%s" a b)


        handleCommand arcConfiguration (results.GetSubCommand())

        1
    with e ->
        printfn "%s" e.Message
        0
