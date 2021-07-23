namespace ArcCommander.Commands

open Argu
open ArcCommander.CLIArguments

/// Assay object subcommand verbs
type GitCommand =

    | [<CliPrefix(CliPrefix.None)>] Init    of init_args:   ParseResults<GitInitArgs>
    | [<CliPrefix(CliPrefix.None)>] Sync    of sync_args:   ParseResults<GitSyncArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init      _ -> "Set up git locally for this arc"
            | Sync      _ -> "Commits changes made in the arc. If a remote is set or is given, also pulls from there and pushes all previously made commits."
