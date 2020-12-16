namespace ArcCommander.CLIArguments

open Argu 

/// CLI arguments for creating a new investigation file for the arc
// in the case of investigations 'empty' does not mean empty file but rather an 
// investigation without studies/assays. To reflect the need for metadata here,
// this command is called `create` instead of `init`
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

/// CLI arguments updating the arc's existing investigation file
// Same arguments as `create` because all 'creatable' metadata fields are also 'updatable'
type InvestigationUpdateArgs = InvestigationCreateArgs

/// CLI arguments interactively editing the arc's existing investigation file
// Same arguments as `create` because all 'creatable' metadata fields are also 'editable'
type InvestigationEditArgs = InvestigationCreateArgs

/// CLI arguments for deleting the arc's investigation file (danger zone!)
type InvestigationDeleteArgs =
    | [<Mandatory>][<Unique>] Identifier of investigation_identifier:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _-> "Identifier of the investigation"
