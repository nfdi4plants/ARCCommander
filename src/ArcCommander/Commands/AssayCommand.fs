namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments

/// Assay object subcommand verbs
type AssayCommand = 
    //Additions
    | [<CliPrefix(CliPrefix.None)>] Init        of init_args:       ParseResults<AssayInitArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args:   ParseResults<AssayRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Add         of add_args:        ParseResults<AssayAddArgs>
    //Removals
    | [<CliPrefix(CliPrefix.None)>] Delete      of delete_args:     ParseResults<AssayDeleteArgs>
    | [<CliPrefix(CliPrefix.None)>] Unregister  of unregister_args: ParseResults<AssayUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove      of remove_args:     ParseResults<AssayRemoveArgs>
    //Modifications
    | [<CliPrefix(CliPrefix.None)>] Update      of update_args:     ParseResults<AssayUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args:       ParseResults<AssayEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Move        of move_args:       ParseResults<AssayMoveArgs>
    //Retrievals
    | [<CliPrefix(CliPrefix.None)>] Get         of get_args:        ParseResults<AssayGetArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init              _ -> "Initialize a new empty assay and associated folder structure in the arc."
            | Register          _ -> "Register an existing assay in the arc with the given assay metadata."
            | Add               _ -> "Create a new assay file and associated folder structure in the arc and subsequently register it with the given assay metadata"
            
            | Delete            _ -> "Delete the given assays folder and its underlying file structure"
            | Unregister        _ -> "Unregister an assay from the given studys' assay register in the investigation file"
            | Remove            _ -> "Both unregister and assay from the investigation file and delete its folders and files"

            | Update            _ -> "Update an existing assay in the arc with the given assay metadata"
            | Edit              _ -> "Open and edit an existing assay in the arc with a text editor. Arguments passed for this command will be pre-set in the editor."
            | Move              _ -> "Move an assay from one study to another"

            | Get               _ -> "Gets the values of an existing assay"
            | List              _ -> "List all assays registered in the arc"