namespace ArcCommander

open IniParser
open IniParser.Model
open IniParser.Parser

module Configuration =

    /// Splits the name of form section.key into section and key
    let splitName (name:string) = 
        if name = "" then
            failwith "name cannot be an empty string"
        elif name.Contains '.' |> not then
            failwith "name must be of form \"section.key\""
        else
            let a = name.Split '.'
            if a.Length <> 2 then failwith "name must be of form \"section.key\""
            else a.[0],a.[1] 

    let getGlobalConfigPath () =
        System.IO.DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory).FullName + "config"

    let getLocalConfigPath workDir =
        System.IO.DirectoryInfo(workDir).FullName + ".arc/config"

    let private defaultParserConfiguration =
        let c = Configuration.IniParserConfiguration()
        c.CommentString <- "#"
        c.CaseInsensitive <- false
        c

    let fromFile path =
        let parser = Parser.IniDataParser(defaultParserConfiguration) |> FileIniDataParser
        parser.ReadFile path

    let toFile path configuration =
        let parser = Parser.IniDataParser(defaultParserConfiguration) |> FileIniDataParser
        parser.WriteFile(path,configuration)
    
    let private tryGetSection sectionName (configuration:IniData) =
        try configuration.Item sectionName |> Some with | _ -> None

    let private tryGetValue key (keydData:KeyDataCollection) =
        try keydData.Item key |> Some with | _ -> None

    let getAllValuesOfKey key (configuration:IniData) =
        configuration.Sections
        |> Seq.choose (fun s ->
            if s.Keys.ContainsKey key then
                Some s.Keys.[key]
            else None
        )

    let tryGetValueOfName (name:string) (configuration:IniData) =
        try
            let section,key =  splitName name 
            tryGetSection section configuration
            |> Option.bind (tryGetValue key)
        with 
        | err -> 
            printfn "Error: Could not retrieve value with given name\n %s" err.Message
            None

    let nameExists (name:string) (configuration:IniData) =
        let section,key = splitName name 
        tryGetSection section configuration
        |> Option.bind (tryGetValue key)
        |> Option.isSome

    let setValue (name:string) (value:string) (configuration:IniData) =
        if nameExists (name:string) (configuration:IniData) then
            let section,key = splitName name 
            configuration.[section].[key] <- value
            configuration
        else
            printfn "name %s does not exist in the config" name
            configuration

    let addValue (name:string) (value:string) (configuration:IniData) =
        if nameExists (name:string) (configuration:IniData) then
            printfn "name %s already exists in the config" name
            configuration
        else
            let section,key = splitName name 
            configuration.[section].AddKey(key,value) |> ignore
            configuration

    let merge (localConfig:IniData) (globalConfig:IniData) = 
        globalConfig.Merge localConfig
        globalConfig

    let flatten (configuration:IniData) =
        configuration.Sections
        |> Seq.collect (fun s ->
            s.Keys
            |> Seq.map (fun kv -> s.SectionName+"."+kv.KeyName,kv.Value)
        )

