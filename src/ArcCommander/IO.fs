namespace ArcCommander

module IO =

    let rec purgeAndDeleteDirectory (directoryPath:string) =
        System.IO.Directory.GetFiles(directoryPath)
        |> Array.iter System.IO.File.Delete

        System.IO.Directory.GetDirectories(directoryPath)
        |> Array.iter purgeAndDeleteDirectory

        System.IO.Directory.Delete directoryPath

    let findInvestigationFile (dir) =
        System.IO.DirectoryInfo(dir).GetFiles()
        |> Seq.tryFind (fun fi -> 
            fi.Name = "isa_investigation.xlsx"
        )
        |> fun p -> 
            match p with
            | Some f ->
                let path = f.FullName
                printfn "found investigation file %s" path
                path
            | None ->  
                failwith "could not find investigation file"