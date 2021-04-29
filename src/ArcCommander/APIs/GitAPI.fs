namespace ArcCommander.APIs

open System
open ArcCommander
open ArgumentProcessing
open Fake.Tools.Git
open Fake.IO

module GitAPI =

    let executeGitCommand (repoDir:string) (command:string) =
        printfn "git %s" command
        let success = Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir command
        if not success
        then printfn "[ERROR]"
        success

    let getRepoDir (arcConfiguration:ArcConfiguration) =
        let workdir = GeneralConfiguration.getWorkDirectory arcConfiguration
        let gitDir = Fake.Tools.Git.CommandHelper.findGitDir(workdir).FullName

        Fake.IO.Path.getDirectory(gitDir)

    let update (arcConfiguration:ArcConfiguration) (gitArgs:Map<string,Argument>) =

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
            executeGitCommand repoDir ("add "+file) |> ignore

        let trackWithLFS (file:string) =
            executeGitCommand repoDir ("lfs track "+file) |> ignore
            trackWithAdd (repoDir+".gitattributes")

        allFilesPlusSizes |> List.iter(
            fun pair ->
                let (file,size) = pair

                if size>150000000L
                  then trackWithLFS file
                  else trackWithAdd file
        )
        executeGitCommand repoDir ("add -u") |> ignore
        printfn "-----------------------------"

        // commit all changes
        let commitMessage =
            match tryGetFieldValueByName "CommitMessage" gitArgs with
            | Some "" | None -> "Update"
            | Some s -> s

        // print git status if verbose
        // executeGitCommand repoDir ("status") |> ignore

        printfn "commit -m '%s'" commitMessage
        Fake.Tools.Git.Commit.exec repoDir commitMessage |> ignore

        // detect existing remote
        let hasRemote () =
            let ok,msg,error = Fake.Tools.Git.CommandHelper.runGitCommand repoDir "remote -v"
            msg.Length>0

        // add remote if specified
        match tryGetFieldValueByName "RepositoryAdress" gitArgs with
            | Some "" | None -> ()
            | Some remote ->
                if hasRemote() then executeGitCommand repoDir ("remote remove origin") |> ignore

                executeGitCommand repoDir ("remote add origin "+remote) |> ignore

        // pull if remote exists
        if hasRemote() then
            executeGitCommand repoDir ("fetch origin") |> ignore
            executeGitCommand repoDir ("pull --rebase origin master") |> ignore

        // push if remote exists
        if hasRemote() then
            executeGitCommand repoDir ("push origin master") |> ignore

