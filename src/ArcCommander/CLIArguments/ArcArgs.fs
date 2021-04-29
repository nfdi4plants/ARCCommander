namespace ArcCommander.CLIArguments
/// ------------ TOP LEVEL ------------ ///
open Argu 


type ArcInitArgs = 

    | [<Unique>] Owner of owner:string
    | [<Unique>] RepositoryAdress of repository_adress:string
    | [<Unique>] EditorPath of editor_path:string
    | [<Unique>] GitLFSByteThreshold of git_lfs_threshold:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Owner _               ->  "Owner of the arc"
            | RepositoryAdress _    ->  "Github adress"
            | EditorPath _          ->  "The path leading to the editor used for text prompts (Default in Windows is notepad)"
            | GitLFSByteThreshold _ ->  "The git lfs file size threshold in bytes. File larger than this threshold will be tracked by git lfs (Default Value is 150000000Bytes ~ 150MB)"