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

    /// Creates an ArcConfiguration from the section settings.
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
    /// Creates a default ArcConfiguration.
    static member GetDefault() =
        let os = getOs ()
        let editor = 
            match os with
            | Windows   -> "notepad"
            | Unix      -> "nano"
        [
        "general.verbosity"                 , "1"
        "general.editor"                    , editor
        "general.rootfolder"                , ".arc"
        "general.verbosity"                 , "1"
        "general.gitlfsbytethreshold"       , "150000000"
        "general.gitlfsrules"               , "**/dataset/**"
        "general.forceeditor"               , "false"
        
        "isamodel.investigationfilename"    , "isa.investigation.xlsx"
        "isamodel.studiesfilename"          , "isa.studies.xlsx"
        "isamodel.assay location"           , "folder.assay.rootfolder"
        "isamodel.assayfilename"            , "isa.assay.xlsx"
        
        "assay.rootfolder"                  , "assays"
        //"assay.rootfolder.<assayIdentifier>.folder"
        "assay.folders"                     , "dataset;protocols"
        "assay.files"                       , "README.md"
        
        "workflow.rootfolder"               , "workflows"
        "workflow.dockerfile"               , "Dockerfile"
        
        "external.rootfolder"               , "externals"
        "external.externalsfile"            , "isa.tab"
        
        "run.rootfolder"                    , "runs"
        ]
        |> fromNameValuePairs


    /// Gets the current configuration by merging the default settings, the global settings, the local settings and the settings given through arguments.
    static member ofIniData argumentConfig =
        ArcConfiguration.create
            (getSectionMap "general"   argumentConfig)
            (getSectionMap "isamodel"  argumentConfig)
            (getSectionMap "assay"     argumentConfig)
            (getSectionMap "workflow"  argumentConfig)
            (getSectionMap "external"  argumentConfig)
            (getSectionMap "run"       argumentConfig)

    /// Gets the current configuration by merging the default settings, the global settings, the local settings and the settings given through arguments. Uses `ofIniData`for this.
    static member load argumentConfig =
        let log = Logging.createLogger "IniParserModelLoadLog"
        let workdir = tryGetValueByName "general.workdir" argumentConfig |> Option.get
        let mergedIniData = 
            match tryLoadMergedIniData workdir with
            | Some fileConfig ->
                ArcConfiguration.GetDefault()
                |> merge fileConfig
                |> merge argumentConfig
            | None -> 
                log.Warn("WARNING: No config file found. Load default config instead.")
                ArcConfiguration.GetDefault()
                |> merge argumentConfig
        ArcConfiguration.ofIniData mergedIniData

    // TODO TO-DO TO DO: open all record fields using reflection
    /// Returns the full paths of the rootfolders.
    static member getRootFolderPaths (configuration : ArcConfiguration) =
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

    /// Returns all settings as name value pairs.
    static member flatten (configuration : ArcConfiguration) =
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

/// Functions for retrieving general settings from the configuration.
module GeneralConfiguration =

    /// Returns the path to the text editor used for querying user input.
    let tryGetEditor configuration = 
        Map.tryFind "editor" configuration.General

     /// Returns the path to the text editor used for querying user input.
    let getEditor configuration = 
        Map.find "editor" configuration.General

    /// Returns the path to the ARC if it exists. Else returns None.
    let tryGetWorkDirectory configuration = 
        Map.tryFind "workdir" configuration.General

    /// Returns the path to the ARC.
    let getWorkDirectory configuration = 
        Map.find "workdir" configuration.General

    /// Returns the authority address from which the access tokens gets requested if it exists. Else returns None.
    let tryGetKCAuthority configuration  =
        Map.tryFind "kcauthority" configuration.General

    /// Returns the authority address from which the access tokens get requested.
    let getKCAuthority configuration  =
        Map.find "kcauthority" configuration.General 

    /// Returns the client id used for identifying to the token delivery service if it exists. Else returns None.
    let tryGetKCClientID configuration  =
        Map.tryFind "kcclientid" configuration.General

    /// Returns the client id used for identifying to the token delivery service.
    let getKCClientID configuration  =
        Map.find "kcclientid" configuration.General

    /// Returns the scope used for requesting the gitlab token from the token delivery service if it exists. Else returns None.
    let tryGetKCScope configuration  =
        Map.tryFind "kcscope" configuration.General

    /// Returns the scope used for requesting the gitlab token from the token delivery service.
    let getKCScope configuration  =
        Map.find "kcscope" configuration.General

    /// Returns the uri to which the client redirects after authentication to the token delivery service if it exists. Else returns None.
    let tryGetKCRedirectURI configuration  =
        Map.tryFind "kcredirecturi" configuration.General

    /// eturns the uri to which the client redirects after authentication to the token delivery service.
    let getKCRedirectURI configuration  =
        Map.find "kcredirecturi" configuration.General

    /// Returns the verbosity level if it exists. Else returns None.
    let tryGetVerbosity configuration = 
        Map.tryFind "verbosity" configuration.General |> Option.map int

    /// Returns the verbosity level.
    let getVerbosity configuration = 
        Map.find "verbosity" configuration.General |> int

    /// Returns the Git lfs threshold if it exists. Else returns None. Files larger than this amount of bytes will be tracked by Git lfs.
    let tryGetGitLfsByteThreshold configuration = 
        Map.tryFind "gitlfsbytethreshold" configuration.General |> Option.map int64

    /// Returns the Git lfs threshold. Files larger than this amount of bytes will be tracked by Git lfs.
    let getGitLfsByteThreshold configuration = 
        Map.find "gitlfsbytethreshold" configuration.General |> int

    /// Returns the Git lfs rules if they exist. Else returns None. Files matching these rules will be tracked by Git lfs.
    let tryGetGitLfsRules configuration = 
        Map.tryFind "gitlfsrules" configuration.General |> Option.map splitValues

    /// Returns the Git lfs rules. Files matching these rules will be tracked by Git lfs.
    let getGitLfsRules configuration = 
        Map.find "gitlfsrules" configuration.General |> splitValues

    /// Returns force editor parameter if it exists. Else returns None.
    let tryGetForceEditor configuration = 
        Map.tryFind "forceeditor" configuration.General |> Option.map (fun s -> s.ToLower() = "true")

    /// Returns force editor parameter.
    let getForceEditor configuration = 
        Map.find "forceeditor" configuration.General |> (fun s -> s.ToLower() = "true")

/// Functions for retrieving ISA file settings from the configuration.
module IsaModelConfiguration =

    /// Returns the assayIdentifier from a filename.
    let tryGetAssayIdentifierOfFileName (assayFileName : string) (configuration : ArcConfiguration) =      
        let name = Map.tryFind "assayfilename" configuration.IsaModel |> Option.get
        match assayFileName.Replace(@"\","/").Split("/") with
        | [|assayIdentifier; fn |] when fn = name -> Some assayIdentifier
        | [|assayIdentifier; _ |] -> None
        | _ -> None
        

    /// Returns the relative path of the assay file if it exists. Else returns None.
    let tryGetAssayFileName assayIdentifier (configuration : ArcConfiguration) =
        let assayFileName = Map.tryFind "assayfilename" configuration.IsaModel
        match assayFileName with
        | Some f -> 
            Path.Combine([|assayIdentifier;f|])
            |> Some
        | _ -> None

    /// Returns the relative path of the assay file.
    let getAssayFileName assayIdentifier (configuration : ArcConfiguration) =
        tryGetAssayFileName assayIdentifier configuration
        |> Option.get 

    /// Returns the full path of the assay file if it exists. Else returns None.
    let tryGetAssayFilePath assayIdentifier (configuration : ArcConfiguration) =
        let workDir = Map.find "workdir" configuration.General
        let assayFileName = tryGetAssayFileName assayIdentifier configuration
        let rootFolder = Map.tryFind "rootfolder" configuration.Assay
        match assayFileName,rootFolder with
        | Some f, Some r -> 
            Path.Combine([|workDir;r;f|])
            |> Some
        | _ -> None

    /// Returns the full path of the assay file.
    let getAssayFilePath assayIdentifier (configuration : ArcConfiguration)=
        tryGetAssayFilePath assayIdentifier configuration
        |> Option.get

    /// Returns the name of the study's file if it exists. Else returns None.
    let tryGetStudiesFileName identifier (configuration : ArcConfiguration) =
        //Map.tryFind "studiesfilename" configuration.IsaModel
        sprintf "%s_isa.study.xlsx" identifier
        |> Some

    /// Returns the name of the study's file.
    let getStudiesFileName identifier (configuration : ArcConfiguration) =
        tryGetStudiesFileName identifier configuration
        |> Option.get 

    /// Returns the full path of the study's file if it exists. Else returns None.
    let tryGetStudiesFilePath identifier (configuration : ArcConfiguration) =
        let workDir = Map.find "workdir" configuration.General
        match tryGetStudiesFileName identifier configuration with
        | Some i -> 
            Path.Combine(workDir, i)
            |> Some
        | _ -> None
      
    /// Returns the full path of the study's file.
    let getStudiesFilePath identifier (configuration : ArcConfiguration) =
        tryGetStudiesFilePath identifier configuration
        |> Option.get

    /// Returns the full path of the study files located in the arc root folder.
    let findStudyFilePaths (configuration : ArcConfiguration) =
        let workDir = Map.find "workdir" configuration.General
        Directory.GetFiles(workDir)
        |> Array.filter (fun s -> s.EndsWith "_isa.study.xlsx")

    /// Returns the study identifiers of the study files located in the arc root folder.
    let findStudyIdentifiers (configuration : ArcConfiguration) =
        let workDir = Map.find "workdir" configuration.General
        Directory.GetFiles(workDir)
        |> Array.choose (fun s -> 
            if s.EndsWith "_isa.study.xlsx" then
                Some (System.IO.FileInfo(s).Name.Replace("_isa.study.xlsx",""))
            else 
                None
        )

    /// Returns the full path of the investigation file if it exists. Else returns None.
    let tryGetInvestigationFilePath (configuration : ArcConfiguration) =
        let workDir = Map.find "workdir" configuration.General
        match Map.tryFind "investigationfilename" configuration.IsaModel with
        | Some i -> 
            Path.Combine(workDir,i)
            |> Some
        | _ -> None

    /// Returns the full path of the investigation file.
    let getInvestigationFilePath (configuration : ArcConfiguration) =
        tryGetInvestigationFilePath configuration
        |> Option.get

/// Functions for retrieving Assay related information from the configuration.
module AssayConfiguration =

    /// Returns the full path of the assays rootfolder if it exists. Else returns None.
    let tryGetRootFolderPath configuration =
        Map.tryFind "rootfolder" configuration.Assay

    /// Returns the full path of the assays rootfolder.
    let getRootFolderPath configuration =
        tryGetRootFolderPath configuration
        |> Option.get

    /// Returns the full paths of the assay folders.
    let getAssayPaths configuration =
        getRootFolderPath configuration
        |> System.IO.Directory.GetDirectories

    /// Returns the names of the assay folders.
    let getAssayNames configuration =
        getRootFolderPath configuration
        |> System.IO.DirectoryInfo
        |> fun di -> di.GetDirectories()
        |> Array.map (fun d -> d.Name)

    /// Returns the full path of the files associated with the assay.
    let getFilePaths assayIdentifier configuration =
        let workDir = Map.find "workdir" configuration.General
        let fileNames = Map.tryFind "files" configuration.Assay
        let rootFolder = Map.tryFind "rootfolder" configuration.Assay
        match fileNames,rootFolder with
        | Some vs, Some r -> 
            vs
            |> splitValues
            |> Array.map (fun v ->
                Path.Combine([|workDir; r; assayIdentifier; v|])
            )                
        | _ -> [||]

    /// Returns the full path of the assay folder if it exists. Else returns None.
    let tryGetFolderPath assayIdentifier configuration =
        let workDir = Map.find "workdir" configuration.General
        Map.tryFind "rootfolder" configuration.Assay
        |> Option.map (fun r -> Path.Combine([|workDir; r; assayIdentifier|]))

    /// Returns the full path of the assay folder.
    let getFolderPath assayIdentifier configuration =
        tryGetFolderPath assayIdentifier configuration 
        |> Option.get

    /// Returns the full path of the subFolders associated with the assay.
    let getSubFolderPaths assayIdentifier configuration =
        let subFolderNames = Map.tryFind "folders" configuration.Assay
        let assayFolder = tryGetFolderPath assayIdentifier configuration
        match subFolderNames,assayFolder with
        | Some vs, Some r -> 
            vs
            |> splitValues
            |> Array.map (fun v ->
                Path.Combine([|r; v|])
            )
        | _ -> Array.empty


