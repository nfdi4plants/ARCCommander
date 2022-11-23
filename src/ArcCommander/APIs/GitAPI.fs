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

        let lfsConfig = 
            if containsFlag "NoLFS" gitArgs then
                $" {GitHelper.noLFSConfig}"
            else
                ""

        if System.IO.Directory.GetFileSystemEntries repoDir |> Array.isEmpty then
            log.Trace("Downloading into current folder.")
            executeGitCommand repoDir $"clone {lfsConfig} {remoteAddress}{branch} ." |> ignore
        else 
            log.Trace($"Specified folder \"{repoDir}\" is not empty. Downloading into subfolder.")
            executeGitCommand repoDir $"clone {lfsConfig} {remoteAddress}{branch}" |> ignore


    /// Syncs with remote. Commit changes, then pull remote and push to remote.
    let sync (arcConfiguration : ArcConfiguration) (gitArgs : Map<string,Argument>) =

        let log = Logging.createLogger "GitSyncLog"
        
        log.Info("Start Arc Sync")

        // get repository directory
        let repoDir = getRepoDir(arcConfiguration)     

        if checkUserMetadataConsistency repoDir log |> not then

            log.Error("ERROR: Git user metadata set in the arcCommander config do not match the ones in the git config. This information is needed for git commits. Consider running \"arc config setgituser\" to synchronize the information between configs.")
        
        else
            log.Trace("Load .gitattributes")
            let ruleSet = GitHelper.retrieveLFSRules repoDir

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

                if GitHelper.containsLFSRule ruleSet lfsPath |> not then
                    executeGitCommand repoDir $"lfs track \"{lfsPath}\"" |> ignore

                trackWithAdd file

        
            let gitLfsRules = GeneralConfiguration.getGitLfsRules arcConfiguration

            gitLfsRules
            |> Array.iter (fun rule ->
                if GitHelper.containsLFSRule ruleSet rule |> not then
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
        
            let branch = tryGetFieldValueByName "Branch" gitArgs |> Option.defaultValue "main"

            executeGitCommand repoDir $"branch -M {branch}" |> ignore

            /// check whether a remote is set in git config
            let remoteSpecified () =
                let ok, msg, error = Fake.Tools.Git.CommandHelper.runGitCommand repoDir "remote -v"
                msg.Length > 0
                      
            let remoteIsGitHub () =
                executeGitCommandWithResponse repoDir "remote get-url origin"
                |> Seq.exists (fun s -> s.ToLower().Contains("github.com"))

            /// check whether the specified remote exists online
            let remoteExists () =
                let ok, msg, error = Fake.Tools.Git.CommandHelper.runGitCommand repoDir "fetch"
                error :: msg
                |> List.exists (fun m -> 
                    m.Contains "Repository not found"
                    ||  m.Contains "The project you were looking for could not be found"
                )
                |> not

            // add remote if specified
            match tryGetFieldValueByName "RepositoryAddress" gitArgs with
                | None -> ()
                | Some remote ->
                    if remoteSpecified () then executeGitCommand repoDir ("remote remove origin") |> ignore
                    executeGitCommand repoDir ("remote add origin " + remote) |> ignore

            // pull and push if remote exists
            if remoteSpecified() then

                log.Trace("Start syncing with remote")

                if remoteExists() then
                    log.Trace("Pull")
                    executeGitCommand repoDir ("fetch origin") |> ignore
                    executeGitCommand repoDir ($"pull --rebase origin {branch}") |> ignore

                    log.Trace("Push")
                    executeGitCommand repoDir ($"push -u origin {branch}") |> ignore

                else

                    if containsFlag "Force" gitArgs then

                        if remoteIsGitHub() then
                            let m = 
                                [
                                    "Remote does not exist and --force flag was set. But force pushing a new repository to GitHub is not supported."
                                    "First create an empty repository on github and try pushing again"
                                    "More info: https://docs.github.com/en/get-started/importing-your-projects-to-github/importing-source-code-to-github/adding-an-existing-project-to-github-using-the-command-line"
                                ]
                                |> List.reduce (fun a b -> a + "\n" + b)

                            log.Error(m)

                        else 

                            log.Trace("Remote does not exist and --force flag was set. Trying to create upstream repo.")

                            executeGitCommand repoDir ($"push --set-upstream origin {branch}") |> ignore

                    else
                        let m = 
                            [
                                "Remote repo was set, but does not exist."
                                "Check whether it was spelled correctly. If not, you can run \"arc sync\" again using the --repositoryAddress argument."
                                "If you want to create a new remote repository instead. You can run \"arc sync -f\" to force push the local repository to a new upstream."
                            ]
                            |> List.reduce (fun a b -> a + "\n" + b)
                        log.Error(m)

            else

                log.Error("Can not sync with remote as no remote repository adress was specified.")