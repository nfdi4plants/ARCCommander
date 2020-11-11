namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments

type Arc =

    | [<AltCommandLine("-p")>][<Unique>] WorkingDir of working_directory: string
    | [<Unique>] Silent
    | [<CliPrefix(CliPrefix.None)>][<SubCommand>] Init of init_args:ParseResults<ArcInitArgs>
    | [<AltCommandLine("i")>][<CliPrefix(CliPrefix.None)>] Investigation of verb_and_args:ParseResults<InvestigationCommand>
    | [<AltCommandLine("s")>][<CliPrefix(CliPrefix.None)>] Study of verb_and_args:ParseResults<StudyCommand>
    //| [<CliPrefix(CliPrefix.None)>] AddWorkflow of ParseResults<WorkflowArgs>
    | [<AltCommandLine("a")>][<CliPrefix(CliPrefix.None)>] Assay of verb_and_args:ParseResults<AssayCommand>

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