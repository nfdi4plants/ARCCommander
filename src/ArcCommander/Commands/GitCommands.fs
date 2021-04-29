namespace ArcCommander.Commands

open Argu
open ArcCommander.CLIArguments

/// Assay object subcommand verbs
type GitCommand =

    | [<CliPrefix(CliPrefix.None)>] Update  of update_args: ParseResults<GitUpdateArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update _ -> "Erklärung einfügen"
