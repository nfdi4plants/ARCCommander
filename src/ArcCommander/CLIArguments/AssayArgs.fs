namespace ArcCommander.CLIArguments
open Argu 

/// CLI arguments for empty assay initialization
type AssayInitArgs =
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of assay_identifier:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | AssayIdentifier   _ -> "Identifier of the assay, will be used as name of the root folder of the new assay folder structure"

/// CLI arguments for deleting assay file structure
type AssayDeleteArgs = AssayInitArgs

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
            | MeasurementType _                     -> "A term to qualify the endpoint, or what is being measured (e.g. gene expression profiling or protein identification). The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used the Term Accession Number and Term Source REF fields below are required."
            | MeasurementTypeTermAccessionNumber _  -> "The accession number from the Term Source associated with the selected term."
            | MeasurementTypeTermSourceREF _        -> "The Source REF has to match one of the Term Source Name declared in the Ontology Source Reference section."
            | TechnologyType _                      -> "Term to identify the technology used to perform the measurement, e.g. DNA microarray, mass spectrometry. The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used the Term Accession Number and Term Source REF fields below are required."
            | TechnologyTypeTermAccessionNumber _   -> "The accession number from the Term Source associated with the selected term."
            | TechnologyTypeTermSourceREF _         -> "Identifies the controlled vocabulary or ontology that this term comes from. The Source REF has to match one of the Term Source Names declared in the Ontology Source Reference section."
            | TechnologyPlatform _                  -> "Manufacturer and platform name, e.g. Bruker AVANCE"

/// CLI arguments for interactively editing existing assay metadata 
type AssayEditArgs = 
    | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of study_identifier:string
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of assay_identifier:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier   _ -> "Identifier of the study in which the assay is registered"
            | AssayIdentifier   _ -> "Identifier of the assay of interest"

/// CLI arguments for registering existing assay metadata 
// Same arguments as `update` because all metadata fields that can be updated can also be set while registering
type AssayRegisterArgs = AssayUpdateArgs

/// CLI arguments for unregistering existing assay metadata from investigation file 
type AssayUnregisterArgs = AssayEditArgs

/// CLI arguments for initializing and subsequently registering assay metadata 
// Same arguments as `update` because all metadata fields that can be updated can also be set while registering a new assay
type AssayAddArgs = AssayUpdateArgs

/// CLI arguments for assay removal
type AssayRemoveArgs = AssayEditArgs

/// CLI arguments for assay move between studies
type AssayMoveArgs =
    | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of study_identifier:string
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of assay_identifier:string
    | [<Mandatory>][<AltCommandLine("-t")>][<Unique>] TargetStudyIdentifier of target_study_identifier:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier   _ -> "Name of the study in which the assay is situated"
            | AssayIdentifier   _ -> "Name of the assay of interest"
            | TargetStudyIdentifier _-> "Target study to which the assay should be moved"

/// CLI arguments for getting the values of a specific assay
type AssayGetArgs = AssayEditArgs
