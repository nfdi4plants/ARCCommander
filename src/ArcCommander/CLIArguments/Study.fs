namespace ArcCommander.CLIArguments

open Argu 
open ISA

type StudyParams =
    | [<Mandatory>][<Unique>] Identifier of string
    | [<Unique>] Title of string
    | [<Unique>] Description of string
    | [<Unique>] SubmissionDate of string
    | [<Unique>] PublicReleaseDate of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _->    "Identifier of the study"
            | Title _->         "Title of the study"
            | Description _->   "Description of the study"
            | SubmissionDate _->   "Submission Date of the study"
            | PublicReleaseDate _->   "Public Release Date of the study"

type StudyId =
    | [<Mandatory>][<Unique>] Identifier of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _->    "Identifier of the study, will be used as the file name of the study file"

type Study =

    | [<CliPrefix(CliPrefix.None)>] Update of ParseResults<StudyParams>
    | [<CliPrefix(CliPrefix.None)>] Register of ParseResults<StudyParams>
    | [<CliPrefix(CliPrefix.None)>] Remove of ParseResults<StudyId>
    | [<CliPrefix(CliPrefix.None)>] Edit of ParseResults<StudyId>
    | [<CliPrefix(CliPrefix.None)>] List 

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update    _ -> "The study gets updated with the given parameters"
            | Register  _ -> "Create a new Study"
            | Remove    _ -> "Removes the Study from the arc"
            | Edit      _ -> "Open an editor window to directly edit the isa Study file"
            | List      _ -> "Lists all studies in the investigation file"