namespace ArcCommander

open Logging
open ISADotNet
open ArcCommander.ArgumentProcessing

open System
open System.IO
open System.Diagnostics
open Argu
open arcIO.NET
open arcIO.NET.Converter
open ISADotNet.QueryModel
open FsSpreadsheet.DSL
open FsSpreadsheet.ExcelIO
open FSharp.Data
open Octokit

/// Functions for trying to run external tools, given the command line arguments can not be parsed.
module ArcConversion =

    type ARCconverter with
        member this.ConvertCSV(i,s,a) = 
            match this with
            | ARCtoCSV f -> f i s a
            | _ -> failwith "could not convert to csv"

        member this.ConvertTSV(i,s,a) = 
            match this with
            | ARCtoTSV f -> f i s a
            | _ -> failwith "could not convert to tsv"

        member this.ConvertXLSX(i,s,a) = 
            match this with
            | ARCtoXLSX f -> f i s a
            | _ -> failwith "could not convert to xlsx"

        member this.ConvertXML(i,s,a) = 
            match this with
            | ARCtoXML f -> f i s a
            | _ -> failwith "could not convert to xml"

    let getDll (repoOwner : string) (repoName : string) (dllname : string) =

        let client = new GitHubClient(new ProductHeaderValue("ArcCommander"));

        let releases = client.Repository.Release.GetAll(repoOwner, repoName);
        let latest = releases.Result.[0]
        let asset = 
            latest.Assets
            |> Seq.find (fun r -> r.Name = dllname)
        match Http.Request(asset.BrowserDownloadUrl).Body with
        | HttpResponseBody.Text s -> failwithf "no dll"
        | HttpResponseBody.Binary b -> b

    let callMethodOfAssembly (typeName : string) (methodName : string) (assembly : System.Reflection.Assembly) =
        let t = assembly.GetType(typeName)    
        let m = t.GetMethod(methodName)
        m.Invoke(null,[||])


    let getISA (studyIdentifier : string option) (assayIdentifier : string option) arcDir : QInvestigation*QStudy*QAssay= 
        let i = Investigation.fromArcFolder arcDir
        let s = 
            let s = 
                match studyIdentifier with
                | Option.Some si ->  i.Studies.Value |> List.find (fun s -> s.Identifier.Value = si)
                | None -> i.Studies.Value.Head
            let ps = 
                (s.ProcessSequence |> Option.defaultValue [])
                @
                (s.Assays |> Option.defaultValue [] |> List.collect (fun a -> a.ProcessSequence |> Option.defaultValue []))            
            {s with ProcessSequence = Option.Some ps}
        let a = 
            match assayIdentifier with
                | Option.Some ai ->  s.Assays.Value |> List.find (fun a -> a.FileName.Value = Assay.nameToFileName ai)
                | None -> s.Assays.Value.Head
        
        QInvestigation.fromInvestigation(i),
        QStudy.fromStudy(s),
        QAssay.fromAssay(a,[])

    let prompt (msg:string) =
        System.Console.Write(msg)
        System.Console.ReadLine().Trim()
        |> function | "" -> None | s -> Option.Some s
        |> Option.map (fun s -> s.Replace ("\"","\\\""))

    let rec promptYesNo msg =
        match prompt (sprintf "%s [Yn]: " msg) with
        | Option.Some "Y" | Option.Some "y" -> true
        | Option.Some "N" | Option.Some "n" -> false
        | _ -> System.Console.WriteLine("Sorry, invalid answer"); promptYesNo msg

    let handleTransformations arcDir namePrefix (messages : Message list) = 
        let i = Investigation.fromArcFolder arcDir
        let s = i.Studies.Value.Head
        let transformations = 
            messages
            |> ISADotNet.QueryModel.Linq.Spreadsheet.ErrorHandling.getStudyformations namePrefix
            |> List.distinct       
        let updatedStudy = 
            transformations
            |> List.fold (fun s transformation -> transformation.Transform s) s        
        let updatedArc = 
            i
            |> API.Investigation.mapStudies
                (API.Study.updateByIdentifier API.Update.UpdateAll updatedStudy)
            |> API.Investigation.update
        Study.overWrite arcDir updatedStudy
        Investigation.overWrite arcDir updatedArc

    let writeMessages (arcDir : string) (messages : Message list) =
        let messagesOutPath = Path.Combine(arcDir,".arc/OutputMessages.txt")
        messages
        |> List.map (fun m -> m.AsString())
        |> List.toArray
        |> Array.distinct
        |> fun messages -> 
            File.WriteAllLines(messagesOutPath,messages)

    let handleXLSX (i : QInvestigation) (s : QStudy) (a : QAssay) arcDir converterName (f : arcIO.NET.Converter.ARCconverter) =      
        let outPath = Path.Combine([|arcDir;".arc";$"{converterName}.xlsx"|])
        match f.ConvertXLSX(i,s,a) with
        | Some (workbook,messages) -> 
            let wb = workbook.Parse()
            wb.ToFile(outPath)
            Ok(messages)
        | NoneOptional messages | NoneRequired messages ->
            Error(messages)

    let handleCSV (i : QInvestigation) (s : QStudy) (a : QAssay) arcDir converterName (f : arcIO.NET.Converter.ARCconverter) =
        let outPath = Path.Combine([|arcDir;".arc";$"{converterName}.csv"|])
        match f.ConvertCSV(i,s,a) with
        | Some (workbook,messages) -> 
            let wb = workbook.Parse()
            FsSpreadsheet.CsvIO.Writer.toFile(outPath,wb,Separator = ',')
            Ok(messages)
        | NoneOptional messages | NoneRequired messages ->
            Error(messages)

    let handleTSV (i : QInvestigation) (s : QStudy) (a : QAssay) arcDir converterName (f : arcIO.NET.Converter.ARCconverter) =
        let outPath = Path.Combine([|arcDir;".arc";$"{converterName}.csv"|])
        match f.ConvertCSV(i,s,a) with
        | Some (workbook,messages) -> 
            let wb = workbook.Parse()
            FsSpreadsheet.CsvIO.Writer.toFile(outPath,wb,Separator = '\t')
            Ok(messages)
        | NoneOptional messages | NoneRequired messages ->
            Error(messages)