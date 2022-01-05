namespace ArcCommander

open System
open System.IO
open System.Runtime.InteropServices

open IniParser
open IniParser.Model

type OS =
| Windows
| Unix

/// Functions for accessing and manipulating the arc iniData files.
module IniData =

    /// Splits the name of form "section.key" into section and key.
    let splitName (name : string) = 
        let log = Logging.createLogger "IniDataSplitNameLog"
        let m = Text.RegularExpressions.Regex.Match(name, @"(?<!\.\w*)(?<section>\w+)\.(?<key>\w+)(?!\w*\.)")
        if m.Success then
            m.Groups.[1].Value,m.Groups.[2].Value
        else 
            log.Error(sprintf "Name \"%s\" could not be split into section and key, it must be of form \"section.key\"" name)
            raise (Exception(""))

    let splitValues (value : string) = value.Split(';')

    /// Returns the operating system.
    let getOs () =
        let log = Logging.createLogger "IniDataGetOsLog"

        match RuntimeInformation.IsOSPlatform with
        | _ when RuntimeInformation.IsOSPlatform(OSPlatform.Windows)    -> Windows
        | _ when 
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)             -> Unix
        | _                                                             -> 
            log.Error($"ERROR: OS not supported. Only Windows, MacOS and Linux are supported.")
            raise (Exception(""))

    /// Creates a default config file in the user's config folder (AppData\Local in Windows, ~/config in Unix).
    let createDefault () =
        let os = getOs ()
        let srcFilepath = 
            let appDir = Threading.Thread.GetDomain().BaseDirectory
            let osConfFolder = 
                match os with
                | Windows   -> "config_win"
                | Unix      -> "config_unix"
            Path.Combine(appDir, osConfFolder, "config")
        let configFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify)
        let destDirPath = Path.Combine(configFolder, "DataPLANT", "ArcCommander")
        Directory.CreateDirectory(destDirPath) |> ignore
        let destFilepath = Path.Combine(destDirPath, "ArcCommander.config")
        File.Copy(srcFilepath, destFilepath)

    /// Returns the path at which the global iniData file is located. If no global file is found, creates one in the user's config folder (AppData\Local in Windows, ~/config in Unix).
    let tryGetGlobalConfigPath () =
        let log = Logging.createLogger "IniDataTryGetGlobalConfigPathLog"
        // most of this part only remains for legacy reasons. Config file should not be downloaded and placed by the user (as before) but installed by the ArcCommander itself.
        let getFolderPath specialFolder inOwnFolder inCompanyFolder newName = 
            Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.DoNotVerify)
            |> fun x -> 
                if inOwnFolder then 
                    Path.Combine(x, "ArcCommander", "config") 
                elif inCompanyFolder && (not newName) then
                    Path.Combine(x, "DataPLANT", "ArcCommander", "config")
                elif inCompanyFolder && newName then
                    Path.Combine(x, "DataPLANT", "ArcCommander", "ArcCommander.config")
                else 
                    Path.Combine(x, "config")
        let inConfigFolder  = getFolderPath Environment.SpecialFolder.ApplicationData       false true  true
        let inConfigFolder2 = getFolderPath Environment.SpecialFolder.ApplicationData       false true  false
        let inConfigFolder3 = getFolderPath Environment.SpecialFolder.ApplicationData       true  false false
        let inConfigFolder4 = getFolderPath Environment.SpecialFolder.ApplicationData       false false false
        let inCache         = getFolderPath Environment.SpecialFolder.InternetCache         false false false
        let inCache2        = getFolderPath Environment.SpecialFolder.InternetCache         true  false false
        let inDesktop       = getFolderPath Environment.SpecialFolder.DesktopDirectory      false false false
        let inDesktop2      = getFolderPath Environment.SpecialFolder.DesktopDirectory      true  false false
        let inLocal         = getFolderPath Environment.SpecialFolder.LocalApplicationData  true  false false
        let inLocal2        = getFolderPath Environment.SpecialFolder.LocalApplicationData  false false false
        let inUser          = getFolderPath Environment.SpecialFolder.UserProfile           true  false false
        let inUser2         = getFolderPath Environment.SpecialFolder.UserProfile           false false false
        try
            match File.Exists with
            | x when x inConfigFolder   -> inConfigFolder
            | x when x inConfigFolder2  -> inConfigFolder2
            | x when x inConfigFolder3  -> inConfigFolder3
            | x when x inConfigFolder4  -> inConfigFolder4
            | x when x inUser           -> inUser
            | x when x inUser2          -> inUser2
            | x when x inLocal          -> inLocal
            | x when x inLocal2         -> inLocal2
            | x when x inCache          -> inCache
            | x when x inDesktop        -> inDesktop
            | x when x inDesktop2       -> inDesktop2
            | x when x inCache2         -> inCache2
            | _                         -> createDefault (); inConfigFolder
            |> Some
        with e -> 
            log.Error($"ERROR: tryGetGlobalConfigPath failed with: {e.Message}")
            None
        //| _ -> failwith "ERROR: No global config file found. Initiation of default config file not possible.\nPlease add the specific config file for your OS to your config folder."
        //Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory,"config")
        //Path.Combine(System.Environment.SpecialFolder.ApplicationData |> System.Environment.GetFolderPath, "arcCommanderConfig")
        //System.Environment.SpecialFolder.UserProfile |> System.Environment.GetFolderPath

    /// Returns the path at which the local iniData file for this specific path is located
    let getLocalConfigPath workDir =
        Path.Combine(workDir, ".arc/config")

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
    let fromNameValuePairs (vs : seq<string * string>) =
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
        parser.WriteFile(path, iniData)

    /// If a section with the given name exists in the iniData, returns its keyValue pairs c
    let tryGetSection sectionName (iniData : IniData) =
        try iniData.Item sectionName |> Some with | _ -> None

    let getSectionMap sectionName iniData =
        match tryGetSection sectionName iniData with
        | Some kvs -> 
            kvs
            |> Seq.map (fun kv -> kv.KeyName, kv.Value)
            |> Map.ofSeq
        | None -> Map.empty


    /// If the given key exists in the section (keyData) return its value
    let tryGetValue key (keydData : KeyDataCollection) =
        try keydData.Item key |> Some with | _ -> None


    /// Any given key can be placed once per section
    ///
    /// Returns the values assigned to a given key across all sections
    let getAllValuesOfKey key (iniData : IniData) =
        iniData.Sections
        |> Seq.choose (fun s ->
            if s.Keys.ContainsKey key then
                Some s.Keys.[key]
            else None
        )

    /// Returns the value assigned to a specific name (section+key)
    ///
    /// The name is given as string in form "section.key"
    let tryGetValueByName (name : string) (iniData : IniData) =
        let log = Logging.createLogger "IniDataTryGetValueByNameLog"
        try
            let section,key =  splitName name 
            tryGetSection section iniData
            |> Option.bind (tryGetValue key)
        with 
        | err -> 
            log.Error($"ERROR: Could not retrieve value with given name\n {err.Message}")
            None

    /// Returns true if the name (section+key) is set in the iniData
    ///
    /// The name is given as string in form "section.key"
    let nameExists (name : string) (iniData : IniData) =
        let section,key = splitName name 
        tryGetSection section iniData
        |> Option.bind (tryGetValue key)
        |> Option.isSome

    /// If the name is already set in the config, assigns a new value to it
    ///
    /// The name is given as string in form "section.key"
    let trySetValue (name : string) (value : string) (iniData : IniData) =
        let log = Logging.createLogger "IniDataTrySetValueLog"
        if nameExists (name : string) (iniData : IniData) then
            let section,key = splitName name 
            iniData.[section].[key] <- value
            Some iniData
        else
            log.Error($"Name {name} does not exist in the config")
            None

    /// If the name is already set in the config, assigns a new value to it
    ///
    /// The name is given as string in form "section.key"
    let setValue (name : string) (value : string) (iniData : IniData) =
        match trySetValue name value iniData with
        | Some ini -> ini
        | None -> iniData

    /// If the name is set in the config, remove it
    ///
    /// The name is given as string in form "section.key"
    let tryRemoveValue (name : string) (iniData : IniData) =
        let log = Logging.createLogger "IniDataTryRemoveValueLog"
        if nameExists (name : string) (iniData : IniData) then
            let section,key = splitName name 
            iniData.[section].RemoveKey key |> ignore
            Some iniData
        else
            log.Error($"Name {name} does not exist in the config")
            None

    /// If the name is set in the config, remove it
    ///
    /// The name is given as string in form "section.key"
    let removeValue (name : string) (iniData : IniData) =
        match tryRemoveValue name iniData with
        | Some ini -> ini
        | None -> iniData

    /// If the name is not already set in the config, adds it together with the given value
    ///
    /// The name is given as string in form "section.key"
    let tryAddValue (name : string) (value : string) (iniData : IniData) =
        let log = Logging.createLogger "IniDataTryAddValueLog"
        if nameExists (name : string) (iniData : IniData) then
            log.Error($"Name {name} already exists in the config")
            Some iniData
        else
            let section,key = splitName name 
            iniData.[section].AddKey(key,value) |> ignore
            None

    /// If the name is not already set in the config, adds it together with the given value
    ///
    /// The name is given as string in form "section.key"
    let addValue (name : string) (value : string) (iniData : IniData) =
        match tryAddValue name value iniData with
        | Some ini -> ini
        | None -> iniData

    /// Merges the setting from two iniDatas. If a name is contained in both files, the value bound to this name in the localConfig is used
    let merge (localIni : IniData) (globalIni : IniData) = 
        globalIni.Merge localIni
        globalIni

    /// Returns a collection of all name value pairs in the config
    ///
    /// The names are given as string in form "section.key"
    let flatten (iniData : IniData) =
        iniData.Sections
        |> Seq.collect (fun s ->
            s.Keys
            |> Seq.map (fun kv -> s.SectionName+"."+kv.KeyName,kv.Value)
        )

    /// Returns a new iniData with the iniData from the second iniData removed from the first 
    let difference (iniData1 : IniData) (iniData2) =
        let namesIn2 = flatten iniData2 |> Set.ofSeq
        flatten iniData1 
        |> Seq.filter (namesIn2.Contains >> not)
        |> fromNameValuePairs

    /// Gets the current iniData
    let tryLoadMergedIniData workdir =
        let globalConfigPath = tryGetGlobalConfigPath ()
        let localConfigPath = getLocalConfigPath workdir
        if File.Exists localConfigPath then
            match globalConfigPath with 
            | Some x    -> merge (localConfigPath |> fromFile) (x |> fromFile) |> Some
            | None      -> localConfigPath |> fromFile |> Some
        else
            match globalConfigPath with 
            | Some x    -> (x |> fromFile) |> Some
            | None      -> None

    /// Set the given value for the key in the ini file, overwriting a possibly existing value
    let setValueInIniPath path name value = 
        let iniData = path |> fromFile
        match trySetValue name value iniData with
        | Some ini -> ini
        | None -> addValue name value iniData
        |> toFile path