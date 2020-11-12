namespace ArcCommander

open System.IO
open Configuration

/// Functionality for providing Configurations to the ArcCommander
module ArcConfiguration =

    type ArcConfiguration =
        {
            General     : Map<string,string>
            IsaModel    : Map<string,string>
            Assay       : Map<string,string>
            Workflow    : Map<string,string>
            External    : Map<string,string>
            Run         : Map<string,string>                     
        }

    let createArcConfiguration general isaModel assay workflow external run =
        {
            General     = general
            IsaModel    = isaModel
            Assay       = assay
            Workflow    = workflow
            External    = external 
            Run         = run                     
        }

    let getArcConfigurationSection section configuration =
        match tryGetSection section configuration with
        | Some kvs -> 
            kvs
            |> Seq.map (fun kv -> kv.KeyName,kv.Value)
            |> Map.ofSeq
        | None -> Map.empty

    /// Gets the current configuration
    let loadArcConfiguration workdir =
        let config = loadMergedConfiguration workdir
        createArcConfiguration
            (getArcConfigurationSection "general" config)
            (getArcConfigurationSection "isamodel" config)
            (getArcConfigurationSection "assay" config)
            (getArcConfigurationSection "workflow" config)
            (getArcConfigurationSection "external" config)
            (getArcConfigurationSection "run" config)

    /// Returns the full paths of the rootfolders
    let tryGetRootFolderPaths workingDir configuration =
        getAllValuesOfKey "rootfolder" configuration
        |> Seq.map (fun f -> Path.Combine(workingDir,f))

open ArcConfiguration

/// Functions for retrieving general settings from the configuration
module GeneralConfiguration =

    /// Returns the path to the text editor used for querying user input
    let tryGetEditor configuration =           
        Map.tryFind "editor" configuration.General


module IsaModelConfiguration =

    /// Returns the full path of the assay file
    let tryGetAssayFilePath workingDir assayIdentifier (configuration:ArcConfiguration) =
        let assayFileName = Map.tryFind "assayfilename" configuration.IsaModel
        let rootFolder = Map.tryFind "rootfolder" configuration.Assay
        match assayFileName,rootFolder with
        | Some f, Some r -> 
            Path.Combine([|workingDir;r;assayIdentifier;f|])
            |> Some
        | _ -> None

    /// Returns the full path of the investigation file
    let tryGetStudiesFilePath workingDir (configuration:ArcConfiguration) =
        match Map.tryFind "studiesfilename" configuration.IsaModel with
        | Some i -> 
            Path.Combine(workingDir,i)
            |> Some
        | _ -> None

    /// Returns the full path of the investigation file
    let tryGetInvestigationFilePath workingDir (configuration:ArcConfiguration) =
        match Map.tryFind "investigationfilename" configuration.IsaModel with
        | Some i -> 
            Path.Combine(workingDir,i)
            |> Some
        | _ -> None


/// Functions for retrieving Assay related information from the configuration
module AssayConfiguration =

    /// Returns the full path of the files associated with the assay
    let getFilePaths workingDir assayIdentifier configuration =
        let fileNames = Map.tryFind "files" configuration.Assay
        let rootFolder = Map.tryFind "rootfolder" configuration.Assay
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
        let folderNames = Map.tryFind "folders" configuration.Assay
        let rootFolder = Map.tryFind "rootfolder" configuration.Assay
        match folderNames,rootFolder with
        | Some vs, Some r -> 
            vs
            |> splitValues
            |> Seq.map (fun v ->
                Path.Combine([|workingDir;r;assayIdentifier;v|])
            )                
        | _ -> Seq.empty
