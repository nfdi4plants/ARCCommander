namespace ArcCommander.CLIArguments

open Argu 

/// CLI arguments for empty assay initialization.
type AssayInitArgs =
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>]   AssayIdentifier                     of string
    | [<Unique>]                                        MeasurementType                     of measurement_type             : string
    | [<Unique>]                                        MeasurementTypeTermAccessionNumber  of measurement_type_accession   : string
    | [<Unique>]                                        MeasurementTypeTermSourceREF        of measurement_type_term_source : string
    | [<Unique>]                                        TechnologyType                      of technology_type              : string
    | [<Unique>]                                        TechnologyTypeTermAccessionNumber   of technology_type_accession    : string
    | [<Unique>]                                        TechnologyTypeTermSourceREF         of technology_type_term_source  : string
    | [<Unique>]                                        TechnologyPlatform                  of technology_platform          : string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | AssayIdentifier                       _ -> "Identifier of the assay, will be used as name of the root folder of the new assay folder structure"
            | MeasurementType                       _ -> "A term to qualify the endpoint, or what is being measured (e.g. gene expression profiling or protein identification). The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used the Term Accession Number (TAN) and Term Source REF fields below are required."
            | MeasurementTypeTermAccessionNumber    _ -> "The accession number from the Term Source associated with the selected term"
            | MeasurementTypeTermSourceREF          _ -> "The Source REF has to match one of the Term Source Names declared in the Ontology Source Reference section."
            | TechnologyType                        _ -> "Term to identify the technology used to perform the measurement, e.g. DNA microarray, mass spectrometry. The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used the Term Accession Number (TAN) and Term Source REF fields below are required."
            | TechnologyTypeTermAccessionNumber     _ -> "The accession number from the Term Source associated with the selected term"
            | TechnologyTypeTermSourceREF           _ -> "Identifies the controlled vocabulary or ontology that this term comes from. The Source REF has to match one of the Term Source Names declared in the Ontology Source Reference section."
            | TechnologyPlatform                    _ -> "Manufacturer and platform name, e.g. Bruker AVANCE"

/// CLI arguments for deleting assay file structure.
type AssayDeleteArgs = 
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>]   AssayIdentifier                     of string
    | [<Unique>]                                        Force

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | AssayIdentifier _ -> "Identifier of the assay, will be used as name of the root folder of the new assay folder structure"
            | Force             -> "Forces deletion of all subfolders and -files, no matter if they are user-specific or not"

/// CLI arguments for updating existing assay metadata.
type AssayUpdateArgs =  
    | [<AltCommandLine("-s")>][<Unique>]                StudyIdentifier                     of string
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>]   AssayIdentifier                     of string
    | [<Unique>]                                        MeasurementType                     of measurement_type             : string
    | [<Unique>]                                        MeasurementTypeTermAccessionNumber  of measurement_type_accession   : string
    | [<Unique>]                                        MeasurementTypeTermSourceREF        of measurement_type_term_source : string
    | [<Unique>]                                        TechnologyType                      of technology_type              : string
    | [<Unique>]                                        TechnologyTypeTermAccessionNumber   of technology_type_accession    : string
    | [<Unique>]                                        TechnologyTypeTermSourceREF         of technology_type_term_source  : string
    | [<Unique>]                                        TechnologyPlatform                  of technology_platform          : string
    | [<Unique>]                                        ReplaceWithEmptyValues
    | [<Unique>]                                        AddIfMissing
    
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier                       _ -> "Name of the study in which the assay is situated"
            | AssayIdentifier                       _ -> "Name of the assay of interest"
            | MeasurementType                       _ -> "A term to qualify the endpoint, or what is being measured (e.g. gene expression profiling or protein identification). The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used, the Term Accession Number (TAN) and Term Source REF fields below are required."
            | MeasurementTypeTermAccessionNumber    _ -> "The accession number from the Term Source associated with the selected term"
            | MeasurementTypeTermSourceREF          _ -> "The Source REF has to match one of the Term Source Names declared in the Ontology Source Reference section"
            | TechnologyType                        _ -> "Term to identify the technology used to perform the measurement, e.g. DNA microarray, mass spectrometry. The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used the Term Accession Number (TAN) and Term Source REF fields below are required."
            | TechnologyTypeTermAccessionNumber     _ -> "The accession number from the Term Source associated with the selected term."
            | TechnologyTypeTermSourceREF           _ -> "Identifies the controlled vocabulary or ontology that this term comes from. The Source REF has to match one of the Term Source Names declared in the Ontology Source Reference section."
            | TechnologyPlatform                    _ -> "Manufacturer and platform name, e.g. Bruker AVANCE"
            | ReplaceWithEmptyValues                _ -> "This flag can be used to delete fields from the assay. If this flag is not set, only these fields for which a value was given will be updated."
            | AddIfMissing                          _ -> "If this flag is set, a new assay will be registered with the given parameters, if it did not previously exist"

/// CLI arguments for interactively editing existing assay metadata.
type AssayEditArgs = 
    | [<AltCommandLine("-s")>][<Unique>] StudyIdentifier of study_identifier : string
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier of assay_identifier : string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier   _ -> "Identifier of the study in which the assay is registered"
            | AssayIdentifier   _ -> "Identifier of the assay of interest"

/// CLI arguments for registering existing assay metadata.
type AssayRegisterArgs = 
    | [<AltCommandLine("-s")>][<Unique>]                StudyIdentifier                     of string
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>]   AssayIdentifier                     of string
    
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier                       _ -> "Name of the study in which the assay is situated"
            | AssayIdentifier                       _ -> "Name of the assay of interest"

/// CLI arguments for unregistering existing assay metadata from investigation file.
type AssayUnregisterArgs = AssayEditArgs

/// CLI arguments for initializing and subsequently registering assay metadata.
// Same arguments as `register` because all metadata fields that can be updated can also be set while registering a new assay
type AssayAddArgs =
    | [<AltCommandLine("-s")>][<Unique>]                StudyIdentifier                     of string
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>]   AssayIdentifier                     of string
    | [<Unique>]                                        MeasurementType                     of measurement_type             : string
    | [<Unique>]                                        MeasurementTypeTermAccessionNumber  of measurement_type_accession   : string
    | [<Unique>]                                        MeasurementTypeTermSourceREF        of measurement_type_term_source : string
    | [<Unique>]                                        TechnologyType                      of technology_type              : string
    | [<Unique>]                                        TechnologyTypeTermAccessionNumber   of technology_type_accession    : string
    | [<Unique>]                                        TechnologyTypeTermSourceREF         of technology_type_term_source  : string
    | [<Unique>]                                        TechnologyPlatform                  of technology_platform          : string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier                       _ -> "Name of the study in which the assay is situated"
            | AssayIdentifier                       _ -> "Name of the assay of interest"
            | MeasurementType                       _ -> "A term to qualify the endpoint, or what is being measured (e.g. gene expression profiling or protein identification). The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used, the Term Accession Number (TAN) and Term Source REF fields below are required."
            | MeasurementTypeTermAccessionNumber    _ -> "The accession number from the Term Source associated with the selected term"
            | MeasurementTypeTermSourceREF          _ -> "The Source REF has to match one of the Term Source Names declared in the Ontology Source Reference section"
            | TechnologyType                        _ -> "Term to identify the technology used to perform the measurement, e.g. DNA microarray, mass spectrometry. The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used the Term Accession Number (TAN) and Term Source REF fields below are required."
            | TechnologyTypeTermAccessionNumber     _ -> "The accession number from the Term Source associated with the selected term."
            | TechnologyTypeTermSourceREF           _ -> "Identifies the controlled vocabulary or ontology that this term comes from. The Source REF has to match one of the Term Source Names declared in the Ontology Source Reference section."
            | TechnologyPlatform                    _ -> "Manufacturer and platform name, e.g. Bruker AVANCE"

/// CLI arguments for assay removal.
type AssayRemoveArgs = AssayDeleteArgs

/// CLI arguments for assay move between studies.
type AssayMoveArgs =
    | [<Mandatory>][<AltCommandLine("-s")>][<Unique>] StudyIdentifier       of study_identifier         : string
    | [<Mandatory>][<AltCommandLine("-a")>][<Unique>] AssayIdentifier       of assay_identifier         : string
    | [<Mandatory>][<AltCommandLine("-t")>][<Unique>] TargetStudyIdentifier of target_study_identifier  : string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier       _ -> "Name of the study in which the assay is situated"
            | AssayIdentifier       _ -> "Name of the assay of interest"
            | TargetStudyIdentifier _ -> "Target study to which the assay should be moved"

/// CLI arguments for getting the values of a specific assay.
type AssayShowArgs = AssayEditArgs

/// CLI arguments for exporting a specific assay to json.
type AssayExportArgs = 
    | [<AltCommandLine("-s")>][<Unique>]    StudyIdentifier of study_identifier : string
    | [<AltCommandLine("-a")>][<Unique>]    AssayIdentifier of assay_identifier : string
    | [<AltCommandLine("-o")>][<Unique>]    Output          of output           : string
    | [<AltCommandLine("-ps")>][<Unique>]   ProcessSequence

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | StudyIdentifier   _ -> "Identifier of the study in which the assay is registered"
            | AssayIdentifier   _ -> "Identifier of the assay of interest"
            | Output            _ -> "Path to which the json should be exported. Only written to the cli output if no path given"
            | ProcessSequence   _ -> "If this flag is set, the return value of this assay will be its list of processes"

/// CLI arguments for assay contacts.
module AssayContacts = 

    /// CLI arguments for updating existing person metadata.
    type PersonUpdateArgs =  
        | [<Mandatory>][<AltCommandLine("-s")>][<Unique>]   AssayIdentifier             of string
        | [<Mandatory>][<AltCommandLine("-l")>][<Unique>]   LastName                    of last_name                    : string
        | [<Mandatory>][<AltCommandLine("-f")>][<Unique>]   FirstName                   of first_name                   : string
        | [<AltCommandLine("-m")>][<Unique>]                MidInitials                 of mid_initials                 : string
        | [<Unique>]                                        Email                       of e_mail                       : string
        | [<Unique>]                                        Phone                       of phone_number                 : string
        | [<Unique>]                                        Fax                         of fax_number                   : string
        | [<Unique>]                                        Address                     of adress                       : string
        | [<Unique>]                                        Affiliation                 of affiliation                  : string
        | [<Unique>]                                        ORCID                       of orcid                        : string
        | [<Unique>]                                        Roles                       of roles                        : string
        | [<Unique>]                                        RolesTermAccessionNumber    of roles_term_accession_number  : string
        | [<Unique>]                                        RolesTermSourceREF          of roles_term_source_ref        : string
        | [<Unique>]                                        ReplaceWithEmptyValues
        | [<Unique>]                                        AddIfMissing

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | AssayIdentifier           _ -> "Identifier of the assay the person is associated with"
                | LastName                  _ -> "The last name of a person associated with the assay"
                | FirstName                 _ -> "The first name of a person associated with the assay"
                | MidInitials               _ -> "The middle initials of a person associated with the assay"
                | Email                     _ -> "The email address of a person associated with the assay"
                | Phone                     _ -> "The telephone number of a person associated with the assay"
                | Fax                       _ -> "The fax number of a person associated with the assay"
                | Address                   _ -> "The address of a person associated with the assay"
                | Affiliation               _ -> "The organization affiliation for a person associated with the assay"
                | ORCID                     _ -> "The ORCID ID of the person"
                | Roles                     _ -> "Term to classify the role(s) performed by this person in the context of the assay, which means that the roles reported here do not need to correspond to roles held withing their affiliated organization. Multiple annotations or values attached to one person can be provided by using a semicolon (“;”) Unicode (U0003+B) as a separator (e.g.: submitter;funder;sponsor). The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used, the Term Accession Number (TAN) and Term Source REF fields below are required."
                | RolesTermAccessionNumber  _ -> "The accession number from the Term Source associated with the selected term"
                | RolesTermSourceREF        _ -> "Identifies the controlled vocabulary or ontology that this term comes from. The Source REF has to match one of the Term Source Names declared in the Ontology Source Reference section."
                | ReplaceWithEmptyValues    _ -> "This flag can be used to delete fields from the assay. If this flag is not set, only these fields for which a value was given will be updated."
                | AddIfMissing              _ -> "If this flag is set, a new person will be registered with the given parameters, if it did not previously exist"

    /// CLI arguments for interactively editing existing person metadata.
    type PersonEditArgs = 
        | [<Mandatory>][<AltCommandLine("-s")>][<Unique>]   StudyIdentifier of string
        | [<Mandatory>][<AltCommandLine("-l")>][<Unique>]   LastName        of last_name    : string
        | [<Mandatory>][<AltCommandLine("-f")>][<Unique>]   FirstName       of first_name   : string
        | [<AltCommandLine("-m")>][<Unique>]                MidInitials     of mid_initials : string
        
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | StudyIdentifier           _ -> "Identifier of the assay the person is associated with"
                | LastName                  _ -> "The last name of a person associated with the assay"
                | FirstName                 _ -> "The first name of a person associated with the assay"
                | MidInitials               _ -> "The middle initials of a person associated with the assay"

    /// CLI arguments for registering person metadata.
    type PersonRegisterArgs = 
        | [<Mandatory>][<AltCommandLine("-s")>][<Unique>]   AssayIdentifier             of string
        | [<Mandatory>][<AltCommandLine("-l")>][<Unique>]   LastName                    of last_name                    : string
        | [<Mandatory>][<AltCommandLine("-f")>][<Unique>]   FirstName                   of first_name                   : string
        | [<AltCommandLine("-m")>][<Unique>]                MidInitials                 of mid_initials                 : string
        | [<Unique>]                                        Email                       of e_mail                       : string
        | [<Unique>]                                        Phone                       of phone_number                 : string
        | [<Unique>]                                        Fax                         of fax_number                   : string
        | [<Unique>]                                        Address                     of adress                       : string
        | [<Unique>]                                        Affiliation                 of affiliation                  : string
        | [<Unique>]                                        ORCID                       of orcid                        : string
        | [<Unique>]                                        Roles                       of roles                        : string
        | [<Unique>]                                        RolesTermAccessionNumber    of roles_term_accession_number  : string
        | [<Unique>]                                        RolesTermSourceREF          of roles_term_source_ref        : string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | AssayIdentifier           _ -> "Identifier of the assay the person is associated with"
                | LastName                  _ -> "The last name of a person associated with the assay"
                | FirstName                 _ -> "The first name of a person associated with the assay"
                | MidInitials               _ -> "The middle initials of a person associated with the assay"
                | Email                     _ -> "The email address of a person associated with the assay"
                | Phone                     _ -> "The telephone number of a person associated with the assay"
                | Fax                       _ -> "The fax number of a person associated with the assay"
                | Address                   _ -> "The address of a person associated with the assay"
                | Affiliation               _ -> "The organization affiliation for a person associated with the assay"
                | ORCID                     _ -> "The ORCID ID of the person"
                | Roles                     _ -> "Term to classify the role(s) performed by this person in the context of the assay, which means that the roles reported here do not need to correspond to roles held withing their affiliated organization. Multiple annotations or values attached to one person can be provided by using a semicolon (“;”) Unicode (U0003+B) as a separator (e.g.: submitter;funder;sponsor). The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used the Term Accession Number and Term Source REF fields below are required."
                | RolesTermAccessionNumber  _ -> "The accession number from the Term Source associated with the selected term"
                | RolesTermSourceREF        _ -> "Identifies the controlled vocabulary or ontology that this term comes from. The Source REF has to match one of the Term Source Names declared in the Ontology Source Reference section."


    /// CLI arguments for person removal.
    // Same arguments as `edit` because all metadata fields needed for identifying the person also have to be used when editing
    type PersonUnregisterArgs = PersonEditArgs

    /// CLI arguments for getting person.
    // Same arguments as `edit` because all metadata fields needed for identifying the person also have to be used when editing
    type PersonShowArgs = PersonEditArgs