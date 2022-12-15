namespace ArcCommander.APIs

open System
open System.IO

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISADotNet
open ISADotNet.XLSX

/// ArcCommander API functions that get executed by top level subcommand verbs.
module ArcAPI = 

    let version _ =
        
        let log = Logging.createLogger "ArcVersionLog"

        log.Info($"Start Arc Version")
        
        let ver = Reflection.Assembly.GetExecutingAssembly().GetName().Version

        log.Debug($"v{ver.Major}.{ver.Minor}.{ver.Build}")

    // TODO TO-DO TO DO: make use of args
    /// Initializes the ARC-specific folder structure.
    let init (arcConfiguration : ArcConfiguration) (arcArgs : Map<string,Argument>) =

        let log = Logging.createLogger "ArcInitLog"
        
        log.Info("Start Arc Init")

        let workDir = 
            let wd = GeneralConfiguration.getWorkDirectory arcConfiguration
            if System.IO.Directory.GetFileSystemEntries wd |> Array.isEmpty then
                wd
            else 
                let dir = System.IO.Directory.CreateDirectory(wd)
                dir.FullName

        let editor              = tryGetFieldValueByName "EditorPath"           arcArgs
        let gitLFSThreshold     = tryGetFieldValueByName "GitLFSByteThreshold"  arcArgs
        let branch              = tryGetFieldValueByName "Branch"               arcArgs |> Option.defaultValue "main"
        let repositoryAddress   = tryGetFieldValueByName "RepositoryAddress"    arcArgs 


        log.Trace("Create Directory")

        Directory.CreateDirectory workDir |> ignore

        log.Trace("Initiate folder structure")

        ArcConfiguration.getRootFolderPaths arcConfiguration
        |> Array.iter (
            Directory.CreateDirectory 
            >> fun dir -> File.Create(Path.Combine(dir.FullName, ".gitkeep")) |> ignore 
        )

        log.Trace("Set configuration")

        match editor with
        | Some editorValue -> 
            let path = IniData.getLocalConfigPath workDir
            IniData.setValueInIniPath path "general.editor" editorValue
        | None -> ()

        match gitLFSThreshold with
        | Some gitLFSThresholdValue -> 
            let path = IniData.getLocalConfigPath workDir
            IniData.setValueInIniPath path "general.gitlfsbytethreshold" gitLFSThresholdValue
        | None -> ()

        log.Trace("Init Git repository")

        try

            GitHelper.executeGitCommand workDir $"init -b {branch}"
            //GitHelper.executeGitCommand workDir $"add ."
            //GitHelper.executeGitCommand workDir $"commit -m \"Initial commit\""

            if containsFlag "Gitignore" arcArgs then
                let gitignoreAppPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".gitignore")
                let gitignoreArcPath = Path.Combine(workDir, ".gitignore")
                log.Trace($"Copy .gitignore from {gitignoreAppPath} to {gitignoreArcPath}")
                File.Copy(gitignoreAppPath, gitignoreArcPath)

            log.Trace("Add remote repository")
            match repositoryAddress with
            | None -> ()
            | Some remote ->
                GitHelper.executeGitCommand workDir $"remote add origin {remote}"
                //GitHelper.executeGitCommand workDir $"branch -u origin/{branch} {branch}"

        with 
        | e -> 

            log.Error($"Git could not be set up. Please try installing Git cli and run `arc git init`.\n\t{e}")

    /// Update the investigation file with the information from the other files and folders.
    let update (arcConfiguration : ArcConfiguration) =

        let log = Logging.createLogger "ArcUpdateLog"
        
        log.Info("Start Arc Update")

        let assayRootFolder = AssayConfiguration.getRootFolderPath arcConfiguration

        let investigationFilePath = IsaModelConfiguration.getInvestigationFilePath arcConfiguration

        let assayNames = 
            DirectoryInfo(assayRootFolder).GetDirectories()
            |> Array.map (fun d -> d.Name)
            
        let investigation =
            try Investigation.fromFile investigationFilePath 
            with
            | :? FileNotFoundException -> 
                Investigation.empty
            | err -> 
                log.Fatal($"{err.ToString()}")
                raise (Exception(""))

        let rec updateInvestigationAssays (assayNames : string list) (investigation : Investigation) =
            match assayNames with
            | a :: t ->
                let assayFilePath = IsaModelConfiguration.getAssayFilePath a arcConfiguration
                let assayFileName = IsaModelConfiguration.getAssayFileName a arcConfiguration
                let persons,assay = AssayFile.Assay.fromFile assayFilePath
                let factors = API.Assay.getFactors assay |> Option.defaultValue []
                let protocols = API.Assay.getProtocols assay |> Option.defaultValue []
                let studies = investigation.Studies

                match studies with
                | Some studies ->
                    match studies |> Seq.tryFind (API.Study.getAssays >> Option.defaultValue [] >> API.Assay.existsByFileName assayFileName) with
                    | Some study -> 
                        study
                        |> API.Study.mapAssays (API.Assay.updateByFileName API.Update.UpdateByExistingAppendLists assay)
                        |> API.Study.mapFactors (List.append factors >> List.distinctBy (fun f -> f.Name))
                        |> API.Study.mapProtocols (List.append protocols >> List.distinctBy (fun p -> p.Name))
                        |> API.Study.mapContacts (List.append persons >> List.distinctBy (fun p -> p.FirstName,p.LastName))
                        |> fun s -> API.Study.updateBy ((=) study) API.Update.UpdateAll s studies
                    | None ->
                        Study.fromParts (Study.StudyInfo.create a "" "" "" "" "" []) [] [] factors [assay] protocols persons
                        |> API.Study.add studies
                | None ->
                    [Study.fromParts (Study.StudyInfo.create a "" "" "" "" "" []) [] [] factors [assay] protocols persons]
                |> API.Investigation.setStudies investigation
                |> updateInvestigationAssays t
            | [] -> investigation

        updateInvestigationAssays (assayNames |> List.ofArray) investigation
        |> Investigation.toFile investigationFilePath

    /// Export the complete ARC as a JSON object.
    let export (arcConfiguration : ArcConfiguration) (arcArgs : Map<string,Argument>) =
    
        let log = Logging.createLogger "ArcExportLog"

        log.Info("Start Arc Export")
       
        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
    
        let investigation = arcIO.NET.Investigation.fromArcFolder workDir

        if containsFlag "ProcessSequence" arcArgs then

            let output = 
                investigation.Studies 
                |> Option.defaultValue [] |> List.collect (fun s -> 
                    s.Assays
                    |> Option.defaultValue [] |> List.collect (fun a -> 
                        a.ProcessSequence |> Option.defaultValue []
                    )
                )

            match tryGetFieldValueByName "Output" arcArgs with
            | Some p -> ArgumentProcessing.serializeToFile p output
            | None -> ()

            //System.Console.Write(ArgumentProcessing.serializeToString output)
            log.Debug(ArgumentProcessing.serializeToString output)
        else 
               
            match tryGetFieldValueByName "Output" arcArgs with
            | Some p -> ISADotNet.Json.Investigation.toFile p investigation
            | None -> ()

            //System.Console.Write(ISADotNet.Json.Investigation.toString output)
            log.Debug(ISADotNet.Json.Investigation.toString investigation)





    let import (arcConfiguration : ArcConfiguration) (arcArgs : Map<string,Argument>) =

        let log = Logging.createLogger "ArcImportLog"
        
        log.Info("Start Arc Import")

        let investigation = 
            match tryGetFieldValueByName "ArcJson" arcArgs with
            | Some b -> 
                b
                |> Json.Investigation.fromString
            | None -> 
                match tryGetFieldValueByName "ArcJsonFilePath" arcArgs with
                | Some p -> 
                    p
                    |> Json.Investigation.fromFile
                | None ->
                    log.Error("Couldn't import ARC, neither json blob nor json file provided")
                    failwith ""
            |> fun i -> {i with Remarks = []}

        let workDir = 
            let wd = GeneralConfiguration.getWorkDirectory arcConfiguration
            if System.IO.Directory.GetFileSystemEntries wd |> Array.isEmpty then
                wd
            else 
                match investigation.Identifier with
                | Some i -> 
                    let dir = System.IO.Directory.CreateDirectory(Path.Combine(wd,i))
                    dir.FullName
                | None -> 
                    log.Error("Couldn't import ARC, Target Folder is not empty and investigation has no Identifier needed to create an alternative subfolder.")
                    failwith ""

        let repositoryAddress = tryGetFieldValueByName "RepositoryAddress" arcArgs


        arcIO.NET.Arc.importFromInvestigation workDir investigation
       
        log.Trace("Init Git repository")

        try

            GitHelper.executeGitCommand workDir $"init"

            log.Trace("Add remote repository")
            match repositoryAddress with
            | None -> ()
            | Some remote ->
                GitHelper.executeGitCommand workDir $"remote add origin {remote}"
                //GitHelper.executeGitCommand workDir $"branch -u origin/{branch} {branch}"

        with 
        | e -> 
            log.Error($"Git could not be set up. Please try installing Git cli and run `arc git init`.\n\t{e}")

    /// Returns true if called anywhere in an ARC.
    let isArc (arcConfiguration : ArcConfiguration) (arcArgs : Map<string,Argument>) = raise (NotImplementedException())