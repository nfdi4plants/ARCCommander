namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments

/// Assay object subcommand verbs
type ConfigurationCommand = 

    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args:       ParseResults<ConfigurationEditArgs>
    | [<CliPrefix(CliPrefix.None)>] List        of list_args:       ParseResults<ConfigurationListArgs>
    | [<CliPrefix(CliPrefix.None)>] Set         of set_args:        ParseResults<ConfigurationSetArgs>
    | [<CliPrefix(CliPrefix.None)>] Unset       of unset_args:      ParseResults<ConfigurationUnsetArgs>
    | [<CliPrefix(CliPrefix.None)>] SetGitUser  of setgituser_args: ParseResults<ConfigurationSetGitUserArgs>


    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Edit       _ -> "Open and edit an existing assay in the ARC with a text editor. Arguments passed for this command will be pre-set in the editor."
            | List       _ -> "List all assays registered in the ARC"
            | Set        _ -> "Assign the given value to the given name"
            | Unset      _ -> "Remove the value bound to the given name" 
            | SetGitUser _ -> "Transfer the git user metadata from the global arc config to the git config. These are used for commits. Alternative e-mail and username can be specified"