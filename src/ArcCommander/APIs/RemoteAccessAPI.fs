namespace ArcCommander.APIs

open ArcCommander
open ArcCommander.ArgumentProcessing
open ARCtrl.NET

open ArcCommander.CLIArguments.AccessToken

module RemoteAccessAPI =

    module AccessToken =

        /// Store a git token using git credential manager.
        let store (arcConfiguration : ArcConfiguration) (remoteAccessArgs : ArcParseResults<AccessTokenStoreArgs>) =

            let log = Logging.createLogger "GitStoreLog"

            let token = remoteAccessArgs.GetFieldValue AccessTokenStoreArgs.Token

            let hostAddress = 
                let ha = 
                    match remoteAccessArgs.TryGetFieldValue AccessTokenStoreArgs.Server with
                    | Some s -> s
                    | None -> @"https://git.nfdi4plants.org/"
                if ha.Contains "https" then ha
                else $"https://{ha}"

            let user = 
                match remoteAccessArgs.TryGetFieldValue AccessTokenStoreArgs.User with
                | Some s -> s
                | None -> @"oauth2"

            log.Info("Start Remote token store")
        
            if GitHelper.storeCredentials log hostAddress user token then
                log.Info($"Token stored successfully")

            else
                let m = 
                    [
                        $"Credentials could not be stored successfully."
                        $"Check if git is installed and if a credential helper is setup:"
                        $"Run \"git config --global credential.helper cache\" to cache credentials in memory"
                        $"or Run \"git config --global credential.helper store\" to save credentials to disk"
                        $"For more info go to: https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage"
                    ]
                    |> List.reduce (fun a b -> a + "\n" + b)
                log.Error(m)

        /// Authenticates to a token service and stores the token using git credential manager.
        let get (arcConfiguration : ArcConfiguration) (remoteAccessArgs : ArcParseResults<AccessTokenGetArgs>) =

            let log = Logging.createLogger "GitAuthenticateLog"

            let hostAddress = 
                let ha = 
                    match remoteAccessArgs.TryGetFieldValue AccessTokenGetArgs.Server with
                    | Some s -> s
                    | None -> @"https://git.nfdi4plants.org/"
                if ha.Contains "https" then ha
                else $"https://{ha}"

            log.Info("Start Arc Authenticate")
        
            // Select Authorization protocol
            let tryReceiveToken = 
                if remoteAccessArgs.ContainsFlag AccessTokenGetArgs.OAuth2  then
                    Authentication.OAuth2.tryLogin
                elif remoteAccessArgs.ContainsFlag AccessTokenGetArgs.OpenID then
                    Authentication.Oidc.tryLogin
                else
                    log.Info ("No authentication protocol specified, defaulting to OpenID")
                    Authentication.Oidc.tryLogin

            match tryReceiveToken log hostAddress arcConfiguration with 
            | Ok token -> 
                log.Info("Successfully retrieved access token from token service")
                let castedArgs = remoteAccessArgs.Cast<AccessTokenStoreArgs>().AsMap
                store arcConfiguration (ArcParseResults(Map.add "Token" (Field token.AccessToken) castedArgs))
            | Error err -> 
                log.Error($"Could not retrieve access token: {err.Message}")

