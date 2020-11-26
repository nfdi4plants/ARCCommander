namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments

/// Assay object subcommand verbs
type AssayCommand = 

    | [<CliPrefix(CliPrefix.None)>] Init     of init_args:      ParseResults<AssayInitArgs>
    | [<CliPrefix(CliPrefix.None)>] Update   of update_args:    ParseResults<AssayUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit     of edit_args:      ParseResults<AssayEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register of register_args:  ParseResults<AssayRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Add      of add_args:       ParseResults<AssayAddArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove   of remove_args:    ParseResults<AssayRemoveArgs>
    | [<CliPrefix(CliPrefix.None)>] Move     of move_args:      ParseResults<AssayMoveArguments>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init              _ -> "Initialize a new empty assay and associated folder structure in the arc."
            | Update            _ -> "Update an existing assay in the arc with the given assay metadata"
            | Edit              _ -> "Open and edit an existing assay in the arc with a text editor. Arguments passed for this command will be pre-set in the editor."
            | Register          _ -> "Register an existing assay in the arc with the given assay metadata."
            | Add               _ -> "Create a new assay file and associated folder structure in the arc and subsequently register it with the given assay metadata"
            | Remove            _ -> "Remove an assay from the given studys' assay register"
            | Move              _ -> "Move an assay from one study to another"
            | List              _ -> "List all assays registered in the arc"