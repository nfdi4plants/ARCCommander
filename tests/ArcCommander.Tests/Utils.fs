module TestingUtils

open Expecto
open System.IO

open ARCtrl
open ARCtrl.ISA
open ARCtrl.ISA.Spreadsheet
open ARCtrl.NET
open ArcCommander
open ArgumentProcessing
open Argu

let rec directoryCopy srcPath dstPath copySubDirs =

    if not <| System.IO.Directory.Exists(srcPath) then
        let msg = System.String.Format("Source directory does not exist or could not be found: {0}", srcPath)
        raise (System.IO.DirectoryNotFoundException(msg))

    if not <| System.IO.Directory.Exists(dstPath) then
        System.IO.Directory.CreateDirectory(dstPath) |> ignore

    let srcDir = new System.IO.DirectoryInfo(srcPath)

    for file in srcDir.GetFiles() do
        let temppath = System.IO.Path.Combine(dstPath, file.Name)
        file.CopyTo(temppath, true) |> ignore

    if copySubDirs then
        for subdir in srcDir.GetDirectories() do
            let dstSubDir = System.IO.Path.Combine(dstPath, subdir.Name)
            directoryCopy subdir.FullName dstSubDir copySubDirs

type ArcInvestigation with

    member this.ContainsStudy(studyIdentifier : string) =
        this.StudyIdentifiers |> Seq.contains studyIdentifier

    member this.TryGetStudy(studyIdentifier : string) =
        if this.ContainsStudy studyIdentifier then 
            Some (this.GetStudy studyIdentifier)
        else
            None

    member this.DeregisterStudy(studyIdentifier : string) =
        this.RegisteredStudyIdentifiers.Remove(studyIdentifier)

type ArcStudy with
    member this.TryGetRegisteredAssayAt(index : int) = 
        this.RegisteredAssays
        |> Seq.tryItem index

    member this.TryGetRegisteredAssay(assayIdentifier : string) =
        this.RegisteredAssayIdentifiers 
        |> Seq.tryFindIndex((=) assayIdentifier)
        |> Option.bind this.TryGetRegisteredAssayAt


    ///
let floatsClose accuracy (seq1:seq<float>) (seq2:seq<float>) = 
    Seq.map2 (fun x1 x2 -> Accuracy.areClose accuracy x1 x2) seq1 seq2
    |> Seq.contains false
    |> not

let createConfigFromDir testListName testCaseName =
    let dir = Path.Combine(__SOURCE_DIRECTORY__, "TestResult", testListName, testCaseName)
    ArcConfiguration.GetDefault()
    |> ArcConfiguration.ofIniData
    |> fun c -> {c with General = (Map.ofList ["workdir", dir; "verbosity", "2"]) }

let standardISAArgs = 
    Map.ofList 
        [
            "investigationfilename","isa.investigation.xlsx";
            "studiesfilename","isa.study.xlsx";
            "assayfilename","isa.assay.xlsx"
        ]

let processCommand (arcConfiguration : ArcConfiguration) (commandF : _ -> ArcParseResults<'T> -> _) (r : 'T list when 'T :> IArgParserTemplate) =
    let g = groupArguments r
    Prompt.deannotateArguments g 
    |> commandF arcConfiguration

let processCommandWoArgs (arcConfiguration : ArcConfiguration) commandF = commandF arcConfiguration


module Result =

    let getMessage res =
        match res with
        | Ok m -> m
        | Error m -> m