namespace ArcCommander.APIs

open System
open ArcCommander
open ArgumentProcessing

/// ArcCommander Configuration API functions that get executed by the configuration focused subcommand verbs
module ConfigurationAPI =     
    
    /// Opens the configuration file specified with (global or local) with the text editor set in globalArgs.
    let edit (arcConfiguration:ArcConfiguration) (configurationArgs:Map<string,Argument>) =

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

        match containsFlag "Global" configurationArgs, containsFlag "Local" configurationArgs with
        // If only global flag is set, open prompt only with global settings and apply changes on global settings
        | true,false ->
            let path = IniData.tryGetGlobalConfigPath()
            let iniData = 
                match path with
                | Some p    -> p |> IniData.fromFile
                | None      -> printfn "WARNING: No config file found. Load default config instead."; ArcConfiguration.GetDefault()
            match path with
            | Some p ->
                iniData
                |> Prompt.createIniDataQuery editorPath workdir 
                |> fun newIni -> IniData.difference newIni iniData
                |> fun differences -> updateWithDifferences differences iniData
                |> IniData.toFile p
            | None ->
                printfn "WARNING: No folder for global config file known for this environment. Config file settings cannot be saved."
        // If only local flag is set, open prompt only with local settings and apply changes on local settings
        | false,true ->
            let path = IniData.getLocalConfigPath workdir
            let iniData = path |> IniData.fromFile           
            iniData
            |> Prompt.createIniDataQuery editorPath workdir 
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
                |> Prompt.createIniDataQuery editorPath workdir 
                |> fun newIni -> IniData.difference newIni inidat
                |> fun differences -> updateWithDifferences differences localIni
                |> IniData.toFile path
            | None -> 
                printfn "WARNING: No folder for global config file known for this environment. Config file settings cannot be saved."
        // If local and global flags are set, open prompt with merged settings and set both local and global settings files to the user input
        | true,true ->
            let globalPath = IniData.tryGetGlobalConfigPath()
            let globalIni = 
                match globalPath with
                | Some p    -> IniData.fromFile p
                | None      -> printfn "WARNING: No config file found. Load default config instead."; ArcConfiguration.GetDefault()
            let localPath = IniData.getLocalConfigPath workdir
            let localIni = IniData.fromFile localPath
            let iniData = IniData.tryLoadMergedIniData workdir
            match iniData with
            | Some inidat ->
                inidat
                |> Prompt.createIniDataQuery editorPath workdir 
                //|> fun newIni -> IniData.difference newIni iniData // If this line is uncommented, only the changes from the user input are applied to the local and global files
                |> fun differences -> 
                    updateWithDifferences differences globalIni
                    |> IniData.toFile globalPath.Value
                    updateWithDifferences differences localIni
                    |> IniData.toFile localPath
            | None -> 
                printfn "WARNING: No folder for global config file known for this environment. Config file settings cannot be saved."

    /// Lists all current settings specified in the configuration
    let list (arcConfiguration:ArcConfiguration) (configurationArgs:Map<string,Argument>) =
        match containsFlag "Global" configurationArgs, containsFlag "Local" configurationArgs with
        // If only global flag is set, only global settings are listed
        | true,false ->
            match IniData.tryGetGlobalConfigPath () with
            | Some p    -> IniData.fromFile p
            | None      -> printfn "WARNING: No config file found. Load default config instead."; ArcConfiguration.GetDefault()
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
            | None          -> printfn "WARNING: No config file found. Load default config instead."; ArcConfiguration.GetDefault()
        |> IniData.flatten
        |> Seq.iter (fun (n,v) -> printfn "%s:%s" n v)

    /// Set the given name-value pair for the called config
    let set (arcConfiguration:ArcConfiguration) (configurationArgs:Map<string,Argument>) =

        let name = getFieldValueByName "Name" configurationArgs
        let value = getFieldValueByName "Value" configurationArgs

        let setValueInIniPath path = IniData.setValueInIniPath path name value

        match containsFlag "Global" configurationArgs, containsFlag "Local" configurationArgs with
        // If only global flag is set, the setting is set globally
        | true,false ->
            match IniData.tryGetGlobalConfigPath () with
            | Some p    -> setValueInIniPath p
            | None      -> printfn "WARNING: No folder for global config file known for this environment. Config file settings cannot be saved."
        // If both global and local flags are set, the setting is set both in the local and the global config file
        | true,true ->
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> setValueInIniPath
            match IniData.tryGetGlobalConfigPath () with
            | Some p    -> setValueInIniPath p
            | None      -> printfn "WARNING: No folder for global config file known for this environment. Config file settings cannot be saved."
        // If only local or no flag is set, the setting is set only in the local config file
        | false,false | false,true  -> 
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> setValueInIniPath

    /// Set the given name-value pair for the called config
    let unset (arcConfiguration:ArcConfiguration) (configurationArgs:Map<string,Argument>) =

        let name = getFieldValueByName "Name" configurationArgs

        let unsetValueInIniPath path = 
            path 
            |> IniData.fromFile
            |> IniData.removeValue name
            |> IniData.toFile path

        match containsFlag "Global" configurationArgs, containsFlag "Local" configurationArgs with
        // If only global flag is set, the setting is unset globally
        | true,false ->
            match IniData.tryGetGlobalConfigPath () with
            | Some p    -> unsetValueInIniPath p
            | None      -> printfn "WARNING: No folder for global config file known for this environment. Config file settings cannot be saved."
        // If both global and local flags are set, the setting is unset both in the local and the global config file
        | true,true ->
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> unsetValueInIniPath
            match IniData.tryGetGlobalConfigPath () with
            | Some p    -> unsetValueInIniPath p
            | None      -> printfn "WARNING: No folder for global config file known for this environment. Config file settings cannot be saved."
        // If only local or no flag is set, the setting is set only in the local config file
        | false,false | false,true  -> 
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> unsetValueInIniPath