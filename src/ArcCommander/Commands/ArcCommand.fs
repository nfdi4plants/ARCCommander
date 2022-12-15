namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments

[<HelpFlags([|"--help"; "-h"|])>]
type ArcCommand =
    ///Parameters
    | [<AltCommandLine("-p")>][<Unique>]                        WorkingDir  of working_directory : string
    | [<AltCommandLine("-v")>][<Unique>]                        Verbosity       of verbosity : int
    ///Commands
    | [<CliPrefix(CliPrefix.None)>]                             Init            of init_args    : ParseResults<ArcInitArgs>
    | [<CliPrefix(CliPrefix.None)>]                             Export          of export_args  : ParseResults<ArcExportArgs>
    | [<CliPrefix(CliPrefix.None)>]                             Import          of import_args  : ParseResults<ArcImportArgs>
    | [<CliPrefix(CliPrefix.None)>]                             Sync            of sync_args    : ParseResults<ArcSyncArgs>
    | [<CliPrefix(CliPrefix.None)>]                             Get             of get_args     : ParseResults<ArcGetArgs>
    | [<CliPrefix(CliPrefix.None)>][<SubCommand()>]             Update
    | [<CliPrefix(CliPrefix.DoubleDash)>][<SubCommand()>]       Version
    ///Subcommands
    | [<AltCommandLine("i")>][<CliPrefix(CliPrefix.None)>]      Investigation   of verb_and_args : ParseResults<InvestigationCommand>
    | [<AltCommandLine("s")>][<CliPrefix(CliPrefix.None)>]      Study           of verb_and_args : ParseResults<StudyCommand>
    | [<AltCommandLine("a")>][<CliPrefix(CliPrefix.None)>]      Assay           of verb_and_args : ParseResults<AssayCommand>
    | [<AltCommandLine("config")>][<CliPrefix(CliPrefix.None)>] Configuration   of verb_and_args : ParseResults<ConfigurationCommand>
    | [<AltCommandLine("remote")>][<CliPrefix(CliPrefix.None)>] RemoteAccess    of verb_and_args : ParseResults<RemoteAccessCommand>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | WorkingDir    _   -> "Set the base directory of your ARC"
            | Verbosity     _   -> "Set the amount of additional printed information: 0 -> No information, 1 (Default) -> Basic Information, 2 -> Additional information"
            | Init          _   -> "Initialize basic folder structure"
            | Update        _   -> "Update items in the arc against each other"
            | Export        _   -> "Exports the full arc to a json object"
            | Import        _   -> "Create a full arc folder and annotation file structure from an arc json blob. Currently this arc json is equal to an ISA investigation json."
            | Sync          _   -> "Syncronize the ARC with its upstream repository. Commits changes made in the ARC. If a remote is set or is given, also pulls from there and pushes all previously made commits."
            | Get           _   -> "Download an ARC from a remote repository (e.g. from gitlab)"
            | RemoteAccess  _   -> "Subcommands for handling access functionality to remote repositories"
            | Investigation _   -> "Investigation file functions"
            | Study         _   -> "Study functions"
            | Assay         _   -> "Assay functions"
            | Configuration _   -> "Configuration editing"
            | Version       _   -> "Get the ArcCommander's current version"