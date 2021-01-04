namespace ArcCommander

open System.IO
open IniData

/// Contains settings about the arc
type ArcConfiguration =

    {
        General     : Map<string,string>
        IsaModel    : Map<string,string>
        Assay       : Map<string,string>
        Workflow    : Map<string,string>
        External    : Map<string,string>
        Run         : Map<string,string>                     
    }

    /// Creates an arcConfiguration from the section settings
    static member create general isaModel assay workflow external run =
        {
            General     = general
            IsaModel    = isaModel
            Assay       = assay
            Workflow    = workflow
            External    = external 
            Run         = run                     
        }

    //TO:DO, rename and possibly move
    /// 
    static member getDefault() =
        let editor = "notepad"////GET DEFAULT EDITOR for linux
        [
        "general.editor", editor     
        ]
        |> fromNameValuePairs

    /// Gets the current configuration by merging the default settings, the global settings, the local settings and the settings given through arguments
    static member load argumentConfig =
        let workdir = tryGetValueByName "general.workdir" argumentConfig |> Option.get
        let mergedIniData = 
            ArcConfiguration.getDefault()
            |> merge (loadMergedIniData workdir)
            |> merge argumentConfig
        ArcConfiguration.create
            (getSectionMap "general"   mergedIniData)
            (getSectionMap "isamodel"  mergedIniData)
            (getSectionMap "assay"     mergedIniData)
            (getSectionMap "workflow"  mergedIniData)
            (getSectionMap "external"  mergedIniData)
            (getSectionMap "run"       mergedIniData)

    // TODO TO-DO TO DO: open all record fields using reflection
    /// Returns the full paths of the rootfolders
    static member getRootFolderPaths (configuration:ArcConfiguration) =
        let workDir = Map.find "workdir" configuration.General
        [|
            configuration.General.TryFind "rootfolder"
            configuration.Assay.TryFind "rootfolder"
            configuration.External.TryFind "rootfolder"
            configuration.IsaModel.TryFind "rootfolder"
            configuration.Run.TryFind "rootfolder"
            configuration.Workflow.TryFind "rootfolder"
        |]
        |> Array.choose (id)
        |> Array.map (fun f -> Path.Combine(workDir,f))

    /// Returns all settings as name value pairs
    static member flatten (configuration:ArcConfiguration) =
        let keyValueToNameValue s k v = sprintf "%s.%s" s k, v
        [|
            configuration.General |> Map.map (keyValueToNameValue "general")
            configuration.Assay |> Map.map (keyValueToNameValue "assay")
            configuration.External |> Map.map (keyValueToNameValue "external")
            configuration.IsaModel |> Map.map (keyValueToNameValue "isamodel")
            configuration.Run |> Map.map (keyValueToNameValue "run")
            configuration.Workflow |> Map.map (keyValueToNameValue "workflow")
        |]
        |> Seq.collect (Map.toSeq >> Seq.map snd)

/// Functions for retrieving general settings from the configuration
module GeneralConfiguration =

    /// Returns the path to the text editor used for querying user input
    let tryGetEditor configuration = 
        Map.tryFind "editor" configuration.General

     /// Returns the path to the text editor used for querying user input
    let getEditor configuration = 
        Map.find "editor" configuration.General

    /// Returns the path to the arc
    let tryGetWorkDirectory configuration = 
        Map.tryFind "workdir" configuration.General

    /// Returns the path to the arc
    let getWorkDirectory configuration = 
        Map.find "workdir" configuration.General

    /// Returns the path to the arc
    let tryGetVerbosity configuration = 
        Map.tryFind "verbosity" configuration.General |> Option.map int

    /// Returns the path to the arc
    let getVerbosity configuration = 
        Map.find "verbosity" configuration.General |> int

/// Functions for retrieving isa file settings from the configuration
module IsaModelConfiguration =

    /// Returns the full path of the assay file
    let tryGetAssayFilePath assayIdentifier (configuration:ArcConfiguration) =
        let workDir = Map.find "workdir" configuration.General
        let assayFileName = Map.tryFind "assayfilename" configuration.IsaModel
        let rootFolder = Map.tryFind "rootfolder" configuration.Assay
        match assayFileName,rootFolder with
        | Some f, Some r -> 
            Path.Combine([|workDir;r;assayIdentifier;f|])
            |> Some
        | _ -> None

    /// Returns the full path of the assay file
    let tryGetAssayIdentifierOfFileName (assayFileName : string) =
        System.IO.Path.GetFileName assayFileName

    /// Returns the relative path of the assay file
    let tryGetAssayFileName assayIdentifier (configuration:ArcConfiguration) =
        let assayFileName = Map.tryFind "assayfilename" configuration.IsaModel
        match assayFileName with
        | Some f -> 
            Path.Combine([|assayIdentifier;f|])
            |> Some
        | _ -> None


    /// Returns the full path of the investigation file
    let tryGetStudiesFilePath (configuration:ArcConfiguration) =
        let workDir = Map.find "workdir" configuration.General
        match Map.tryFind "studiesfilename" configuration.IsaModel with
        | Some i -> 
            Path.Combine(workDir,i)
            |> Some
        | _ -> None

    /// Returns the full path of the investigation file
    let tryGetInvestigationFilePath (configuration:ArcConfiguration) =
        let workDir = Map.find "workdir" configuration.General
        match Map.tryFind "investigationfilename" configuration.IsaModel with
        | Some i -> 
            Path.Combine(workDir,i)
            |> Some
        | _ -> None


/// Functions for retrieving Assay related information from the configuration
module AssayConfiguration =

    /// Returns the full path of the files associated with the assay
    let getFilePaths assayIdentifier configuration =
        let workDir = Map.find "workdir" configuration.General
        let fileNames = Map.tryFind "files" configuration.Assay
        let rootFolder = Map.tryFind "rootfolder" configuration.Assay
        match fileNames,rootFolder with
        | Some vs, Some r -> 
            vs
            |> splitValues
            |> Array.map (fun v ->
                Path.Combine([|workDir;r;assayIdentifier;v|])
            )                
        | _ -> [||]

    /// Returns the full path of the folders associated with the assay
    let getFolderPaths assayIdentifier configuration =
        let workDir = Map.find "workdir" configuration.General
        let folderNames = Map.tryFind "folders" configuration.Assay
        let rootFolder = Map.tryFind "rootfolder" configuration.Assay
        match folderNames,rootFolder with
        | Some vs, Some r -> 
            vs
            |> splitValues
            |> Array.map (fun v ->
                Path.Combine([|workDir;r;assayIdentifier;v|])
            )                
        | _ -> Array.empty
