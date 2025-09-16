namespace ArcCommander.APIs

open System
open System.IO

open ArcCommander
open ArcCommander.CLIArguments

open ARCtrl
open ARCtrl.Json

module API =
    
    module ARC = 
        
        open ARCtrl.Process.Conversion

        let getProcesses (arc : ARC) =
            match arc.ISA with
            | Some inv ->

                let studyProcs = 
                    inv.Studies
                    |> Seq.collect (fun s -> s.GetProcesses())
                let assayProcs =
                    inv.Assays
                    |> Seq.collect (fun a -> a.GetProcesses())
                Seq.append studyProcs assayProcs
                |> Seq.toList

            | None -> []
            


/// ArcCommander API functions that get executed by top level subcommand verbs.
module ArcAPI = 

    let version _ =
        
        let log = Logging.createLogger "ArcVersionLog"

        log.Info($"Start Arc Version")
        
        let assembly = Reflection.Assembly.GetExecutingAssembly()
            
        let productVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion
        
        if productVersion.Contains "-" then 
            log.Debug($"v{productVersion}")
        else 
            let fv = assembly.GetName().Version
            log.Debug($"v{fv.Major}.{fv.Minor}.{fv.Build}")

    /// Initializes the ARC-specific folder structure.
    let init (arcConfiguration : ArcConfiguration) (arcArgs : ArcParseResults<ArcInitArgs>) =

        let log = Logging.createLogger "ArcInitLog"
        
        log.Info("Start Arc Init")

        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        let editor              = arcArgs.TryGetFieldValue ArcInitArgs.EditorPath
        let gitLFSThreshold     = arcArgs.TryGetFieldValue ArcInitArgs.GitLFSByteThreshold 
        let branch              = arcArgs.TryGetFieldValue ArcInitArgs.Branch |> Option.defaultValue GitHelper.defaultBranch
        let repositoryAddress   = arcArgs.TryGetFieldValue ArcInitArgs.RepositoryAddress 
        let identifier =    
            arcArgs.TryGetFieldValue ArcInitArgs.InvestigationIdentifier
            |> Option.defaultValue (DirectoryInfo(workDir).Name)       

        log.Trace("Create Directory")

        Directory.CreateDirectory workDir |> ignore

        log.Trace("Initiate folder structure")

        let isa = ArcInvestigation.create(identifier)
        ARC(isa).Write(arcConfiguration)     

        GeneralConfiguration.tryGetRootfolder arcConfiguration
        |> Option.iter (fun p -> 
            let dir = Path.Combine(workDir,p)
            Directory.CreateDirectory dir |> ignore
            let p = Path.Combine(dir,".gitkeep")
            File.WriteAllText(p,"")
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

            if arcArgs.ContainsFlag ArcInitArgs.Gitignore then
                log.Warn("The default GitIgnore is an experimental feature. Be careful and double check that all your wanted files are being tracked.")
                let gitignoreAppPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "defaultGitignore")
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

        let arc = ARC.load(arcConfiguration)
        arc.ISA
        |> Option.iter (fun isa -> 
            isa.UpdateIOTypeByEntityID()
        )
        arc.Update(arcConfiguration)

    /// Export the complete ARC as a JSON object.
    let export (arcConfiguration : ArcConfiguration) (arcArgs : ArcParseResults<ArcExportArgs>) =
    
        let log = Logging.createLogger "ArcExportLog"

        log.Info("Start Arc Export")
       
        let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration
                    
        let arc = ARC.load workDir

        let output =
            if arcArgs.ContainsFlag ArcExportArgs.ProcessSequence then
                API.ARC.getProcesses arc
                |> ARCtrl.Json.ProcessSequence.toISAJsonString(2)
            else 
                arc.ISA.Value.ToISAJsonString(2)

        match arcArgs.TryGetFieldValue Output with
        | Some p -> 
            let absolutePath = FileInfo(p).FullName
            File.WriteAllText(absolutePath, output)
        | None -> 
            log.Debug(output)
        
    /// Convert the complete ARC to a target format.
    let convert (arcConfiguration : ArcConfiguration) (arcArgs : ArcParseResults<ArcConvertArgs>) =
    
        let log = Logging.createLogger "ArcConvertLog"

        log.Fatal("Convert command currently disabled.")

        //log.Info("Start Arc Convert")
       
        //let workDir = GeneralConfiguration.getWorkDirectory arcConfiguration

        //let nameRoot = getFieldValueByName "Target" arcArgs
        //let converterName = $"arc-convert-{nameRoot}"
        //let repoOwner =  "nfdi4plants"
        //let repoName = "converters"

        //log.Info (converterName)

        //let assayIdentifier = tryGetFieldValueByName "AssayIdentifier" arcArgs
        //let studyIdentifier = tryGetFieldValueByName "StudyIdentifier" arcArgs

        //log.Info("Fetch converter dll")
       
        //let assembly = 
        //    if nameRoot.Contains ".dll" then
        //        System.Reflection.Assembly.LoadFile nameRoot
        //    else
        //        let dll = ArcConversion.getDll repoOwner repoName $"{converterName}.dll"
        //        System.Reflection.Assembly.Load dll
        //let converter = 
        //    ArcConversion.callMethodOfAssembly converterName "create" assembly :?> ARCconverter
        //log.Info("Load ARC")

        //let i,s,a = ArcConversion.getISA studyIdentifier assayIdentifier workDir

        //log.Info("Run conversion")
           
        //match converter with
        //| ARCtoCSV f -> 
        //    ArcConversion.handleCSV i s a workDir nameRoot converter
        //| ARCtoTSV f -> 
        //    ArcConversion.handleTSV i s a workDir nameRoot converter
        //| ARCtoXLSX f -> 
        //    ArcConversion.handleXLSX i s a workDir nameRoot converter
        //| ARCtoJSON f -> 
        //    ArcConversion.handleJSON i s a workDir nameRoot converter
        //| _ -> failwith "no other converter defined"
        //|> function 
        //   | Ok messages ->
        //        log.Info $"Successfully converted to {nameRoot}"
        //        ArcConversion.writeMessages workDir messages
        //   | Error messages ->
        //        ArcConversion.writeMessages workDir messages
        //        log.Error $"Arc could not be converted to {nameRoot}, as some required values could not be retreived"
        //        if ArcConversion.promptYesNo "Do you want missing fields to be written back into ARC? (y/n)" then
        //            ArcConversion.handleTransformations workDir converterName messages