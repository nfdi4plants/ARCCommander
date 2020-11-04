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


    /// ------------ Arc Arguments ------------ ///

    type ArcParams = 

        | [<Unique>] Owner of string
        | [<Unique>] RepositoryAdress of string
        | [<Unique>] EditorPath of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Owner _               ->  "Owner of the arc"
                | RepositoryAdress _    ->  "Github adress"
                | EditorPath _          ->  "The path leading to the editor used for text prompts (Default in Windows is notepad)"


    /// ------------ Investigation Arguments ------------ ///

    type InvestigationParams = 
        | [<Mandatory>][<Unique>] Identifier of string
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

    /// ------------ STUDY ARGUMENTS ------------ ///

    type StudyFull =
        | [<Mandatory>][<Unique>] Identifier of string
        | [<Unique>] Title of string
        | [<Unique>] Description of string
        | [<Unique>] SubmissionDate of string
        | [<Unique>] PublicReleaseDate of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Identifier _->    "Identifier of the study, will be used as the file name of the study file"
                | Title _->         "Title of the study"
                | Description _->   "Description of the study"
                | SubmissionDate _->   "Submission Date of the study"
                | PublicReleaseDate _->   "Public Release Date of the study"

    and StudyBasic =
        | [<Mandatory>][<Unique>] Identifier of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Identifier _->    "Identifier of the study, will be used as the file name of the study file"

    and Study =

        | [<CliPrefix(CliPrefix.None)>] Update of ParseResults<StudyFull>
        | [<CliPrefix(CliPrefix.None)>] Register of ParseResults<StudyFull>
        | [<CliPrefix(CliPrefix.None)>] Remove of ParseResults<StudyBasic>
        | [<CliPrefix(CliPrefix.None)>] Edit of ParseResults<StudyBasic>

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Update    _ -> "The study gets updated with the given parameters"
                | Register  _ -> "Create a new Study"
                | Remove    _ -> "Removes the Study from the arc"
                | Edit      _ -> "Open an editor window to directly edit the isa Study file"

    /// ------------ ASSAY ARGUMENTS ------------ ///


    type AssayBasic =
        | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of string
        | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | StudyIdentifier   _ -> "Name of the study in which the assay is situated"
                | AssayIdentifier   _ -> "Name of the assay of interest"

    and AssayMove =
        | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of string
        | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of string
        | [<Mandatory>][<AltCommandLine("-t")>][<Unique>] TargetStudyIdentifier of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | StudyIdentifier   _ -> "Name of the study in which the assay is situated"
                | AssayIdentifier   _ -> "Name of the assay of interest"
                | TargetStudyIdentifier _-> "Target study"

    and AssayFull =  
        | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of string
        | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of string
        | [<Mandatory>][<Unique>]MeasurementType of string
        | [<Mandatory>][<Unique>]MeasurementTypeTermAccessionNumber of string
        | [<Mandatory>][<Unique>]MeasurementTypeTermSourceREF of string
        | [<Mandatory>][<Unique>]TechnologyType of string
        | [<Mandatory>][<Unique>]TechnologyTypeTermAccessionNumber of string
        | [<Mandatory>][<Unique>]TechnologyTypeTermSourceREF of string
        | [<Mandatory>][<Unique>]TechnologyPlatform of string
        
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | StudyIdentifier   _                   -> "Name of the study in which the assay is situated"
                | AssayIdentifier   _                   -> "Name of the assay of interest"
                | MeasurementType _                     -> "Measurement type of the assay"
                | MeasurementTypeTermAccessionNumber _  -> "Measurement type Term Accession Number of the assay"
                | MeasurementTypeTermSourceREF _        -> "Measurement type Term Source REF of the assay"
                | TechnologyType _                      -> "Technology Type of the assay"
                | TechnologyTypeTermAccessionNumber _   -> "Technology Type Term Accession Number of the assay"
                | TechnologyTypeTermSourceREF _         -> "Technology Type Term Source REF of the assay"
                | TechnologyPlatform _                  -> "Technology Platform of the assay"

    and Assay = 
        | [<CliPrefix(CliPrefix.None)>] Create of ParseResults<AssayBasic>
        | [<CliPrefix(CliPrefix.None)>] Register of ParseResults<AssayFull>
        | [<CliPrefix(CliPrefix.None)>] Update of ParseResults<AssayFull>
        | [<CliPrefix(CliPrefix.None)>] Add of ParseResults<AssayFull>
        | [<CliPrefix(CliPrefix.None)>] Move of ParseResults<AssayMove>
        | [<CliPrefix(CliPrefix.None)>] Remove of ParseResults<AssayBasic>
        | [<CliPrefix(CliPrefix.None)>] Edit of ParseResults<AssayBasic>
        | [<CliPrefix(CliPrefix.None)>][<SubCommand>] List

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Create            _ -> "A new assay file structure of the given name is created"
                | Register          _ -> "Registers an existing assay to the study"
                | Update            _ -> "The specified assay gets updated with the given parameters"
                | Add               _ -> "A new assay file structure of the given name is created and the registered to the study"
                | Move              _ -> "Moves the assay from one study to another"
                | Remove            _ -> "Removes the assay from the arc"
                | Edit              _ -> "Open an editor window for manipulating the assay parameters"
                | List              _ -> "Lists all Assays registered in the investigation file"
    
    /// ------------ TOP LEVEL ------------ ///

    type Arc =
        | [<AltCommandLine("-p")>][<Unique>] WorkingDir of string
        | [<CliPrefix(CliPrefix.None)>][<SubCommand>] Init of ParseResults<ArcParams>
        | [<AltCommandLine("i")>][<CliPrefix(CliPrefix.None)>] Investigation of ParseResults<Investigation>
        | [<AltCommandLine("s")>][<CliPrefix(CliPrefix.None)>] Study of ParseResults<Study>
        //| [<CliPrefix(CliPrefix.None)>] AddWorkflow of ParseResults<WorkflowArgs>
        | [<AltCommandLine("a")>][<CliPrefix(CliPrefix.None)>] Assay of ParseResults<Assay>

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | WorkingDir _ -> "Set the base directory of your ARC"
                | Init _ -> "Initializes basic folder structure"
                | Investigation _ -> "Investigation file functions"
                | Study         _ -> "Study functions"
                //| AddWorkflow _ -> "Not yet implemented"
                | Assay _ ->  "Assay functions"
