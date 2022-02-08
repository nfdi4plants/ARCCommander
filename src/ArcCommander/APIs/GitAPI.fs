namespace ArcCommander.APIs

open ArcCommander
open ArgumentProcessing
open Fake.IO
open System.IO


module GitAPI =

    open GitHelper

    /// Returns repository directory path.
    let getRepoDir (arcConfiguration : ArcConfiguration) =
        let workdir = GeneralConfiguration.getWorkDirectory arcConfiguration
        let gitDir = Fake.Tools.Git.CommandHelper.findGitDir(workdir).FullName

        Fake.IO.Path.getDirectory(gitDir)

    /// Authenticates to a token service and stores the token using git credential manager.
    let authenticate (arcConfiguration : ArcConfiguration) =

        let log = Logging.createLogger "GitAuthenticateLog"

        log.Info("Start Arc Authenticate")
        
        match Authentication.tryLogin log arcConfiguration with 
        | Ok token -> 
            log.Info($"Successfully retrieved access token from token service")

            log.Tracery transfer git user metadata to global arcCommander config")
            match IniData.tryGetGlobalConfigPath () with
            | Some globalConfigPath ->
                IniData.setValueInIniPath globalConfigPath "general.gitname"    (token.FirstName + " " + token.LastName)
                IniData.setValueInIniPath globalConfigPath "general.gitemail"   token.Email
                log.Trace($"Successfully transferred git user metadata to global arcCommander config")
            | None ->
                log.Error($"Could not transfer git user metadata to global arcCommander config")

            if storeCredentialsToken log token then
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



    /// Clones Git repository ARC.
    let get (arcConfiguration : ArcConfiguration) (gitArgs : Map<string,Argument>) =

        let log = Logging.createLogger "GitGetLog"
        
        log.Info("Start Arc Get")

        // get repository directory
        let repoDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        let remoteAddress = getFieldValueByName "RepositoryAddress" gitArgs
                   
        let branch = 
            match tryGetFieldValueByName "BranchName" gitArgs with 
            | Some branchName -> $" -b {branchName}"
            | None -> ""

        if System.IO.Directory.GetFileSystemEntries repoDir |> Array.isEmpty then
            log.Trace("Downloading into current folder.")
            executeGitCommand repoDir $"clone {remoteAddress}{branch} ." |> ignore
        else 
            log.Trace($"Specified folder \"{repoDir}\" is not empty. Downloading into subfolder.")
            executeGitCommand repoDir $"clone {remoteAddress}{branch}" |> ignore


    /// Syncs with remote. Commit changes, then pull remote and push to remote.
    let sync (arcConfiguration : ArcConfiguration) (gitArgs : Map<string,Argument>) =

        let log = Logging.createLogger "GitSyncLog"
        
        log.Info("Start Arc Sync")

        // get repository directory
        let repoDir = getRepoDir(arcConfiguration)

        if checkUserMetadataConsistency repoDir log |> not then

            log.Error("ERROR: Git user metadata set in the arcCommander config do not match the ones in the git config. This information is needed for git commits. Consider running \"arc config setgituser\" to synchronize the information between configs.")
        
        else
            log.Trace("Delete .gitattributes")

            File.Delete(Path.Combine(repoDir,".gitattributes"))

            // track all untracked files
            printfn "-----------------------------"
            let rec getAllFiles(cDir:string) =
                let mutable l = []

                let dirs = System.IO.Directory.GetDirectories cDir |> Array.filter (fun x -> not (x.Contains ".git") ) |> List.ofSeq

                l <- List.concat (dirs |> List.map (fun x -> getAllFiles x ))

                let files = System.IO.Directory.GetFiles cDir |> List.ofSeq
                l <- l @ files

                l

            let allFiles = getAllFiles(repoDir)

            let allFilesPlusSizes = allFiles |> List.map( fun x -> x, System.IO.FileInfo(x).Length )

            let trackWithAdd (file : string) =

                executeGitCommand repoDir $"add \"{file}\"" |> ignore

            let trackWithLFS (file : string) =

                let lfsPath = file.Replace(repoDir, "").Replace("\\","/")

                executeGitCommand repoDir $"lfs track \"{lfsPath}\"" |> ignore

                trackWithAdd file
                trackWithAdd (System.IO.Path.Combine(repoDir, ".gitattributes"))

        
            let gitLfsRules = GeneralConfiguration.getGitLfsRules arcConfiguration

            gitLfsRules
            |> Array.iter (fun rule ->
                executeGitCommand repoDir $"lfs track \"{rule}\"" |> ignore
            )

            let gitLfsThreshold = GeneralConfiguration.tryGetGitLfsByteThreshold arcConfiguration

            log.Trace("Start tracking files")

            allFilesPlusSizes 
            |> List.iter (fun (file,size) ->

                    /// Track files larger than the git lfs threshold with git lfs. If no threshold is set, track no files with git lfs
                    match gitLfsThreshold with
                    | Some thr when size > thr -> trackWithLFS file
                    | _ -> trackWithAdd file
            )


            executeGitCommand repoDir ("add -u") |> ignore
            printfn "-----------------------------"

            // commit all changes
            let commitMessage =
                match tryGetFieldValueByName "CommitMessage" gitArgs with
                | None -> "Update"
                | Some s -> s

            // print git status if verbose
            // executeGitCommand repoDir ("status") |> ignore

            log.Trace("Commit tracked files" )
            log.Trace($"git commit -m '{commitMessage}'")

            Fake.Tools.Git.Commit.exec repoDir commitMessage |> ignore
        
            let branch = tryGetFieldValueByName "BranchName" gitArgs |> Option.defaultValue "main"

            executeGitCommand repoDir $"branch -M {branch}" |> ignore

            // detect existing remote
            let hasRemote () =
                let ok, msg, error = Fake.Tools.Git.CommandHelper.runGitCommand repoDir "remote -v"
                msg.Length > 0

            // add remote if specified
            match tryGetFieldValueByName "RepositoryAdress" gitArgs with
                | None -> ()
                | Some remote ->
                    if hasRemote () then executeGitCommand repoDir ("remote remove origin") |> ignore
                    executeGitCommand repoDir ("remote add origin " + remote) |> ignore

        if hasRemote() then log.Trace("Start syncing with remote" )
        else                log.Error("Can not sync with remote as no remote repository adress was specified.")

            // pull if remote exists
            if hasRemote() then
                log.Trace("Pull")
                executeGitCommand repoDir ("fetch origin") |> ignore
                executeGitCommand repoDir ($"pull --rebase origin {branch}") |> ignore

            // push if remote exists
            if hasRemote () then
                log.Trace("Push")
                executeGitCommand repoDir ($"push -u origin {branch}") |> ignore
