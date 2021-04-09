namespace ArcCommander.APIs

open System
open ArcCommander
open ArgumentProcessing
open Fake.Tools.Git

module GitAPI =

    let getRepoDir (arcConfiguration:ArcConfiguration) =
        let workdir = GeneralConfiguration.getWorkDirectory arcConfiguration
        printfn "%s" workdir

        let gitDir = Fake.Tools.Git.CommandHelper.findGitDir(workdir).FullName
        gitDir.Substring(0,gitDir.Length-4)

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
            if Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir ("add "+file)
            then printfn "git add %s" file
            else printfn "[ERROR] git add %s" file

        let trackWithLFS (file:string) =
            if Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir ("lfs track "+file)
            then printfn "git lfs track %s" file; trackWithAdd (repoDir+".gitattributes")
            else printfn "[ERROR] git lfs track %s" file

        allFilesPlusSizes |> List.iter(
            fun pair ->
                let (file,size) = pair

                if size>150000000L
                  then trackWithLFS file
                  else trackWithAdd file
        )
        printfn "git add -u"
        Fake.Tools.Git.CommandHelper.runGitCommand repoDir ("add -u") |> ignore
        printfn "-----------------------------"

        // commit all changes
        let commitMessage =
            match tryGetFieldValueByName "CommitMessage" gitArgs with
            | None -> "Update"
            | Some s -> s
        // printfn "%A" (Fake.Tools.Git.CommandHelper.runGitCommand repoDir ("status"))
        printfn "commit -m '%s'" commitMessage
        Fake.Tools.Git.Commit.exec repoDir commitMessage |> ignore

        // add remote if specified
        match tryGetFieldValueByName "RepositoryAdress" gitArgs with
            | Some "" | None -> ()
            | Some remote ->
                Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir ("remote remove origin") |> ignore
                Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir ("remote add origin "+remote) |> ignore

        // detect existing remote
        let hasRemote () =
            let ok,msg,error = Fake.Tools.Git.CommandHelper.runGitCommand repoDir "remote -v"
            msg.Length>0

        // pull if remote exists
        if hasRemote() then
            printfn "git fetch origin"
            Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir ("fetch origin") |> ignore
            printfn "git pull --rebase origin master"
            Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir ("pull --rebase origin master") |> ignore

        // push if remote exists
        if hasRemote() then
            printfn "git push origin master"
            Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir ("push origin master") |> ignore

