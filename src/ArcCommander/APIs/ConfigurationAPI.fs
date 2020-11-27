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
            let path = IniData.getGlobalConfigPath()
            let iniData = path |> IniData.fromFile           
            iniData
            |> Prompt.createIniDataQuery editorPath workdir 
            |> fun newIni -> IniData.difference newIni iniData
            |> fun differences -> updateWithDifferences differences iniData
            |> IniData.toFile path
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
            let iniData = IniData.loadMergedIniData workdir
            iniData
            |> Prompt.createIniDataQuery editorPath workdir 
            |> fun newIni -> IniData.difference newIni iniData
            |> fun differences -> updateWithDifferences differences localIni
            |> IniData.toFile path
        // If local and global flags are set, open prompt with merged settings and set both local and global settings files to the user input
        | true,true ->
            let globalPath = IniData.getGlobalConfigPath()
            let globalIni = globalPath |> IniData.fromFile    
            let localPath = IniData.getLocalConfigPath workdir
            let localIni = IniData.fromFile localPath
            let iniData = IniData.loadMergedIniData workdir
            iniData
            |> Prompt.createIniDataQuery editorPath workdir 
            //|> fun newIni -> IniData.difference newIni iniData // If this line is uncommented, only the changes from the user input are applied to the local and global files
            |> fun differences -> 
                updateWithDifferences differences globalIni
                |> IniData.toFile globalPath
                updateWithDifferences differences localIni
                |> IniData.toFile localPath

    /// Lists all current settings specified in the configuration
    let list (arcConfiguration:ArcConfiguration) (configurationArgs:Map<string,Argument>) =
        match containsFlag "Global" configurationArgs, containsFlag "Local" configurationArgs with
        // If only global flag is set, only global settings are listed
        | true,false ->
            IniData.getGlobalConfigPath()
            |> IniData.fromFile
        // If only local flag is set, only local settings are listed
        | false,true ->
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> IniData.fromFile
        // If no flag or both flags are set, merged settings are listed
        | true,true | false,false  -> 
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.loadMergedIniData workDir
        |> IniData.flatten
        |> Seq.iter (fun (n,v) -> printfn "%s:%s" n v)

    /// Set the given name-value pair for the called config
    let set (arcConfiguration:ArcConfiguration) (configurationArgs:Map<string,Argument>) =

        let name = getFieldValueByName "Name" configurationArgs
        let value = getFieldValueByName "Value" configurationArgs

        let setValueInIniPath path = 
            let iniData = path |> IniData.fromFile
            match IniData.trySetValue name value iniData with
            | Some ini -> ini
            | None -> IniData.addValue name value iniData
            |> IniData.toFile path

        match containsFlag "Global" configurationArgs, containsFlag "Local" configurationArgs with
        // If only global flag is set, the setting is set globally
        | true,false ->
            IniData.getGlobalConfigPath()
            |> setValueInIniPath
        // If both global and local flags are set, the setting is set both in the local and the global config file
        | true,true ->
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> setValueInIniPath
            IniData.getGlobalConfigPath()
            |> setValueInIniPath
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
            IniData.getGlobalConfigPath()
            |> unsetValueInIniPath
        // If both global and local flags are set, the setting is unset both in the local and the global config file
        | true,true ->
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> unsetValueInIniPath
            IniData.getGlobalConfigPath()
            |> unsetValueInIniPath
        // If only local or no flag is set, the setting is set only in the local config file
        | false,false | false,true  -> 
            let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
            IniData.getLocalConfigPath workDir
            |> unsetValueInIniPath