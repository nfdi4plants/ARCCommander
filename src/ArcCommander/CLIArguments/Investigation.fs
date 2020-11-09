namespace ArcCommander.CLIArguments

open Argu 
open ISA

type InvestigationCreateArgs = 
    | [<Mandatory>][<Unique>] Identifier of investigation_identifier:string
    | [<Unique>] Title of title:string
    | [<Unique>] Description of description:string
    | [<Unique>] SubmissionDate of submission_date:string
    | [<Unique>] PublicReleaseDate of public_release_date:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier        _-> "Identifier of the investigation"
            | Title             _-> "Title of the investigation"
            | Description       _-> "Description of the investigation"
            | SubmissionDate    _-> "Submission Date of the investigation"
            | PublicReleaseDate _-> "Public Release Date of the investigation"

type InvestigationUpdateArgs = InvestigationCreateArgs

type InvestigationRemoveArgs =
    | [<Mandatory>][<Unique>] Identifier of investigation_identifier:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _-> "Identifier of the investigation"

type InvestigationEditArgs = InvestigationRemoveArgs

type Investigation = 
    
    | [<CliPrefix(CliPrefix.None)>] Create of init_args: ParseResults<InvestigationCreateArgs>
    | [<CliPrefix(CliPrefix.None)>] Update of update_args: ParseResults<InvestigationUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit of edit_args: ParseResults<InvestigationEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove of remove_args: ParseResults<InvestigationRemoveArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Create            _ -> "Create a new investigation with the given parameters"
            | Update            _ -> "Update an investigation with the given parameters"
            | Edit              _ -> "Open an editor window to directly edit an investigation"
            | Remove            _ -> "Remove an investigation from the arc"
