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


    type EmptyArgs =
        | [<Hidden;NoCommandLine>] NoArgs
    
        interface IArgParserTemplate with
            member __.Usage = ""

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
    type InvestigationId = 
        | [<Mandatory>][<Unique>] Identifier of string
     
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Identifier _->    "Identifier of the investigation"


    type InvestigationParams = 
        | [<Mandatory>][<Unique>] Identifier of string
        | [<Unique>] Title of string
        | [<Unique>] Description of string
        | [<Unique>] SubmissionDate of string
        | [<Unique>] PublicReleaseDate of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Identifier _->    "Identifier of the investigation"
                | Title _->         "Title of the investigation"
                | Description _->   "Description of the investigation"
                | SubmissionDate _->   "Submission Date of the investigation"
                | PublicReleaseDate _->   "Public Release Date of the investigation"
    
    type Investigation = 
        
        | [<CliPrefix(CliPrefix.None)>] Update of ParseResults<InvestigationParams>
        | [<CliPrefix(CliPrefix.None)>] Init of ParseResults<InvestigationParams>
        | [<CliPrefix(CliPrefix.None)>] Remove of ParseResults<EmptyArgs>
        | [<CliPrefix(CliPrefix.None)>] Edit of ParseResults<EmptyArgs>

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Update            _ -> "The investigation gets updated with the given parameters"
                | Init               _ -> "Create a new investigation file with the given parameters"
                | Remove            _ -> "Removes the investigation from the arc"
                | Edit              _ -> "Open an editor window to directly edit the isa investigation file"

    /// ------------ STUDY ARGUMENTS ------------ ///

    type StudyParams =
        | [<Mandatory>][<Unique>] Identifier of string
        | [<Unique>] Title of string
        | [<Unique>] Description of string
        | [<Unique>] SubmissionDate of string
        | [<Unique>] PublicReleaseDate of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Identifier _->    "Identifier of the study"
                | Title _->         "Title of the study"
                | Description _->   "Description of the study"
                | SubmissionDate _->   "Submission Date of the study"
                | PublicReleaseDate _->   "Public Release Date of the study"

    type StudyId =
        | [<Mandatory>][<Unique>] Identifier of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Identifier _->    "Identifier of the study, will be used as the file name of the study file"

    type Study =

        | [<CliPrefix(CliPrefix.None)>] Update of ParseResults<StudyParams>
        | [<CliPrefix(CliPrefix.None)>] Register of ParseResults<StudyParams>
        | [<CliPrefix(CliPrefix.None)>] Remove of ParseResults<StudyId>
        | [<CliPrefix(CliPrefix.None)>] Edit of ParseResults<StudyId>
        | [<CliPrefix(CliPrefix.None)>] List of ParseResults<EmptyArgs>

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Update    _ -> "The study gets updated with the given parameters"
                | Register  _ -> "Create a new Study"
                | Remove    _ -> "Removes the Study from the arc"
                | Edit      _ -> "Open an editor window to directly edit the isa Study file"
                | List      _ -> "Lists all studies in the investigation file"

    /// ------------ ASSAY ARGUMENTS ------------ ///

    type AssayId =
        | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of string
        | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | StudyIdentifier   _ -> "Name of the study in which the assay is situated"
                | AssayIdentifier   _ -> "Name of the assay of interest"

    type AssayMoveArguments =
        | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of string
        | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of string
        | [<Mandatory>][<AltCommandLine("-t")>][<Unique>] TargetStudyIdentifier of string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | StudyIdentifier   _ -> "Name of the study in which the assay is situated"
                | AssayIdentifier   _ -> "Name of the assay of interest"
                | TargetStudyIdentifier _-> "Target study"

    type AssayParams =  
        | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of string
        | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of string
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
                | StudyIdentifier   _                   -> "Name of the study in which the assay is situated"
                | AssayIdentifier   _                   -> "Name of the assay of interest"
                | MeasurementType _                     -> "Measurement type of the assay"
                | MeasurementTypeTermAccessionNumber _  -> "Measurement type Term Accession Number of the assay"
                | MeasurementTypeTermSourceREF _        -> "Measurement type Term Source REF of the assay"
                | TechnologyType _                      -> "Technology Type of the assay"
                | TechnologyTypeTermAccessionNumber _   -> "Technology Type Term Accession Number of the assay"
                | TechnologyTypeTermSourceREF _         -> "Technology Type Term Source REF of the assay"
                | TechnologyPlatform _                  -> "Technology Platform of the assay"

    type Assay = 
        | [<CliPrefix(CliPrefix.None)>] Create of ParseResults<AssayId>
        | [<CliPrefix(CliPrefix.None)>] Register of ParseResults<AssayParams>
        | [<CliPrefix(CliPrefix.None)>] Update of ParseResults<AssayParams>
        | [<CliPrefix(CliPrefix.None)>] Add of ParseResults<AssayParams>
        | [<CliPrefix(CliPrefix.None)>] Move of ParseResults<AssayMoveArguments>
        | [<CliPrefix(CliPrefix.None)>] Remove of ParseResults<AssayId>
        | [<CliPrefix(CliPrefix.None)>] Edit of ParseResults<AssayId>
        | [<CliPrefix(CliPrefix.None)>] List of ParseResults<EmptyArgs>

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
        | [<Unique>] Silent
        | [<CliPrefix(CliPrefix.None)>][<SubCommand>] Init of ParseResults<ArcParams>
        | [<AltCommandLine("i")>][<CliPrefix(CliPrefix.None)>] Investigation of ParseResults<Investigation>
        | [<AltCommandLine("s")>][<CliPrefix(CliPrefix.None)>] Study of ParseResults<Study>
        //| [<CliPrefix(CliPrefix.None)>] AddWorkflow of ParseResults<WorkflowArgs>
        | [<AltCommandLine("a")>][<CliPrefix(CliPrefix.None)>] Assay of ParseResults<Assay>

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | WorkingDir _ -> "Set the base directory of your ARC"
                | Silent   _ -> "Prevents the tool from printing additional information"
                | Init _ -> "Initializes basic folder structure"
                | Investigation _ -> "Investigation file functions"
                | Study         _ -> "Study functions"
                //| AddWorkflow _ -> "Not yet implemented"
                | Assay _ ->  "Assay functions"
