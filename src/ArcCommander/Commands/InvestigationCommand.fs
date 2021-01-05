namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments
open InvestigationContacts
open InvestigationPublications


type InvestigationCommand = 
    
    | [<CliPrefix(CliPrefix.None)>] Create of create_args: ParseResults<InvestigationCreateArgs>
    | [<CliPrefix(CliPrefix.None)>] Update of update_args: ParseResults<InvestigationUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] Edit 
    | [<CliPrefix(CliPrefix.None)>] Delete of delete_args: ParseResults<InvestigationDeleteArgs>
    | [<CliPrefix(CliPrefix.None)>] Contacts of contacts:   ParseResults<InvestigationPersonCommand>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Create            _ -> "Create a new investigation with the given metadata"
            | Update            _ -> "Update the arc's investigation with the given metdadata"
            | Edit              _ -> "Open an editor window to directly edit the arc's investigation file"
            | Delete            _ -> "Delete the arc's investigation file (danger zone!)"
            | Contacts          _ -> "Person functions"

and InvestigationPersonCommand =

    | [<CliPrefix(CliPrefix.None)>] Update   of update_args:    ParseResults<PersonUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit     of edit_args:      ParseResults<PersonEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register of register_args:  ParseResults<PersonRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove   of remove_args:    ParseResults<PersonRemoveArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update            _ -> "Update an existing person in the arc investigation with the given person metadata. The person is identified by the full name (first name, last name, mid initials)"
            | Edit              _ -> "Open and edit an existing person in the arc investigation with a text editor. The person is identified by the full name (first name, last name, mid initials)"
            | Register          _ -> "Register a person in the arc investigation with the given assay metadata."
            | Remove            _ -> "Remove a person from the given investigation. The person is identified by the full name (first name, last name, mid initials)."
            | List              _ -> "List all person registered in the arc investigation"

and InvestigationPublicationCommand =

    | [<CliPrefix(CliPrefix.None)>] Update   of update_args:    ParseResults<PublicationUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit     of edit_args:      ParseResults<PublicationEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register of register_args:  ParseResults<PublicationRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove   of remove_args:    ParseResults<PublicationRemoveArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update            _ -> "Update an existing publication in the arc investigation with the given publication metadata. The publication is identified by the doi"
            | Edit              _ -> "Open and edit an existing publication in the arc investigation with a text editor. The publication is identified by the doi"
            | Register          _ -> "Register a publication in the arc investigation with the given assay metadata."
            | Remove            _ -> "Remove a publication from the given investigation. The publication is identified by the doi"
            | List              _ -> "List all publication registered in the arc investigation"
