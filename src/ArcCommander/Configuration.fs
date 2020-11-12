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

    /// Reads the ini config from a string
    let fromText s =
        let parser = Parser.IniDataParser(defaultParserConfiguration)
        parser.Parse(s)

    /// Reads the ini config file at the given location
    let fromFile path =
        let parser = Parser.IniDataParser(defaultParserConfiguration) |> FileIniDataParser
        parser.ReadFile path

    /// Reads the ini config from a string
    let fromNameValuePairs (vs:seq<string*string>) =
        let parser = Parser.IniDataParser(defaultParserConfiguration)
        vs
        |> Seq.fold (fun s (n,v) -> sprintf "%s\n%s,%s" s n v) ""        
        |> parser.Parse

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
    let loadMergedConfiguration workdir =
        let globalConfigPath = getGlobalConfigPath ()
        let localConfigPath = getLocalConfigPath workdir
        if System.IO.File.Exists localConfigPath then
            merge (localConfigPath |> fromFile) (globalConfigPath |> fromFile)
        else
            (globalConfigPath |> fromFile)
