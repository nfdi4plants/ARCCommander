module ArcCommander.Program

open ArcCommander
open ArcCommander.ArgumentProcessing
open ArcCommander.Commands
open ArcCommander.APIs
open ArcCommander.ExternalExecutables
open System.Diagnostics
open Argu

/// Runs the given command with the given arguments and configuration. If mandatory arguments are missing, or the "forceEditor" flag is set, opens a prompt asking for additional input
let processCommand (arcConfiguration:ArcConfiguration) commandF (r : ParseResults<'T>) =

    // Collect information from the configuration
    let editor = GeneralConfiguration.getEditor arcConfiguration
    let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
    let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
    let forceEditor = GeneralConfiguration.getForceEditor arcConfiguration

    // Create a collection of all arguments and flags of 'T, including information about whether they were given by the user or not
    let annotatedArguments = groupArguments (r.GetAllResults())

    // Try to collect additional informations
    let arguments = 
        // Opens a command line prompt asking for addtional information if a mandatory argument is missing. Fails if still not given
        if containsMissingMandatoryAttribute annotatedArguments then
            let stillMissingMandatoryArgs,arguments =
                Prompt.createMissingArgumentQuery editor workDir annotatedArguments
            if stillMissingMandatoryArgs then
                failwith "Mandatory arguments were not given either via cli or editor prompt"
            arguments
        // Opens a command line prompt asking for addtional information if the "forceeditor" flag is set.
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

/// Runs the given command with the given configuration
let processCommandWithoutArgs (arcConfiguration:ArcConfiguration) commandF =

    let verbosity = GeneralConfiguration.getVerbosity arcConfiguration

    if verbosity >= 1 then

        printf "Start processing parameterless command"

    if verbosity >= 2 then
        printfn "with the config"
        arcConfiguration
        |> ArcConfiguration.flatten
        |> Seq.iter (fun (a,b) -> printfn "\t%s:%s" a b)

    else printfn ""

    try commandF arcConfiguration
    finally
        if verbosity >= 1 then printfn "Done processing command"

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

    // Try parse the command line arguments
    let parseResults = 
        try
            // Correctly parsed arguments will be threaded into the command handler below
            parser.ParseCommandLine(inputs = argv, raiseOnUsage = true) 
            |> Some
        with
        | e -> 
            // Incorrectly parsed arguments will be threaded into external executable tool handler
            // Here for the first unknown argument in the argument chain, an executable of the same name will be called
            // If this tool exists, try executing it with all the following arguments
            printfn "Could not parse given commands."
            match tryGetUnknownArguments parser argv with
            | Some (executableNameArgs, args) ->
                let executableName = (makeExecutableName executableNameArgs)
                let pi = ProcessStartInfo("cmd", String.concat " " ["/c"; executableName; workingDir])
                printfn $"Try checking if executable with given argument name \"{executableName}\" exists."
                // temporarily add extra directories to PATH
                let folderToAddToPath = getArcFoldersForExtExe workingDir
                let os = IniData.getOs ()
                List.iter (addExtraDirToPath os) folderToAddToPath
                // call external tool
                //writer.
                try Process.Start(pi).WaitForExit(); None
                with e -> printfn "%s" e.Message; None
            // If neither parsing, nor external executable tool search led to success, just return the error message
            | None -> 
                printfn "%s" e.Message
                None

    // Run the according command if command line args can be parsed
    match parseResults with
    | Some results ->
        handleCommand arcConfiguration (results.GetSubCommand())
        1
    | None -> 
        0