namespace ArcCommander.APIs

open System
open ArcCommander
open ArgumentProcessing
open Fake.Tools.Git
// open Fake.DotNet
// open Fake.IO
// open Fake.IO.FileSystemOperators
// open Fake.IO.Globbing.Operators
// open Fake.Tools

/// ArcCommander Configuration API functions that get executed by the configuration focused subcommand verbs

module GitAPI =

    let getRepoDir (arcConfiguration:ArcConfiguration) =
        let workdir = GeneralConfiguration.getWorkDirectory arcConfiguration + "/../TEMP/arc0/"
        let gitDir = Fake.Tools.Git.CommandHelper.findGitDir(workdir).FullName
        gitDir.Substring(0,gitDir.Length-4)

    let update (arcConfiguration:ArcConfiguration) (gitArgs:Map<string,Argument>) =
        printfn "-----------------------------"

        let repoDir = getRepoDir(arcConfiguration)

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

                if size>1500000L
                  then trackWithLFS file
                  else trackWithAdd file
        )
        printfn "-----------------------------"
        Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir ("commit -m 'Update'") |> ignore

    let push (arcConfiguration:ArcConfiguration) (gitArgs:Map<string,Argument>) =
        let repoDir = getRepoDir(arcConfiguration)
        Fake.Tools.Git.CommandHelper.directRunGitCommand repoDir ("push origin master") |> ignore

    let init (arcConfiguration:ArcConfiguration) (gitArgs:Map<string,Argument>) =
        let workdir = GeneralConfiguration.getWorkDirectory arcConfiguration + "/../TEMP/arc0/"
        Fake.Tools.Git.CommandHelper.directRunGitCommand workdir ("init") |> ignore
        Fake.Tools.Git.CommandHelper.directRunGitCommand workdir ("lfs install") |> ignore
