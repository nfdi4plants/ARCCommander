namespace ArcCommander.Commands

open Argu
open ArcCommander.CLIArguments

/// Assay object subcommand verbs
type GitCommand =

    | [<CliPrefix(CliPrefix.None)>] Update  of update_args: ParseResults<GitUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Push    of push_args: ParseResults<GitPushArgs>
    | [<CliPrefix(CliPrefix.None)>] Init    of init_args: ParseResults<GitInitArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update _ -> "Erklärung einfügen"
            | Push _ -> "Erklärung einfügen"
            | Init _ -> "Erklärung einfügen"
