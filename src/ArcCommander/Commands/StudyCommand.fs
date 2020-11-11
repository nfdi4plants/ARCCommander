namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments

/// Study object subcommand verbs
type StudyCommand =

    | [<CliPrefix(CliPrefix.None)>] Init     of init_args:ParseResults<StudyInitArgs>
    | [<CliPrefix(CliPrefix.None)>] Update   of update_args:ParseResults<StudyUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit     of edit_args:ParseResults<StudyEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register of register_args:ParseResults<StudyRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Add      of add_args:ParseResults<StudyAddArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove   of remove_args:ParseResults<StudyRemoveArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List 

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init      _ -> "Initialize a new empty study file in the arc"
            | Update    _ -> "Update an existing study in the arc with the given study metadata"
            | Edit      _ -> "Open and edit an existing study in the arc with a text editor. Arguments passed for this command will be pre-set in the editor."
            | Register  _ -> "Register an existing study in the arc with the given assay metadata."
            | Add       _ -> "Create a new study file in the arc and subsequently register it with the given study metadata"
            | Remove    _ -> "Remove a study from the arc"
            | List      _ -> "List all studies registered in the arc"