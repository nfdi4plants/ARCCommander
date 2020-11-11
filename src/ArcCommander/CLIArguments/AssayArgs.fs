namespace ArcCommander.CLIArguments
open Argu 
open ISA

/// CLI arguments for empty assay initialization
type AssayInitArgs =
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of assay_identifier:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | AssayIdentifier   _ -> "identifier of the assay, will be used as name of the root folder of the new assay folder structure"

/// CLI arguments for updating existing assay metadata
type AssayUpdateArgs =  
    | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of string
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of string
    | [<Unique>] MeasurementType of measurement_type:string
    | [<Unique>] MeasurementTypeTermAccessionNumber of measurement_type_accession:string
    | [<Unique>] MeasurementTypeTermSourceREF of measurement_type_term_source:string
    | [<Unique>] TechnologyType of technology_type:string
    | [<Unique>] TechnologyTypeTermAccessionNumber of technology_type_accession:string
    | [<Unique>] TechnologyTypeTermSourceREF of technology_type_term_source:string
    | [<Unique>] TechnologyPlatform of technology_platform:string
    
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

/// CLI arguments for interactively editing existing assay metadata 
// Same arguments as `update` because edit is basically an interactive update, where 
// the arguments set in the command line will be already set when opening the editor
type AssayEditArgs = AssayUpdateArgs

/// CLI arguments for registering existing assay metadata 
// Same arguments as `update` because all metadata fields that can be updated can also be set while registering
type AssayRegisterArgs = AssayUpdateArgs

/// CLI arguments for initializing and subsequently registering assay metadata 
// Same arguments as `update` because all metadata fields that can be updated can also be set while registering a new assay
type AssayAddArgs = AssayUpdateArgs

/// CLI arguments for assay removal
type AssayRemoveArgs =
    | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of study_identifier:string
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of assay_identifier:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier   _ -> "identifier of the study in which the assay is registered"
            | AssayIdentifier   _ -> "identifier of the assay of interest"

/// CLI arguments for assay move between studies
type AssayMoveArguments =
    | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of study_identifier:string
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of assay_identifier:string
    | [<Mandatory>][<AltCommandLine("-t")>][<Unique>] TargetStudyIdentifier of target_study_identifier:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier   _ -> "Name of the study in which the assay is situated"
            | AssayIdentifier   _ -> "Name of the assay of interest"
            | TargetStudyIdentifier _-> "Target study"


