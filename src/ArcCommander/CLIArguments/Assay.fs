namespace ArcCommander.CLIArguments
open Argu 
open ISA

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
    | [<CliPrefix(CliPrefix.None)>] List

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


