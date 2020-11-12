namespace ArcCommander

open System.IO

open IniParser
open IniParser.Model
open IniParser.Parser

/// Functions for accessing and manipulating the arc configuration files
module Configuration =

    /// Splits the name of form "section.key" into section and key
    let splitName (name:string) = 
        let m = System.Text.RegularExpressions.Regex.Match(name,@"(?<!\.\w*)(?<section>\w+)\.(?<key>\w+)(?!\w*\.)")
        if m.Success then
            m.Groups.[1].Value,m.Groups.[2].Value
        else 
            failwithf "name \"%s\" could not be split into section and key, it must be of form \"section.key\"" name

    let splitValues (value:string) =
        value.Split(';')

    /// Returns the path at which the global configuration file is located
    let getGlobalConfigPath () =
        Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory,"config")

    /// Returns the path at which the local configuration file for this specific path is located
    let getLocalConfigPath workDir =
        Path.Combine(workDir,".arc/config")

    let defaultParserConfiguration =
        let c = Configuration.IniParserConfiguration()
        c.CommentString <- "#"
        c.CaseInsensitive <- false
        c

    /// Reads the ini config file at the given location
    let fromFile path =
        let parser = Parser.IniDataParser(defaultParserConfiguration) |> FileIniDataParser
        parser.ReadFile path

    /// Writes the configuration as an ini file to the given location
    let toFile path configuration =
        let parser = Parser.IniDataParser(defaultParserConfiguration) |> FileIniDataParser
        parser.WriteFile(path,configuration)
    
    /// If a section with the given name exists in the configuration, returns its keyValue pairs c
    let tryGetSection sectionName (configuration:IniData) =
        try configuration.Item sectionName |> Some with | _ -> None

    /// If the given key exists in the section (keyData) return its value
    let tryGetValue key (keydData:KeyDataCollection) =
        try keydData.Item key |> Some with | _ -> None

    /// Any given key can be placed once per section
    ///
    /// Returns the values assigned to a given key across all sections
    let getAllValuesOfKey key (configuration:IniData) =
        configuration.Sections
        |> Seq.choose (fun s ->
            if s.Keys.ContainsKey key then
                Some s.Keys.[key]
            else None
        )

    /// Returns the value assigned to a specific name (section+key)
    ///
    /// The name is given as string in form "section.key"
    let tryGetValueByName (name:string) (configuration:IniData) =
        try
            let section,key =  splitName name 
            tryGetSection section configuration
            |> Option.bind (tryGetValue key)
        with 
        | err -> 
            printfn "Error: Could not retrieve value with given name\n %s" err.Message
            None

    /// Returns true if the name (section+key) is set in the configuration
    ///
    /// The name is given as string in form "section.key"
    let nameExists (name:string) (configuration:IniData) =
        let section,key = splitName name 
        tryGetSection section configuration
        |> Option.bind (tryGetValue key)
        |> Option.isSome

    /// If the name is already set in the config, assigns a new value to it
    ///
    /// The name is given as string in form "section.key"
    let setValue (name:string) (value:string) (configuration:IniData) =
        if nameExists (name:string) (configuration:IniData) then
            let section,key = splitName name 
            configuration.[section].[key] <- value
            configuration
        else
            printfn "name %s does not exist in the config" name
            configuration

    /// If the name is not already set in the config, adds it together with the given value
    ///
    /// The name is given as string in form "section.key"
    let addValue (name:string) (value:string) (configuration:IniData) =
        if nameExists (name:string) (configuration:IniData) then
            printfn "name %s already exists in the config" name
            configuration
        else
            let section,key = splitName name 
            configuration.[section].AddKey(key,value) |> ignore
            configuration

    /// Merges the setting from two configurations. If a name is contained in both files, the value bound to this name in the localConfig is used
    let merge (localConfig:IniData) (globalConfig:IniData) = 
        globalConfig.Merge localConfig
        globalConfig

    /// Returns a collection of all name value pairs in the config
    ///
    /// The names are given as string in form "section.key"
    let flatten (configuration:IniData) =
        configuration.Sections
        |> Seq.collect (fun s ->
            s.Keys
            |> Seq.map (fun kv -> s.SectionName+"."+kv.KeyName,kv.Value)
        )

    /// Gets the current configuration
    let loadArcConfiguration workdir =
        let globalConfigPath = getGlobalConfigPath ()
        let localConfigPath = getLocalConfigPath workdir
        if System.IO.File.Exists localConfigPath then
            merge (localConfigPath |> fromFile) (globalConfigPath |> fromFile)
        else
            (globalConfigPath |> fromFile)

    /// Functions for retrieving general settings from the configuration
    module GeneralConfiguration =

        /// Returns the path to the text editor used for querying user input
        let getEditor configuration =           
            tryGetValueByName "general.editor" configuration

    /// Functions for retrieving Assay related information from the configuration
    module AssayConfiguration =

        let tryGetAssayFileName configuration =
            tryGetValueByName "isamodel.assayfilename" configuration

        /// Returns the full path of the assay file
        let tryGetAssayFilePath workingDir assayIdentifier configuration =
            let assayFileName = tryGetAssayFileName configuration
            let rootFolder = tryGetValueByName "assay.rootfolder" configuration
            match assayFileName,rootFolder with
            | Some f, Some r -> 
                Path.Combine([|workingDir;r;assayIdentifier;f|])
                |> Some
            | _ -> None


        /// Returns the full path of the files associated with the assay
        let getFilePaths workingDir assayIdentifier configuration =
            let fileNames = tryGetValueByName "assay.files" configuration
            let rootFolder = tryGetValueByName "assay.rootfolder" configuration
            match fileNames,rootFolder with
            | Some vs, Some r -> 
                vs
                |> splitValues
                |> Array.map (fun v ->
                    Path.Combine([|workingDir;r;assayIdentifier;v|])
                )                
            | _ -> [||]

        /// Returns the full path of the folders associated with the assay
        let getFolderPaths workingDir assayIdentifier configuration =
            let folderNames = tryGetValueByName "assay.folders" configuration
            let rootFolder = tryGetValueByName "assay.rootfolder" configuration
            match folderNames,rootFolder with
            | Some vs, Some r -> 
                vs
                |> splitValues
                |> Seq.map (fun v ->
                    Path.Combine([|workingDir;r;assayIdentifier;v|])
                )                
            | _ -> Seq.empty

    /// Functions for retrieving investigation related information from the configuration
    module InvestigationConfiguration =

        let tryGetInvestigationFileName configuration =
            tryGetValueByName "isamodel.investigationfilename" configuration

        /// Returns the full path of the investigation file
        let tryGetInvestigationFilePath workingDir configuration =
            match tryGetInvestigationFileName configuration with
            | Some i -> 
                Path.Combine(workingDir,i)
                |> Some
            | _ -> None

    module ArcConfiguration =
        
        /// Returns the full paths of the rootfolders
        let tryGetRootFolderPaths workingDir configuration =
            getAllValuesOfKey "rootfolder" configuration
            |> Seq.map (fun f -> Path.Combine(workingDir,f))