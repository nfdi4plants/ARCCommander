namespace ArcCommander

open System.Diagnostics

module GitHelper =

    /// Executes Git command.
    let executeGitCommand (repoDir : string) (command : string) =
        
        let log = Logging.createLogger "ExecuteGitCommandLog"

        log.Trace(sprintf "git %s" command)
        let success = Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir command
        if not success
        then log.Error("ERROR: Git command could not be run.")
        success

    let formatRepoString username pass (url : string) = 
        let comb = username + ":" + pass + "@"
        url.Replace("https://","https://" + comb)

    let formatRepoToken (token : Authentication.IdentityToken) (url : string) = 
        formatRepoString token.UserName token.GitAccessToken url

    let setLocalEmail (dir : string) (email : string) =
        executeGitCommand dir (sprintf "config user.email \"%s\"" email)

    let setLocalEmailToken (dir : string) (token : Authentication.IdentityToken) =
        setLocalEmail dir token.Email

    let setGlobalEmail (email : string) =
        executeGitCommand "" (sprintf "config --global user.email \"%s\"" email)

    let setLocalName (dir : string) (name : string) =
        executeGitCommand dir (sprintf "config user.name \"%s\"" name)

    let setLocalNameToken (dir : string) (token : Authentication.IdentityToken) =
        setLocalName dir (token.FirstName + " " + token.LastName)

    let setGlobalName (name : string) =
        executeGitCommand "" (sprintf "config --global user.name \"%s\"" name)

    let clone dir url =
        executeGitCommand dir (sprintf "clone %s" url)

    let cloneWithToken dir token url  =
        let url = formatRepoToken token url
        clone dir url 

    let add dir = 
        executeGitCommand dir "add ."

    let commit dir message =
        executeGitCommand dir (sprintf "commit -m \"%s\"" message)

    let push dir =
        executeGitCommand dir "push"

    /// Stores git credentials to a git host using the git credential interface
    let storeCredentials (log : NLog.Logger) host username password =

        log.Info($"INFO: Start git credential storing")

        let protocol = "https"
        let path = $"git:{protocol}://{host}"
    
        let procStartInfo = 
            ProcessStartInfo(
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // Redirect standard input, as input is required after the process starts
                RedirectStandardInput = true,
                UseShellExecute = false,
                FileName = "git",
                Arguments = "credential approve"
            )
            
        let outputs = System.Collections.Generic.List<string>()
        let errors = System.Collections.Generic.List<string>()
        let outputHandler (_sender:obj) (args:DataReceivedEventArgs) = 
            outputs.Add args.Data
            log.Trace($"TRACE: {args.Data}")
        
        let errorHandler (_sender:obj) (args:DataReceivedEventArgs) = 
            
            if args.Data.Contains "trace:" then
                outputs.Add args.Data
                log.Trace($"TRACE: {args.Data}")
            else 
                errors.Add args.Data
                log.Error($"ERROR: {args.Data}")

        let p = new Process(StartInfo = procStartInfo)
        
        p.OutputDataReceived.AddHandler(DataReceivedEventHandler outputHandler)
        p.ErrorDataReceived.AddHandler(DataReceivedEventHandler errorHandler)

        log.Trace($"TRACE: Start storing git credentials by running \"git credential approve\"")

        p.Start() |> ignore
        p.BeginOutputReadLine()
        p.BeginErrorReadLine()
        
        log.Trace($"TRACE: Start feeding credentials into git credential interface")

        p.StandardInput.WriteLine $"url={path}"
        p.StandardInput.WriteLine $"username={username}"
        p.StandardInput.WriteLine $"host={host}"
        p.StandardInput.WriteLine $"path={path}"
        p.StandardInput.WriteLine $"protocol={protocol}"
        p.StandardInput.WriteLine $"password={password}"
        p.StandardInput.WriteLine ""

        log.Trace($"TRACE: Exiting git credential storing")

        p.WaitForExit()

        errors.Count = 0

    /// Stores git credentials to a git host using the git credential interface
    let storeCredentialsToken (log : NLog.Logger) (token : Authentication.IdentityToken) =
        storeCredentials log token.GitHost token.UserName token.GitAccessToken
