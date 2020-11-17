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

/// CLI arguments for setting a configuration setting
type ConfigurationSetArgs = 
    | [<AltCommandLine("-l")>][<Unique>] Local
    | [<AltCommandLine("-g")>][<Unique>] Global
    | [<Mandatory>][<AltCommandLine("-n")>][<Unique>] Name  of string
    | [<Mandatory>][<AltCommandLine("-v")>][<Unique>] Value of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Local _   -> "Set the the value of the name locally for this arc"
            | Global _  -> "Set the the value of the name globally for the arccommander"
            | Name _    -> "The name of the setting in 'Section.Key' format"
            | Value _   -> "The new value of the setting"

/// CLI arguments for unsetting a configuration setting
type ConfigurationUnsetArgs = 
    | [<AltCommandLine("-l")>][<Unique>] Local
    | [<AltCommandLine("-g")>][<Unique>] Global
    | [<Mandatory>][<AltCommandLine("-n")>][<Unique>] Name  of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Local _   -> "Unset the the value of the name locally for this arc"
            | Global _  -> "Unset the the value of the name globally for the arccommander"
            | Name _    -> "The name of the setting in 'Section.Key' format"
