namespace ArcCommander.APIs

open ArcCommander
open ArcCommander.ArgumentProcessing



module RemoteAccessAPI =

    module AccessToken =

        /// Store a git token using git credential manager.
        let store (arcConfiguration : ArcConfiguration) (remoteAccessArgs : Map<string,Argument>) =

            let log = Logging.createLogger "GitStoreLog"

            let token = getFieldValueByName "Token" remoteAccessArgs

            let hostAddress = 
                let ha = 
                    match tryGetFieldValueByName "Server" remoteAccessArgs with
                    | Some s -> s
                    | None -> @"https://git.nfdi4plants.org/"
                if ha.Contains "https" then ha
                else $"https://{ha}"

            let user = 
                match tryGetFieldValueByName "User" remoteAccessArgs with
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
        let get (arcConfiguration : ArcConfiguration) (remoteAccessArgs : Map<string,Argument>) =

            let log = Logging.createLogger "GitAuthenticateLog"

            let hostAddress = 
                let ha = 
                    match tryGetFieldValueByName "Server" remoteAccessArgs with
                    | Some s -> s
                    | None -> @"https://git.nfdi4plants.org/"
                if ha.Contains "https" then ha
                else $"https://{ha}"

            log.Info("Start Arc Authenticate")
        
            // Select Authorization protocol
            let tryReceiveToken = 
                if containsFlag "OAuth2" remoteAccessArgs then
                    Authentication.OAuth2.tryLogin
                elif containsFlag "OpenID" remoteAccessArgs then
                    Authentication.Oidc.tryLogin
                else
                    log.Info ("No authentication protocol specified, defaulting to OpenID")
                    Authentication.Oidc.tryLogin

            match tryReceiveToken log hostAddress arcConfiguration with 
            | Ok token -> 
                log.Info("Successfully retrieved access token from token service")
                store arcConfiguration (Map.add "Token" (Field token.AccessToken) remoteAccessArgs)
            | Error err -> 
                log.Error($"Could not retrieve access token: {err.Message}")

