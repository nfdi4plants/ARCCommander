namespace ArcCommander.CLIArguments
open Argu 
open ISA

/// CLI arguments for listing configuration settings
type ConfigurationListArgs =
    | [<AltCommandLine("-l")>][<Unique>] Local
    | [<AltCommandLine("-g")>][<Unique>] Global

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Local   _ -> "Lists the local settings for this arc"
            | Global   _ -> "Lists the global settings of the arccommander"

/// CLI arguments for configuration editing
type ConfigurationEditArgs = 
    | [<AltCommandLine("-l")>][<Unique>] Local
    | [<AltCommandLine("-g")>][<Unique>] Global

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Local   _ -> "Edit the local settings for this arc"
            | Global   _ -> "Edit the global settings of the arccommander"
