namespace ArcCommander


open Argu 

open System
open ISA
open DataModel

open ISA_XLSX.IO


[<CliPrefix(CliPrefix.Dash)>]
type InvestigationArgs = 
    | [<Unique>]Identifier of string
    | [<Unique>]Title of string
    | [<Unique>]Description of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _->    "Identifier of the investigation, will be used as the file name of the investigation file"
            | Title _->         "Title of the investigation"
            | Description _->   "Description of the investigation"

    member this.mapInvestigation(inv:InvestigationFile.InvestigationItem) =
        match this with
        | Identifier x -> inv.Identifier <- x
        | Title x -> inv.Title <- x
        | Description x -> inv.Description <- x

[<CliPrefix(CliPrefix.Dash)>]
type StudyArgs =
    | [<Unique>]Identifier of string
    | [<Unique>]Title of string
    | [<Unique>]Description of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _->    "Identifier of the study"
            | Title _->         "Title of the study"
            | Description _ ->  "Description of the study"

    member this.mapStudy(study:InvestigationFile.StudyItem) =
        match this with
        | Identifier x -> study.Identifier <- x
        | Title x -> study.Title <- x
        | Description x -> study.Description <- x

and AssayArgs =
    | [<Unique>]StudyIdentifier of string 
    | [<Unique>]Identifier of string
    | [<Unique>]MeasurementType of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier _-> "The new assay gets added to the study with this identifier"
            | Identifier _-> "Identifier of the assay, will be used for folder and assay file names"
            | MeasurementType _-> "Measurment type of the assay"

    member this.mapAssay(assay:InvestigationFile.Assay) =
        match this with
        | Identifier x -> assay.FileName <- x
        | MeasurementType x -> assay.MeasurementType <- x
        | _ -> ()

and WorkflowArgs = 
    | [<Unique>]Identifier of string 

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier _-> "Identifier of the workflow"

and ArcArgs =
    | [<Unique>] WorkingDir of string
    | [<CliPrefix(CliPrefix.None)>] InitArc of ParseResults<InvestigationArgs>
    | [<CliPrefix(CliPrefix.None)>] AddStudy of ParseResults<StudyArgs>
    | [<CliPrefix(CliPrefix.None)>] AddAssay of ParseResults<AssayArgs>
    | [<CliPrefix(CliPrefix.None)>] AddWorkflow of ParseResults<WorkflowArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | WorkingDir _ -> "Set the base directory of your ARC"
            | InitArc _ -> "Initializes basic folder structure and the investigation fole of the arc"
            | AddStudy _ -> "Adds a new study to the arc"
            | AddAssay _ -> "Adds a new assay to the given study, creates a new study if not existent"
            | AddWorkflow _ -> "Not yet implemented"


module ArgumentMatching = 

    let (|AddAssay|_|) (results: ParseResults<ArcArgs>) =
        match results.TryGetResult(ArcArgs.AddAssay) with
        | Some (r) -> 
            let identifier = 
                match r.TryGetResult(AssayArgs.Identifier) with 
                | Some x -> x 
                | None -> 
                    printfn "Please Input assay identifier"
                    Console.ReadLine()
            let studyIdentifier = 
                match r.TryGetResult(AssayArgs.Identifier) with 
                | Some x -> x 
                | None -> 
                    printfn "Please Input study identifier to which the assay should be added. If left empty, the study will have the same name as the assay"
                    match Console.ReadLine() with
                    | "" -> identifier
                    | x -> x
            let assay = InvestigationFile.Assay(fileName=identifier)
            r.GetAllResults()
            |> List.iter (fun x -> x.mapAssay assay)

            (studyIdentifier,assay)
            |> Some
        | _ -> None

    let (|AddStudy|_|) (results: ParseResults<ArcArgs>) =
        match results.TryGetResult(ArcArgs.AddStudy) with
        | Some (r) -> 
            let identifier = 
                match r.TryGetResult(StudyArgs.Identifier) with 
                | Some x -> x 
                | None -> 
                    printfn "Please Input assay identifier"
                    Console.ReadLine()

            let study = InvestigationFile.StudyItem(identifier=identifier)
            r.GetAllResults()
            |> List.iter (fun x -> x.mapStudy study)

            study
            |> Some
        | _ -> None

    let (|InitArc|_|) (results: ParseResults<ArcArgs>) =
        match results.TryGetResult(ArcArgs.InitArc) with
        | Some (r) -> 
            let identifier = 
                match r.TryGetResult(InvestigationArgs.Identifier) with 
                | Some x -> x 
                | None -> 
                    printfn "Please Input investigation identifier"
                    Console.ReadLine()
            let investigation = ISA.DataModel.InvestigationFile.InvestigationItem(identifier=identifier)
            r.GetAllResults()
            |> List.iter (fun x -> x.mapInvestigation investigation)

            investigation
            |> Some
        | _ -> None

module Arc =

    // Creates Arc specific folder structure 
    let init workingDir (inv : InvestigationFile.InvestigationItem) =

        let dir = System.IO.Directory.CreateDirectory workingDir
        dir.CreateSubdirectory "assays"     |> ignore
        dir.CreateSubdirectory "codecaps"   |> ignore
        dir.CreateSubdirectory "externals"  |> ignore
        dir.CreateSubdirectory "runs"       |> ignore

        let investigationFilePath = dir.FullName + "/" + inv.Identifier + ".xlsx"

        inv
        |> ISA_XLSX.IO.ISA_Investigation.createEmpty investigationFilePath 

    // Returns true if called anywhere in an arc 
    let isArc () =
        NotImplementedException() |> raise

    let findInvestigationFile (dir) =
        System.IO.DirectoryInfo(dir).GetFiles()
        |> Seq.find (fun fi -> 
            fi.Name.StartsWith "i_"
            &&
            fi.Name.EndsWith ".xlsx"
        )
        |> fun x -> x.FullName

    let addAssay workingDir studyIdentifier (assay : InvestigationFile.Assay) =

        let name = assay.FileName

        let dir = System.IO.Directory.CreateDirectory (workingDir + @"\assays\" + name)

        System.IO.File.Create (dir.FullName + "\dataset")   |> ignore
        System.IO.File.Create (dir.FullName + "\protocols") |> ignore
        System.IO.File.Create (dir.FullName + "\isa.tab")   |> ignore

        let investigationFilePath = findInvestigationFile workingDir
        
        let doc = FSharpSpreadsheetML.Spreadsheet.openSpreadsheet investigationFilePath true
        ISA_Investigation.addItemToStudy assay studyIdentifier doc
        doc.Save()
        doc.Close()

