namespace ArcCommander.CLIArguments
open Argu 
open ISA

type AssayCreateArgs =
    | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier of study_identifier:string
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of assay_identifier:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier   _ -> "identifier of the study in which the assay is registered"
            | AssayIdentifier   _ -> "identifier of the assay of interest"

type AssayEditArgs = AssayCreateArgs
type AssayRemoveArgs = AssayCreateArgs

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

type AssayRegisterArgs = AssayUpdateArgs
type AssayAddArgs = AssayUpdateArgs

type Assay = 
    | [<CliPrefix(CliPrefix.None)>] Create   of create_args:  ParseResults<AssayCreateArgs>
    | [<CliPrefix(CliPrefix.None)>] Update   of update_args:  ParseResults<AssayUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit     of edit_args:    ParseResults<AssayEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register of register_args:ParseResults<AssayRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Add      of add_args:     ParseResults<AssayAddArgs>
    | [<CliPrefix(CliPrefix.None)>] Move     of move_args:    ParseResults<AssayMoveArguments>
    | [<CliPrefix(CliPrefix.None)>] Remove   of remove_args:  ParseResults<AssayRemoveArgs>
    | [<CliPrefix(CliPrefix.None)>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Create            _ -> "Create a new assay with the given parameters"
            | Update            _ -> "Update an assaywith the given parameters"
            | Edit              _ -> "Open an editor window to directly edit an assay"
            | Register          _ -> "Register an existing assay in the given study"
            | Add               _ -> "Create a new assay and register it to the given study"
            | Move              _ -> "Move an assay from one study to another"
            | Remove            _ -> "Remove an assay from the arc"
            | List              _ -> "List all assays registered in the investigation file"


