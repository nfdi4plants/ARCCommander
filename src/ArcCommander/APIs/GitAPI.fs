namespace ArcCommander.APIs

open System
open ArcCommander
open ArgumentProcessing
open Fake.Tools.Git
open Fake.IO

module GitAPI =

    let executeGitCommand (verbosity : int) (repoDir:string) (command:string) =
        if verbosity >= 2 then printfn "git %s" command
        let success = Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir command
        if not success
        then printfn "[ERROR]"
        success

    let getRepoDir (arcConfiguration:ArcConfiguration) =
        let workdir = GeneralConfiguration.getWorkDirectory arcConfiguration
        let gitDir = Fake.Tools.Git.CommandHelper.findGitDir(workdir).FullName

        Fake.IO.Path.getDirectory(gitDir)

    /// Clones git repository arc
    let get (arcConfiguration : ArcConfiguration) (gitArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Arc get"

        // get repository directory
        let repoDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        let remoteAddress = getFieldValueByName "RepositoryAddress" gitArgs

        if System.IO.Directory.GetFileSystemEntries repoDir |> Array.isEmpty then
            if verbosity >= 2 then printfn "Downloading into current folder"
            executeGitCommand verbosity repoDir $"clone {remoteAddress} ." |> ignore
        else 
            if verbosity >= 2 then printfn "Specified folder \"%s\" is not empty. " repoDir
            if verbosity >= 2 then printfn "Downloading into subfolder"
            executeGitCommand verbosity repoDir $"clone {remoteAddress}" |> ignore


    /// sync with remote. Commit changes, then pull remote and push to remote
    let sync (arcConfiguration : ArcConfiguration) (gitArgs : Map<string,Argument>) =

        let verbosity = GeneralConfiguration.getVerbosity arcConfiguration
        
        if verbosity >= 1 then printfn "Start Arc sync"

        // get repository directory
        let repoDir = getRepoDir(arcConfiguration)

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

        let trackWithAdd (file:string) =

            executeGitCommand verbosity repoDir $"add \"{file}\"" |> ignore

        let trackWithLFS (file:string) =

            let lfsPath = file.Replace(repoDir,"").Replace("\\","/")

            executeGitCommand verbosity repoDir $"lfs track \"{lfsPath}\"" |> ignore

            trackWithAdd file
            trackWithAdd (System.IO.Path.Combine(repoDir,".gitattributes"))

        let gitLfsThreshold = GeneralConfiguration.tryGetGitLfsByteThreshold arcConfiguration

        if verbosity >= 2 then printfn "Start tracking files" 

        allFilesPlusSizes 
        |> List.iter (fun (file,size) ->

                /// Track files larger than the git lfs threshold with git lfs. If no threshold is set, track no files with git lfs
                match gitLfsThreshold with
                | Some thr when size > thr -> trackWithLFS file
                | _ -> trackWithAdd file
        )

        executeGitCommand verbosity repoDir ("add -u") |> ignore
        printfn "-----------------------------"

        // commit all changes
        let commitMessage =
            match tryGetFieldValueByName "CommitMessage" gitArgs with
            | None -> "Update"
            | Some s -> s

        // print git status if verbose
        // executeGitCommand repoDir ("status") |> ignore

        if verbosity >= 2 then 
            printfn "Commit tracked files" 
            printfn "git commit -m '%s'" commitMessage

        Fake.Tools.Git.Commit.exec repoDir commitMessage |> ignore
        
        executeGitCommand verbosity repoDir "branch -M main" |> ignore

        // detect existing remote
        let hasRemote () =
            let ok,msg,error = Fake.Tools.Git.CommandHelper.runGitCommand repoDir "remote -v"
            msg.Length>0

        // add remote if specified
        match tryGetFieldValueByName "RepositoryAdress" gitArgs with
            | None -> ()
            | Some remote ->
                if hasRemote() then executeGitCommand verbosity repoDir ("remote remove origin") |> ignore

                executeGitCommand verbosity repoDir ("remote add origin " + remote) |> ignore

        if verbosity >= 2 then
            if hasRemote() then printfn "Start syncing with remote" 
            else                printfn "Can not sync with remote as no remote repository adress was specified"

        // pull if remote exists
        if hasRemote() then
            if verbosity >= 2 then printfn "Pull" 
            executeGitCommand verbosity repoDir ("fetch origin") |> ignore
            executeGitCommand verbosity repoDir ("pull --rebase origin main") |> ignore

        // push if remote exists
        if hasRemote() then
            if verbosity >= 2 then printfn "Push"            
            executeGitCommand verbosity repoDir ("push -u origin main") |> ignore

