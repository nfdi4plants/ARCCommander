namespace ArcCommander.CLIArguments
/// ------------ TOP LEVEL ------------ ///
open Argu 


type ArcInitArgs = 

    | [<Unique>] Owner of owner : string
    | [<Unique>] RepositoryAdress of repository_adress : string
    | [<Unique>] EditorPath of editor_path : string
    | [<Unique>] GitLFSByteThreshold of git_lfs_threshold : string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Owner _               ->  "Owner of the ARC"
            | RepositoryAdress _    ->  "Github adress"
            | EditorPath _          ->  "The path leading to the editor used for text prompts (Default in Windows is Notepad; Default in Unix systems is Nano)"
            | GitLFSByteThreshold _ ->  "The git LFS file size threshold in bytes. File larger than this threshold will be tracked by git LFS (Default Value is 150000000 Bytes ~ 150 MB)."

type ArcExportArgs = 

    | [<AltCommandLine("-p")>][<Unique>] Path of path : string
    | [<AltCommandLine("-ps")>][<Unique>] ProcessSequence

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Path              _ -> "Path to which the json should be exported. Only written to the cli output if no path given"
            | ProcessSequence   _ -> "If this flag is set, the return value of this arc will be its list of all its processes"
