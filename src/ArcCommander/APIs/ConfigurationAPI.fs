namespace ArcCommander.APIs

open ArcCommander
open ArgumentProcessing
open ARCtrl.NET

open ArcCommander.CLIArguments
/// ArcCommander Configuration API functions that get executed by the configuration focused subcommand verbs
module ConfigurationAPI =
    
    /// Opens the configuration file specified with (global or local) with the text editor set in globalArgs.
    let edit (arcConfiguration : ArcConfiguration) (configurationArgs : ArcParseResults<ConfigurationEditArgs>) =

        let log = Logging.createLogger "ConfigurationEditLog"

        let editorPath = GeneralConfiguration.getEditor arcConfiguration
        let workdir = GeneralConfiguration.getWorkDirectory arcConfiguration

        /// The differences obtained from user input are applied on the iniData of interest
        let updateWithDifferences differences iniData =
            differences
            |> IniData.flatten
            |> Seq.fold (fun iniData (n,v) -> 
                match IniData.trySetValue n v iniData with
                | Some ini -> ini
                | None -> IniData.addValue n v iniData            
            ) iniData

        match configurationArgs.ContainsFlag ConfigurationEditArgs.Global, configurationArgs.ContainsFlag ConfigurationEditArgs.Local with
        // If only global flag is set, open prompt only with global settings and apply changes on global settings
        | true,false ->
            let path = IniData.tryGetGlobalConfigPath()
            let iniData = 
                match path with
                | Some p    -> p |> IniData.fromFile
                | None      -> log.Warn("No config file found. Load default config instead."); ArcConfiguration.GetDefault()
            match path with
            | Some p ->
                iniData
                |> Prompt.createIniDataQuery editorPath
                |> fun newIni -> IniData.difference newIni iniData
                |> fun differences -> updateWithDifferences differences iniData
                |> IniData.toFile p
            | None ->
                log.Error("No folder for global config file known for this environment. Config file settings cannot be saved.")
        // If only local flag is set, open prompt only with local settings and apply changes on local settings
        | false,true ->
            let path = IniData.getLocalConfigPath workdir
            let iniData = path |> IniData.fromFile           
            iniData
            |> Prompt.createIniDataQuery editorPath
            |> fun newIni -> IniData.difference newIni iniData
            |> fun differences -> updateWithDifferences differences iniData
            |> IniData.toFile path
        // If no flag is set, open prompt with merged settings and apply changes on local settings
        | false,false ->
            let path = IniData.getLocalConfigPath workdir
            let localIni = IniData.fromFile path
            let iniData = IniData.tryLoadMergedIniData workdir
            match iniData with
            | Some inidat ->
                inidat
                |> Prompt.createIniDataQuery editorPath
                |> fun newIni -> IniData.difference newIni inidat
                |> fun differences -> updateWithDifferences differences localIni
                |> IniData.toFile path
            | None -> 
                log.Error("No folder for global config file known for this environment. Config file settings cannot be saved.")
        // If local and global flags are set, open prompt with merged settings and set both local and global settings files to the user input
        | true,true ->
            let globalPath = IniData.tryGetGlobalConfigPath()
            let globalIni = 
                match globalPath with
                | Some p    -> IniData.fromFile p
                | None      -> log.Warn("No config file found. Load default config instead."); ArcConfiguration.GetDefault()
            let localPath = IniData.getLocalConfigPath workdir
            let localIni = IniData.fromFile localPath
            let iniData = IniData.tryLoadMergedIniData workdir
            match iniData with
            | Some inidat ->
                inidat
                |> Prompt.createIniDataQuery editorPath
                //|> fun newIni -> IniData.difference newIni iniData // If this line is uncommented, only the changes from the user input are applied to the local and global files
                |> fun differences -> 
                    updateWithDifferences differences globalIni
                    |> IniData.toFile globalPath.Value
                    updateWithDifferences differences localIni
                    |> IniData.toFile localPath
            | None -> 
                log.Error("No folder for global config file known for this environment. Config file settings cannot be saved.")

    /// Lists all current settings specified in the configuration.
    let list (arcConfiguration : ArcConfiguration) (configurationArgs : ArcParseResults<ConfigurationListArgs>) =

        let log = Logging.createLogger "ConfigurationListLog"

        match configurationArgs.ContainsFlag ConfigurationListArgs.Global, configurationArgs.ContainsFlag ConfigurationListArgs.Local with
        // If only global flag is set, only global settings are listed
        | true,false ->
            match IniData.tryGetGlobalConfigPath () with
            | Some p    -> IniData.fromFile p
            | None      -> log.Warn("No config file found. Load default config instead."); ArcConfiguration.GetDefault()
        // If only local flag is set, only local settings are listed
        | false,true ->
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> IniData.fromFile
        // If no flag or both flags are set, merged settings are listed
        | true,true | false,false  -> 
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            match IniData.tryLoadMergedIniData workDir with
            | Some inidat   -> inidat
            | None          -> log.Warn("No config file found. Load default config instead."); ArcConfiguration.GetDefault()
        |> IniData.flatten
        |> Seq.iter (fun (n,v) -> log.Debug($"{n}:{v}"))

    /// Set the given name-value pair for the called config.
    let set (arcConfiguration : ArcConfiguration) (configurationArgs : ArcParseResults<ConfigurationSetArgs>) =

        let log = Logging.createLogger "ConfigurationSetLog"

        let name = configurationArgs.GetFieldValue ConfigurationSetArgs.Name
        let value = configurationArgs.GetFieldValue ConfigurationSetArgs.Value

        let setValueInIniPath path = IniData.setValueInIniPath path name value

        match configurationArgs.ContainsFlag ConfigurationSetArgs.Global, configurationArgs.ContainsFlag ConfigurationSetArgs.Local with
        // If only global flag is set, the setting is set globally
        | true,false ->
            match IniData.tryGetGlobalConfigPath () with
            | Some p    -> setValueInIniPath p
            | None      -> log.Error("No folder for global config file known for this environment. Config file settings cannot be saved.")
        // If both global and local flags are set, the setting is set both in the local and the global config file
        | true,true ->
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> setValueInIniPath
            match IniData.tryGetGlobalConfigPath () with
            | Some p    -> setValueInIniPath p
            | None      -> log.Error("No folder for global config file known for this environment. Config file settings cannot be saved.")
        // If only local or no flag is set, the setting is set only in the local config file
        | false,false | false,true  -> 
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> setValueInIniPath

    /// Set the given name-value pair for the called config.
    let unset (arcConfiguration : ArcConfiguration) (configurationArgs : ArcParseResults<ConfigurationUnsetArgs>) =

        let log = Logging.createLogger "ConfigurationUnsetLog"

        let name = configurationArgs.GetFieldValue ConfigurationUnsetArgs.Name

        let unsetValueInIniPath path = 
            path 
            |> IniData.fromFile
            |> IniData.removeValue name
            |> IniData.toFile path

        match configurationArgs.ContainsFlag ConfigurationUnsetArgs.Global, configurationArgs.ContainsFlag ConfigurationUnsetArgs.Local with
        // If only global flag is set, the setting is unset globally
        | true,false ->
            match IniData.tryGetGlobalConfigPath () with
            | Some p    -> unsetValueInIniPath p
            | None      -> log.Error("No folder for global config file known for this environment. Config file settings cannot be saved.")
        // If both global and local flags are set, the setting is unset both in the local and the global config file
        | true,true ->
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> unsetValueInIniPath
            match IniData.tryGetGlobalConfigPath () with
            | Some p    -> unsetValueInIniPath p
            | None      -> log.Error("No folder for global config file known for this environment. Config file settings cannot be saved.")
        // If only local or no flag is set, the setting is set only in the local config file
        | false,false | false,true  -> 
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> unsetValueInIniPath


    /// Transfer git user metadata from arc config to git config.
    let setGitUser (arcConfiguration : ArcConfiguration) (configurationArgs : ArcParseResults<ConfigurationSetGitUserArgs>) =

        let log = Logging.createLogger "ConfigurationSetGitUserLog"

        log.Info("Start Config SetGitUser")

        let nameOption = 
            match configurationArgs.TryGetFieldValue ConfigurationSetGitUserArgs.Name with
            | Some name -> 
                log.Trace("Retrieved user name from given argument.")
                Some name
            | None -> 
                log.Info("Could not retrieve user name from argument. Try to retrieve it from ARC config.")
                GeneralConfiguration.tryGetGitName arcConfiguration
            
        let emailOption = 
            match configurationArgs.TryGetFieldValue ConfigurationSetGitUserArgs.Email with
            | Some name -> 
                log.Trace("Retrieved user email from given argument.")
                Some name
            | None -> 
                log.Info("Could not retrieve user email from argument. Try to retrieve it from ARC config.")
                GeneralConfiguration.tryGetGitEmail arcConfiguration  

        match nameOption, emailOption with
        | Some name, Some email ->

            if configurationArgs.ContainsFlag ConfigurationSetGitUserArgs.Global then 
                GitHelper.setGlobalName name
                GitHelper.setGlobalEmail email
                
            if (configurationArgs.ContainsFlag ConfigurationSetGitUserArgs.Global |> not) || configurationArgs.ContainsFlag ConfigurationSetGitUserArgs.Local then
                let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
                GitHelper.setLocalName workDir name
                GitHelper.setLocalEmail workDir email

            log.Info("Finished setting git user information.")

        | _, _ -> 
            log.Error("Git user metadata neither present in the ARC config nor given by argument. Consider first running \"arc auth\", setting the user name and email by running \"arc config set\" or rerunning this command and specifying both name and email.")