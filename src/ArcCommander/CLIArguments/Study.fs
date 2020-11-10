namespace ArcCommander.CLIArguments

open Argu 
open ISA

type StudyCreateArgs =
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

type StudyUpdateArgs = StudyCreateArgs
type StudyRegisterArgs = StudyCreateArgs
type StudyAddArgs = StudyCreateArgs

type StudyEditArgs =
    | [<Mandatory>][<Unique>] Identifier of study_identifier:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _->    "Identifier of the study, will be used as the file name of the study file"

type StudyRemoveArgs = StudyEditArgs

type Study =

    | [<CliPrefix(CliPrefix.None)>] Create of create_args:ParseResults<StudyCreateArgs>
    | [<CliPrefix(CliPrefix.None)>] Update of update_args:ParseResults<StudyUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit of edit_args:ParseResults<StudyEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register of register_args:ParseResults<StudyRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Add of add_args:ParseResults<StudyAddArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove of remove_args:ParseResults<StudyRemoveArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List 

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Create    _ -> "Create a new study with the given parameters"
            | Update    _ -> "Update a study with the given parameters"
            | Edit      _ -> "Open an editor window to directly edit a study"
            | Register  _ -> "Register an existing study in the given investigation"
            | Add       _ -> "Create a new study and add it to the given investigation"
            | Remove    _ -> "Remove a study from the arc"
            | List      _ -> "List all studies registered in the given investigation"