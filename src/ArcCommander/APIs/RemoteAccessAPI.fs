namespace ArcCommander.APIs

open ArcCommander
open ArcCommander.ArgumentProcessing



module RemoteAccessAPI =

    module AccessToken =

        /// Authenticates to a token service and stores the token using git credential manager.
        let get (arcConfiguration : ArcConfiguration) (remoteAccessArgs : Map<string,Argument>) =

            let log = Logging.createLogger "GitAuthenticateLog"

            let hostAddress = getFieldValueByName "Server" remoteAccessArgs

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
                log.Info($"Successfully retrieved access token from token service")

                log.Trace($"Try transfer git user metadata to global arcCommander config")
                match IniData.tryGetGlobalConfigPath () with
                | Some globalConfigPath ->
                    IniData.setValueInIniPath globalConfigPath "general.gitname"    (token.FirstName + " " + token.LastName)
                    IniData.setValueInIniPath globalConfigPath "general.gitemail"   token.Email
                    log.Trace($"Successfully transferred git user metadata to global arcCommander config")
                | None ->
                    log.Error($"Could not transfer git user metadata to global arcCommander config")

                if GitHelper.storeCredentialsToken log token then
                    log.Info($"Finished Authentication")

                else
                    let m = 
                        [
                            $"Authentication worked, but credentials could not be stored successfully."
                            $"Check if git is installed and if a credential helper is setup:"
                            $"Run \"git config --global credential.helper cache\" to cache credentials in memory"
                            $"or Run \"git config --global credential.helper store\" to save credentials to disk"
                            $"For more info go to: https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage"
                        ]
                        |> List.reduce (fun a b -> a + "\n" + b)
                    log.Error(m)
            | Error err -> 
                log.Error($"Could not retrieve access token: {err.Message}")