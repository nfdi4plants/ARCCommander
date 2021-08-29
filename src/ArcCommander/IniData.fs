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
            failwithf "Name \"%s\" could not be split into section and key, it must be of form \"section.key\"" name

    let splitValues (value:string) =
        value.Split(';')

    /// Returns the path at which the global iniData file is located
    let getGlobalConfigPath () =
        let getFolderPath specialFolder inOwnFolder = 
            System.Environment.GetFolderPath specialFolder
            |> fun x -> 
                if inOwnFolder then 
                    Path.Combine(x,"ArcCommander","config") 
                else 
                    Path.Combine(x,"config")
        let inConfigFolder  = getFolderPath System.Environment.SpecialFolder.ApplicationData        true
        let inConfigFolder2 = getFolderPath System.Environment.SpecialFolder.ApplicationData        false
        let inCache         = getFolderPath System.Environment.SpecialFolder.InternetCache          false
        let inCache2        = getFolderPath System.Environment.SpecialFolder.InternetCache          true
        let inDesktop       = getFolderPath System.Environment.SpecialFolder.DesktopDirectory       false
        let inDesktop2      = getFolderPath System.Environment.SpecialFolder.DesktopDirectory       true
        let inLocal         = getFolderPath System.Environment.SpecialFolder.LocalApplicationData   true
        let inLocal2        = getFolderPath System.Environment.SpecialFolder.LocalApplicationData   false
        let inUser          = getFolderPath System.Environment.SpecialFolder.UserProfile            true
        let inUser2         = getFolderPath System.Environment.SpecialFolder.UserProfile            false
        match System.IO.File.Exists with
        | x when x inConfigFolder   -> inConfigFolder
        | x when x inConfigFolder2  -> inConfigFolder2
        | x when x inUser           -> inUser
        | x when x inUser2          -> inUser2
        | x when x inLocal          -> inLocal
        | x when x inLocal2         -> inLocal2
        | x when x inCache          -> inCache
        | x when x inDesktop        -> inDesktop
        | x when x inDesktop2       -> inDesktop2
        | x when x inCache2         -> inCache2
        | _ -> failwith "No global config file found.\nPlease add the specific config file for your OS to your config folder."
        //Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory,"config")
        //Path.Combine(System.Environment.SpecialFolder.ApplicationData |> System.Environment.GetFolderPath, "arcCommanderConfig")
        //System.Environment.SpecialFolder.UserProfile |> System.Environment.GetFolderPath

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
        if File.Exists path then
            parser.ReadFile path
        else
            fromText ""

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

    let getSectionMap sectionName iniData =
        match tryGetSection sectionName iniData with
        | Some kvs -> 
            kvs
            |> Seq.map (fun kv -> kv.KeyName,kv.Value)
            |> Map.ofSeq
        | None -> Map.empty


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
            printfn "ERROR: Could not retrieve value with given name\n %s" err.Message
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
    let trySetValue (name:string) (value:string) (iniData:IniData) =
        if nameExists (name:string) (iniData:IniData) then
            let section,key = splitName name 
            iniData.[section].[key] <- value
            Some iniData
        else
            printfn "Name %s does not exist in the config" name
            None

    /// If the name is already set in the config, assigns a new value to it
    ///
    /// The name is given as string in form "section.key"
    let setValue (name:string) (value:string) (iniData:IniData) =
        match trySetValue name value iniData with
        | Some ini -> ini
        | None -> iniData

    /// If the name is set in the config, remove it
    ///
    /// The name is given as string in form "section.key"
    let tryRemoveValue (name:string) (iniData:IniData) =
        if nameExists (name:string) (iniData:IniData) then
            let section,key = splitName name 
            iniData.[section].RemoveKey key |> ignore
            Some iniData
        else
            printfn "Name %s does not exist in the config" name
            None

    /// If the name is set in the config, remove it
    ///
    /// The name is given as string in form "section.key"
    let removeValue (name:string) (iniData:IniData) =
        match tryRemoveValue name iniData with
        | Some ini -> ini
        | None -> iniData

    /// If the name is not already set in the config, adds it together with the given value
    ///
    /// The name is given as string in form "section.key"
    let tryAddValue (name:string) (value:string) (iniData:IniData) =
        if nameExists (name:string) (iniData:IniData) then
            printfn "Name %s already exists in the config" name
            Some iniData
        else
            let section,key = splitName name 
            iniData.[section].AddKey(key,value) |> ignore
            None

    /// If the name is not already set in the config, adds it together with the given value
    ///
    /// The name is given as string in form "section.key"
    let addValue (name:string) (value:string) (iniData:IniData) =
        match tryAddValue name value iniData with
        | Some ini -> ini
        | None -> iniData

    /// Merges the setting from two iniDatas. If a name is contained in both files, the value bound to this name in the localConfig is used
    let merge (localIni:IniData) (globalIni:IniData) = 
        globalIni.Merge localIni
        globalIni

    /// Returns a collection of all name value pairs in the config
    ///
    /// The names are given as string in form "section.key"
    let flatten (iniData:IniData) =
        iniData.Sections
        |> Seq.collect (fun s ->
            s.Keys
            |> Seq.map (fun kv -> s.SectionName+"."+kv.KeyName,kv.Value)
        )

    /// Returns a new iniData with the iniData from the second iniData removed from the first 
    let difference (iniData1:IniData) (iniData2) =
        let namesIn2 = flatten iniData2 |> Set.ofSeq
        flatten iniData1 
        |> Seq.filter (namesIn2.Contains >> not)
        |> fromNameValuePairs

    /// Gets the current iniData
    let loadMergedIniData workdir =
        let globalConfigPath = getGlobalConfigPath ()
        let localConfigPath = getLocalConfigPath workdir
        if System.IO.File.Exists localConfigPath then
            merge (localConfigPath |> fromFile) (globalConfigPath |> fromFile)
        else
            (globalConfigPath |> fromFile) 

    /// Set the given value for the key in the ini file, overwriting a possibly existing value
    let setValueInIniPath path name value = 
        let iniData = path |> fromFile
        match trySetValue name value iniData with
        | Some ini -> ini
        | None -> addValue name value iniData
        |> toFile path