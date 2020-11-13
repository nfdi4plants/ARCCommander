namespace ArcCommander

open System.IO
open Configuration


type ArcConfiguration =
    {
        General     : Map<string,string>
        IsaModel    : Map<string,string>
        Assay       : Map<string,string>
        Workflow    : Map<string,string>
        External    : Map<string,string>
        Run         : Map<string,string>                     
    }

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
    static member getDefault() =
        let editor = "notepad"////GET DEFAULT EDITOR for linux
        [
        "general.editor", editor
        "general.silence", "false"            
        ]
        |> fromNameValuePairs


    static member getSection sectionName configuration =
        match tryGetSection sectionName configuration with
        | Some kvs -> 
            kvs
            |> Seq.map (fun kv -> kv.KeyName,kv.Value)
            |> Map.ofSeq
        | None -> Map.empty

    /// Gets the current configuration
    static member load argumentConfig =
        let workdir = tryGetValueByName "general.workdir" argumentConfig |> Option.get
        let config = 
            ArcConfiguration.getDefault()
            |> merge (loadMergedConfiguration workdir)
            |> merge argumentConfig
        ArcConfiguration.create
            (ArcConfiguration.getSection "general" config)
            (ArcConfiguration.getSection "isamodel" config)
            (ArcConfiguration.getSection "assay" config)
            (ArcConfiguration.getSection "workflow" config)
            (ArcConfiguration.getSection "external" config)
            (ArcConfiguration.getSection "run" config)

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
