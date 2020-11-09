namespace ArcCommander.CLIArguments

open Argu 
open ISA

type InvestigationCreateArgs = 
    | [<Mandatory>][<Unique>] Identifier of string
    | [<Unique>] Title of string
    | [<Unique>] Description of string
    | [<Unique>] SubmissionDate of string
    | [<Unique>] PublicReleaseDate of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _->    "Identifier of the investigation"
            | Title _->         "Title of the investigation"
            | Description _->   "Description of the investigation"
            | SubmissionDate _->   "Submission Date of the investigation"
            | PublicReleaseDate _->   "Public Release Date of the investigation"

type InvestigationUpdateArgs = InvestigationCreateArgs

type InvestigationRemoveArgs =
    | [<Mandatory>][<Unique>] Identifier of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _-> "Identifier of the investigation"

type InvestigationEditArgs = InvestigationRemoveArgs

type Investigation = 
    
    | [<CliPrefix(CliPrefix.None)>] Create of investigation_init_args: ParseResults<InvestigationCreateArgs>
    | [<CliPrefix(CliPrefix.None)>] Update of investigation_update_args: ParseResults<InvestigationUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove of investigation_remove_args: ParseResults<InvestigationRemoveArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit of investigation_edit_args: ParseResults<InvestigationEditArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update            _ -> "The investigation gets updated with the given parameters"
            | Create            _ -> "Create a new investigation file with the given parameters"
            | Remove            _ -> "Removes the investigation from the arc"
            | Edit              _ -> "Open an editor window to directly edit the isa investigation file"
