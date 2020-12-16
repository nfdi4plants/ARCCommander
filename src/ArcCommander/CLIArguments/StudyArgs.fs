namespace ArcCommander.CLIArguments

open Argu 

/// CLI arguments for empty study initialization
type StudyInitArgs =
    | [<Mandatory>][<Unique>] Identifier of study_identifier:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _->    "Identifier of the study, will be used as the file name of the study file"

/// CLI arguments for updating existing study metadata
type StudyUpdateArgs =
    | [<Mandatory>][<Unique>] Identifier of study_identifier:string
    | [<Unique>] Title of title:string
    | [<Unique>] Description of description:string
    | [<Unique>] SubmissionDate of submission_date:string
    | [<Unique>] PublicReleaseDate of public_release_date:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _->    "Identifier of the study"
            | Title _->         "Title of the study"
            | Description _->   "Description of the study"
            | SubmissionDate _->   "Submission Date of the study"
            | PublicReleaseDate _->   "Public Release Date of the study"

/// CLI arguments for interactively editing existing study metadata 
// Same arguments as `update` because edit is basically an interactive update, where 
// the arguments set in the command line will be already set when opening the editor
type StudyEditArgs = StudyUpdateArgs

/// CLI arguments for registering existing study metadata 
// Same arguments as `update` because all metadata fields that can be updated can also be set while registering
type StudyRegisterArgs = StudyUpdateArgs

/// CLI arguments for initializing and subsequently registering study metadata 
// Same arguments as `update` because all metadata fields that can be updated can also be set while registering a new assay
type StudyAddArgs = StudyUpdateArgs

/// CLI arguments for study removal
// same as `init` because both commands only need to be passed a study identifier
type StudyRemoveArgs = StudyInitArgs