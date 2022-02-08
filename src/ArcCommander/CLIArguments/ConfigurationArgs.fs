namespace ArcCommander.CLIArguments
open Argu 

/// CLI arguments for listing configuration settings
type ConfigurationListArgs =
    | [<AltCommandLine("-l")>][<Unique>] Local
    | [<AltCommandLine("-g")>][<Unique>] Global

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Local   _ -> "Lists the local settings for this Arc"
            | Global   _ -> "Lists the global settings of the ArcCommander"

/// CLI arguments for configuration editing
type ConfigurationEditArgs = 
    | [<AltCommandLine("-l")>][<Unique>] Local
    | [<AltCommandLine("-g")>][<Unique>] Global

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Local   _ -> "Edit the local settings for this ARC"
            | Global   _ -> "Edit the global settings of the ArcCommander"

/// CLI arguments for setting a configuration setting
type ConfigurationSetArgs = 
    | [<AltCommandLine("-l")>][<Unique>] Local
    | [<AltCommandLine("-g")>][<Unique>] Global
    | [<Mandatory>][<AltCommandLine("-n")>][<Unique>] Name  of string
    | [<Mandatory>][<AltCommandLine("-v")>][<Unique>] Value of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Local _   -> "Set the the value of the name locally for this ARC"
            | Global _  -> "Set the the value of the name globally for the ArcCommander"
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
            | Local _   -> "Unset the the value of the name locally for this ARC"
            | Global _  -> "Unset the the value of the name globally for the ArcCommander"
            | Name _    -> "The name of the setting in 'Section.Key' format"

/// CLI arguments for transferring the git user metadata from the arc config to the git config
type ConfigurationSetGitUserArgs = 
    | [<AltCommandLine("-l")>][<Unique>] Local
    | [<AltCommandLine("-g")>][<Unique>] Global
    | [<AltCommandLine("-n")>][<Unique>] Name   of string
    | [<AltCommandLine("-e")>][<Unique>] Email  of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Local  _ -> "Set the git user metadata locally for this arc repository"
            | Global _ -> "Set the git user metadata globally for the git installation"
            | Name   _ -> "The name of the user"
            | Email  _ -> "The e-mail of the user"