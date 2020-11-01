namespace ArcCommander

open Argu 
open System
open ISA
open DataModel
open ISA_XLSX.IO
module CLIArguments = 


//[<CliPrefix(CliPrefix.Dash)>]
//type StudyArgs =
//    | [<Unique>]Identifier of string
//    | [<Unique>]Title of string
//    | [<Unique>]Description of string

//    interface IArgParserTemplate with
//        member this.Usage =
//            match this with
//            | Identifier _->    "Identifier of the study"
//            | Title _->         "Title of the study"
//            | Description _ ->  "Description of the study"

//    member this.mapStudy(study:InvestigationFile.StudyItem) =
//        match this with
//        | Identifier x -> study.Identifier <- x
//        | Title x -> study.Title <- x
//        | Description x -> study.Description <- x

    /// ------------ Investigation Arguments ------------ ///

    type InvestigationParams = 
        | [<Unique>] Identifier of string
        | [<Unique>] Title of string
        | [<Unique>] Description of string
        | [<Unique>] SubmissionDate of string
        | [<Unique>] PublicReleaseDate of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Identifier _->    "Identifier of the investigation, will be used as the file name of the investigation file"
                | Title _->         "Title of the investigation"
                | Description _->   "Description of the investigation"
                | SubmissionDate _->   "Submission Date of the investigation"
                | PublicReleaseDate _->   "Public Release Date of the investigation"
    
    and Investigation = 
        
        | [<CliPrefix(CliPrefix.None)>] Update of ParseResults<InvestigationParams>
        | [<CliPrefix(CliPrefix.None)>] Init of ParseResults<InvestigationParams>
        | [<CliPrefix(CliPrefix.None)>][<SubCommand>] Remove
        | [<CliPrefix(CliPrefix.None)>][<SubCommand>] Edit

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Update            _ -> "The investigation gets updated with the given parameters"
                | Init               _ -> "Create a new investigation file with the given parameters"
                | Remove            _ -> "Removes the investigation from the arc"
                | Edit              _ -> "Open an editor window to directly edit the isa investigation file"

    /// ------------ ASSAY ARGUMENTS ------------ ///

    type TargetStudy =
        | [<Unique>] TargetStudyIdentifier of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | TargetStudyIdentifier _-> "Target study"



    and AssayParams =  
        | [<Unique>]MeasurementType of string
        | [<Unique>]MeasurementTypeTermAccessionNumber of string
        | [<Unique>]MeasurementTypeTermSourceREF of string
        | [<Unique>]TechnologyType of string
        | [<Unique>]TechnologyTypeTermAccessionNumber of string
        | [<Unique>]TechnologyTypeTermSourceREF of string
        | [<Unique>]TechnologyPlatform of string
        
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | MeasurementType _                     -> "Measurement type of the assay"
                | MeasurementTypeTermAccessionNumber _  -> "Measurement type Term Accession Number of the assay"
                | MeasurementTypeTermSourceREF _        -> "Measurement type Term Source REF of the assay"
                | TechnologyType _                      -> "Technology Type of the assay"
                | TechnologyTypeTermAccessionNumber _   -> "Technology Type Term Accession Number of the assay"
                | TechnologyTypeTermSourceREF _         -> "Technology Type Term Source REF of the assay"
                | TechnologyPlatform _                  -> "Technology Platform of the assay"

        //member this.mapAssay(assay:InvestigationFile.Assay) =
        //    match this with
        //    | MeasurementType x -> assay.MeasurementType <- x
        //    | _ -> ()


    and Assay = 
        | [<AltCommandLine("-s")>][<InheritAttribute>][<Unique>] StudyIdentifier of string
        | [<AltCommandLine("-a")>][<InheritAttribute>][<Unique>] AssayIdentifier of string
        | [<CliPrefix(CliPrefix.None)>][<SubCommand>] Create
        | [<CliPrefix(CliPrefix.None)>] Register of ParseResults<AssayParams>
        | [<CliPrefix(CliPrefix.None)>] Update of ParseResults<AssayParams>
        | [<CliPrefix(CliPrefix.None)>] Add of ParseResults<AssayParams>
        | [<CliPrefix(CliPrefix.None)>] Move of ParseResults<TargetStudy>
        | [<CliPrefix(CliPrefix.None)>][<SubCommand>] Remove
        | [<CliPrefix(CliPrefix.None)>][<SubCommand>] Edit 

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | StudyIdentifier   _ -> "Name of the study in which the assay is situated"
                | AssayIdentifier   _ -> "Name of the assay of interest"
                | Create            _ -> "A new assay file structure of the given name is created"
                | Register          _ -> "Registers an existing assay to the study"
                | Update            _ -> "The specified assay gets updated with the given parameters"
                | Add               _ -> "A new assay file structure of the given name is created and the registered to the study"
                | Move              _ -> "Moves the assay from one study to another"
                | Remove            _ -> "Removes the assay from the arc"
                | Edit              _ -> "Open an editor window for manipulating the assay parameters"



    and ArcArgs =
        | [<AltCommandLine("-p")>][<Unique>] WorkingDir of string
        | [<CliPrefix(CliPrefix.None)>][<SubCommand>] Init
        | [<AltCommandLine("i")>][<CliPrefix(CliPrefix.None)>] Investigation of ParseResults<Investigation>
        //| [<CliPrefix(CliPrefix.None)>] AddAssay of ParseResults<AssayParams>
        //| [<CliPrefix(CliPrefix.None)>] AddWorkflow of ParseResults<WorkflowArgs>
        | [<AltCommandLine("a")>][<CliPrefix(CliPrefix.None)>] Assay of ParseResults<Assay>

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | WorkingDir _ -> "Set the base directory of your ARC"
                | Init _ -> "Initializes basic folder structure"
                | Investigation _ -> "Investigation file functions"
                //| AddAssay _ -> "Adds a new assay to the given study, creates a new study if not existent"
                //| AddWorkflow _ -> "Not yet implemented"
                | Assay _ ->  "Assay functions"
