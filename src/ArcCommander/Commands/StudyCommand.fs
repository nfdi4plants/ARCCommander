namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments

open StudyContacts
open StudyPublications
open StudyDesignDescriptors


/// Study object subcommand verbs
type StudyCommand =

    | [<CliPrefix(CliPrefix.None)>] Init        of init_args:ParseResults<StudyInitArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args:ParseResults<StudyRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Add         of add_args:ParseResults<StudyAddArgs>

    | [<CliPrefix(CliPrefix.None)>] Delete      of delete_args:ParseResults<StudyDeleteArgs>
    | [<CliPrefix(CliPrefix.None)>] Unregister  of unregister_args:ParseResults<StudyUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove      of remove_args:ParseResults<StudyRemoveArgs>

    | [<CliPrefix(CliPrefix.None)>] Update      of update_args:ParseResults<StudyUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args:ParseResults<StudyEditArgs>

    | [<CliPrefix(CliPrefix.None)>] Get        of get_args:ParseResults<StudyGetArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List 

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init          _ -> "Initialize a new empty study file in the arc"
            | Register      _ -> "Register an existing study in the arc with the given assay metadata."
            | Add           _ -> "Create a new study file in the arc and subsequently register it with the given study metadata"
            | Delete        _ -> "Delete a study from the arc file structure"
            | Unregister    _ -> "Unregister a study from the arc investigation file"
            | Remove        _ -> "Remove a study from the arc"
            | Update        _ -> "Update an existing study in the arc with the given study metadata"
            | Edit          _ -> "Open and edit an existing study in the arc with a text editor. Arguments passed for this command will be pre-set in the editor."
            | Get           _ -> "Get the values of a study"
            | List          _ -> "List all studies registered in the arc"


and StudyPersonCommand =

    | [<CliPrefix(CliPrefix.None)>] Update      of update_args:     ParseResults<PersonUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args:       ParseResults<PersonEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args:   ParseResults<PersonRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Unregister  of remove_args:     ParseResults<PersonUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Get         of get_args:        ParseResults<PersonGetArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update            _ -> "Update an existing person in the arc investigation study with the given person metadata. The person is identified by the full name (first name, last name, mid initials)"
            | Edit              _ -> "Open and edit an existing person in the arc investigation study with a text editor. The person is identified by the full name (first name, last name, mid initials)"
            | Register          _ -> "Register a person in the arc investigation study with the given assay metadata."
            | Unregister        _ -> "Unregister a person from the given investigation study. The person is identified by the full name (first name, last name, mid initials)."
            | Get               _ -> "Get the metadata of a person registered in the arc investigation study"
            | List              _ -> "List all persons registered in the arc investigation"

and StudyPublicationCommand =

    | [<CliPrefix(CliPrefix.None)>] Update      of update_args:     ParseResults<PublicationUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args:       ParseResults<PublicationEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args:   ParseResults<PublicationRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove      of remove_args:     ParseResults<PublicationUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Get         of get_args:        ParseResults<PublicationGetArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update            _ -> "Update an existing publication in the arc investigation study with the given publication metadata. The publication is identified by the doi"
            | Edit              _ -> "Open and edit an existing publication in the arc investigation study with a text editor. The publication is identified by the doi"
            | Register          _ -> "Register a publication in the arc investigation study with the given assay metadata."
            | Remove            _ -> "Unregister a publication from the given investigation study. The publication is identified by the doi"
            | Get               _ -> "Get the metadata of a publication registered in the arc investigation study"
            | List              _ -> "List all publication registered in the arc investigation study"

