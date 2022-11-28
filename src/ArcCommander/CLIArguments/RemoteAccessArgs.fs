namespace ArcCommander.CLIArguments

open Argu 

/// CLI arguments for remote access access tokens
module AccessToken = 

    /// CLI arguments for receiving access tokens
    type AccessTokenGetArgs =  
        | [<AltCommandLine("-s")>][<Unique>] Server of string
        | [<Unique>] OAuth2
        | [<Unique>] OpenID

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Server    _ -> "URL of the service for which you want to receive an access token. If no url is given, \"git.nfdi4plants.org\" is used as default."
                | OAuth2    _ -> "Use OAuth2 authorization protocol. E.g. used by GitHub."
                | OpenID    _ -> "Use OpenID connect authorization protocol. E.g. used by DataPlant GitLab instances."

    /// CLI arguments for receiving access tokens
    type AccessTokenStoreArgs = 
        | [<Mandatory>][<AltCommandLine("-t")>][<Unique>] Token of string
        | [<AltCommandLine("-u")>][<Unique>] User of string
        | [<AltCommandLine("-s")>][<Unique>] Server of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Token     _ -> "The access token you want to store."
                | User      _ -> "User name for which the token applies. If no name is given, \"oauth2\" is used as a default value. This is also the value expected by the \"git.nfdi4plants.org\" GitLab instance."
                | Server    _ -> "URL of the service for which you want to receive an access token. If no url is given, \"git.nfdi4plants.org\" is used as default."