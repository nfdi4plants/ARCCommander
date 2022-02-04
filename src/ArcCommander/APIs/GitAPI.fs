namespace ArcCommander.APIs

open ArcCommander
open ArgumentProcessing
open Fake.IO
open System.IO

open System
open System.Threading
open System.Threading.Tasks
open System.Runtime.InteropServices
open System.Collections.Generic
open System.Text
open System.Diagnostics

open IdentityModel.OidcClient
open Microsoft.Net.Http.Server
open Newtonsoft.Json

module GitHelper =

    /// Executes Git command.
    let executeGitCommand (repoDir : string) (command : string) =
        
        let log = Logging.createLogger "ExecuteGitCommandLog"

        log.Trace(sprintf "git %s" command)
        let success = Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir command
        if not success
        then log.Error("Git command could not be run.")
        success

    let formatRepoString username pass (url : string) = 
        let comb = username + ":" + pass + "@"
        url.Replace("https://","https://" + comb)

    let formatRepoToken (token : Authentication.IdentityToken) (url : string) = 
        formatRepoString token.UserName token.GitLabToken url

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

    //let pushWithToken dir =
    //    let url = formatRepoToken token url
    //    executeGitCommand dir "push"

module GitAPI =

    open GitHelper

    [<STAThread>]
    let tryLogin (log : NLog.Logger) (arcConfiguration : ArcConfiguration) =
        
        let options = Authentication.loadOptionsFromConfig arcConfiguration            

        let t = Authentication.signInAsync log options
        t.Wait()
        t.Result
        |> Result.map (fun result -> Authentication.IdentityToken.ofJwt result.AccessToken)
        //match t.Result with 
        //| Ok r -> 
        //    printfn "Success: %s" r.IdentityToken
        //    t.Result
        //| Error err -> 
        //    printfn "Failure"
        //    t.Result

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

        let remoteAddress = 
            let preliminaryAddress = getFieldValueByName "RepositoryAddress" gitArgs
            if preliminaryAddress.Contains "github.com"then
                preliminaryAddress
            else 
                match tryLogin log arcConfiguration with 
                | Ok token -> 
                    GitHelper.setLocalNameToken repoDir token  |> ignore
                    GitHelper.setLocalEmailToken repoDir token |> ignore
                    GitHelper.formatRepoToken token preliminaryAddress
                | Error err -> 
                    printfn "Failure"
                    preliminaryAddress
                   
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
