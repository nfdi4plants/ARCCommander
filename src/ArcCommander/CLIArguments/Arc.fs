namespace ArcCommander.CLIArguments
/// ------------ TOP LEVEL ------------ ///
open Argu 
open ISA


type ArcInitArgs = 

    | [<Unique>] Owner of owner:string
    | [<Unique>] RepositoryAdress of repository_adress:string
    | [<Unique>] EditorPath of editor_path:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Owner _               ->  "Owner of the arc"
            | RepositoryAdress _    ->  "Github adress"
            | EditorPath _          ->  "The path leading to the editor used for text prompts (Default in Windows is notepad)"


type Arc =
    | [<AltCommandLine("-p")>][<Unique>] WorkingDir of working_directory: string
    | [<Unique>] Silent
    | [<CliPrefix(CliPrefix.None)>][<SubCommand>] Init of init_args:ParseResults<ArcInitArgs>
    | [<AltCommandLine("i")>][<CliPrefix(CliPrefix.None)>] Investigation of verb_and_args:ParseResults<Investigation>
    | [<AltCommandLine("s")>][<CliPrefix(CliPrefix.None)>] Study of verb_and_args:ParseResults<Study>
    //| [<CliPrefix(CliPrefix.None)>] AddWorkflow of ParseResults<WorkflowArgs>
    | [<AltCommandLine("a")>][<CliPrefix(CliPrefix.None)>] Assay of verb_and_args:ParseResults<Assay>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | WorkingDir _ -> "Set the base directory of your ARC"
            | Silent   _ -> "Prevents the tool from printing additional information"
            | Init _ -> "Initializes basic folder structure"
            | Investigation _ -> "Investigation file functions"
            | Study         _ -> "Study functions"
            //| AddWorkflow _ -> "Not yet implemented"
            | Assay _ ->  "Assay functions"