namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments

/// Assay object subcommand verbs
type ConfigurationCommand = 

    | [<CliPrefix(CliPrefix.None)>] Edit of ParseResults<ConfigurationEditArgs>
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ConfigurationListArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Edit              _ -> "Open and edit an existing assay in the arc with a text editor. Arguments passed for this command will be pre-set in the editor."
            | List              _ -> "List all assays registered in the arc"