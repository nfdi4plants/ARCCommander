namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments

open AccessToken

/// Remote access subcommand verbs
type RemoteAccessCommand =

    | [<AltCommandLine("token")>][<CliPrefix(CliPrefix.None)>] AccessToken of access_token_verbs   : ParseResults<AccessTokenCommand>


    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | AccessToken   _ -> "Functions related to receiving and handling git access token."


and AccessTokenCommand =

    | [<CliPrefix(CliPrefix.None)>] Get     of get_args:     ParseResults<AccessTokenGetArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Get   _ -> "Receive and store a git access token by authenticating to a token delivery service."

