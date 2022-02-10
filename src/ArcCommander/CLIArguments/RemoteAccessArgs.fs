namespace ArcCommander.CLIArguments

open Argu 

/// CLI arguments for remote access access tokens
module AccessToken = 

    /// CLI arguments for receiving access tokens
    type AccessTokenGetArgs =  
        | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] Server of string
        | [<Unique>] OAuth2
        | [<Unique>] OpenID

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Server    _ -> "URL of the service for which you want to receive an access token."
                | OAuth2    _ -> "Use OAuth2 authorization protocol. E.g. used by GitHub."
                | OpenID    _ -> "Use OpenID connect authorization protocol. E.g. used by DataPlant GitLab instances."
