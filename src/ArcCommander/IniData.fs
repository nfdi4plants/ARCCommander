namespace ArcCommander

open System.IO

open IniParser
open IniParser.Model
open IniParser.Parser


/// Functions for accessing and manipulating the arc iniData files
module IniData =

    /// Splits the name of form "section.key" into section and key
    let splitName (name:string) = 
        let m = System.Text.RegularExpressions.Regex.Match(name,@"(?<!\.\w*)(?<section>\w+)\.(?<key>\w+)(?!\w*\.)")
        if m.Success then
            m.Groups.[1].Value,m.Groups.[2].Value
        else 
            failwithf "name \"%s\" could not be split into section and key, it must be of form \"section.key\"" name

    let splitValues (value:string) =
        value.Split(';')

    /// Returns the path at which the global iniData file is located
    let getGlobalConfigPath () =
        Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory,"config")

    /// Returns the path at which the local iniData file for this specific path is located
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
        let sd = SectionDataCollection()
        vs
        |> Seq.groupBy (fst >> splitName >> fst)
        |> Seq.iter (fun (sectionName,nvs) ->
            let section = SectionData(sectionName)
            nvs
            |> Seq.iter (fun (n,v) -> 
                section.Keys.AddKey(splitName n |> snd,v) |> ignore           
            ) 
            sd.Add(section) |> ignore
        )
        IniData(sd)

    /// Writes the iniData as an ini file to the given location
    let toFile path iniData =
        let parser = Parser.IniDataParser(defaultParserConfiguration) |> FileIniDataParser
        parser.WriteFile(path,iniData)
    
    /// If a section with the given name exists in the iniData, returns its keyValue pairs c
    let tryGetSection sectionName (iniData:IniData) =
        try iniData.Item sectionName |> Some with | _ -> None

    /// If the given key exists in the section (keyData) return its value
    let tryGetValue key (keydData:KeyDataCollection) =
        try keydData.Item key |> Some with | _ -> None

   
    /// Any given key can be placed once per section
    ///
    /// Returns the values assigned to a given key across all sections
    let getAllValuesOfKey key (iniData:IniData) =
        iniData.Sections
        |> Seq.choose (fun s ->
            if s.Keys.ContainsKey key then
                Some s.Keys.[key]
            else None
        )

    /// Returns the value assigned to a specific name (section+key)
    ///
    /// The name is given as string in form "section.key"
    let tryGetValueByName (name:string) (iniData:IniData) =
        try
            let section,key =  splitName name 
            tryGetSection section iniData
            |> Option.bind (tryGetValue key)
        with 
        | err -> 
            printfn "Error: Could not retrieve value with given name\n %s" err.Message
            None

    /// Returns true if the name (section+key) is set in the iniData
    ///
    /// The name is given as string in form "section.key"
    let nameExists (name:string) (iniData:IniData) =
        let section,key = splitName name 
        tryGetSection section iniData
        |> Option.bind (tryGetValue key)
        |> Option.isSome

    /// If the name is already set in the config, assigns a new value to it
    ///
    /// The name is given as string in form "section.key"
    let setValue (name:string) (value:string) (iniData:IniData) =
        if nameExists (name:string) (iniData:IniData) then
            let section,key = splitName name 
            iniData.[section].[key] <- value
            iniData
        else
            printfn "name %s does not exist in the config" name
            iniData

    /// If the name is not already set in the config, adds it together with the given value
    ///
    /// The name is given as string in form "section.key"
    let addValue (name:string) (value:string) (iniData:IniData) =
        if nameExists (name:string) (iniData:IniData) then
            printfn "name %s already exists in the config" name
            iniData
        else
            let section,key = splitName name 
            iniData.[section].AddKey(key,value) |> ignore
            iniData

    /// Merges the setting from two iniDatas. If a name is contained in both files, the value bound to this name in the localConfig is used
    let merge (localConfig:IniData) (globalConfig:IniData) = 
        globalConfig.Merge localConfig
        globalConfig

    /// Returns a collection of all name value pairs in the config
    ///
    /// The names are given as string in form "section.key"
    let flatten (iniData:IniData) =
        iniData.Sections
        |> Seq.collect (fun s ->
            s.Keys
            |> Seq.map (fun kv -> s.SectionName+"."+kv.KeyName,kv.Value)
        )

    /// Gets the current iniData
    let loadMergedIniData workdir =
        let globalConfigPath = getGlobalConfigPath ()
        let localConfigPath = getLocalConfigPath workdir
        if System.IO.File.Exists localConfigPath then
            merge (localConfigPath |> fromFile) (globalConfigPath |> fromFile)
        else
            (globalConfigPath |> fromFile)
