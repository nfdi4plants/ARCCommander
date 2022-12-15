namespace ArcCommander.CLIArguments
/// ------------ TOP LEVEL ------------ ///
open Argu 


type ArcImportArgs = 

    | [<Unique>][<AltCommandLine("-j")>] ArcJson of arc_json : string
    | [<Unique>][<AltCommandLine("-f")>] ArcJsonFilePath of arc_json_filepath : string
    | [<Unique>][<AltCommandLine("-r")>] RepositoryAddress of repository_address : string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | ArcJson           _ ->  "Investigation json blob as string."
            | ArcJsonFilePath   _ ->  "Investigation json blob as string in a file."
            | RepositoryAddress _ ->  "Git repository address"


type ArcInitArgs = 

    | [<Unique>] Owner of owner : string
    | [<AltCommandLine("-b")>][<Unique>] Branch of branch_name : string
    // --repositoryadress is obsolete (previous spelling mistake)
    | [<AltCommandLine("--repositoryadress")>][<AltCommandLine("-r")>][<Unique>] RepositoryAddress of repository_address : string
    | [<Unique>] EditorPath of editor_path : string
    | [<Unique>] GitLFSByteThreshold of git_lfs_threshold : string
    | [<Unique>] Gitignore

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Owner               _ ->  "Owner of the ARC"
            | Branch              _ ->  "Name of the git branch to be created"
            | RepositoryAddress   _ ->  "Git repository address"
            | EditorPath          _ ->  "The path leading to the editor used for text prompts (Default in Windows is Notepad; Default in Unix systems is Nano)"
            | GitLFSByteThreshold _ ->  "The git LFS file size threshold in bytes. File larger than this threshold will be tracked by git LFS (Default Value is 150000000 Bytes ~ 150 MB)."
            | Gitignore           _ ->  "Use this flag if you want a default .gitignore to be added to the initialized repo"

type ArcExportArgs = 

    | [<AltCommandLine("-o")>][<Unique>] Output of output : string
    | [<AltCommandLine("-ps")>][<Unique>] ProcessSequence

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Output            _ -> "Path to which the json should be exported. Only written to the cli output if no path given"
            | ProcessSequence   _ -> "If this flag is set, the return value of this arc will be its list of all its processes"


/// TO-DO: Argumente anpassen
type ArcSyncArgs =
    | [<Unique>][<AltCommandLine("-r")>] RepositoryAddress  of repository_address:string
    | [<Unique>][<AltCommandLine("-m")>] CommitMessage      of commit_message:string
    | [<Unique>][<AltCommandLine("-b")>] Branch             of branch:string
    | [<Unique>][<AltCommandLine("-f")>] Force


    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | RepositoryAddress _ -> "Git remote address with which to sync the ARC. If no address is either given here or set previously when initing or getting the ARC, changes are only committed but not synced."
            | CommitMessage     _ -> "Descriptive title for the changes made (e.g. add new assay). If none is given, default commit message is used."
            | Branch            _ -> "Name of the branch to which the changes should be pushed. If none is given, defaults to \"main\""
            | Force             _ -> "When a remote is set, but does not exist online, tries to create it."

/// TO-DO: Argumente anpassen
type ArcGetArgs =
    | [<Mandatory>][<Unique>][<AltCommandLine("-r")>] RepositoryAddress of repository_address:string
    | [<Unique>][<AltCommandLine("-b")>] BranchName         of branch_name:string
    | [<Unique>][<AltCommandLine("-n")>] NoLFS

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | RepositoryAddress _ -> "Git remote address from which to pull the ARC"
            | BranchName        _ -> "Branch of the remote address which should be used. If none is given, uses \"main\""
            | NoLFS             _ -> "Does download only the pointers of LFS files, not the file content itself. Ideal for when you're only interested in the experimental metadata, not the data itself."
