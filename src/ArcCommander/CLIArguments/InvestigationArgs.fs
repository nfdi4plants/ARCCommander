namespace ArcCommander.CLIArguments

open Argu 
open ArcCommander.ArgumentProcessing

///// CLI arguments for creating a new investigation file for the arc
//// in the case of investigations 'empty' does not mean empty file but rather an 
//// investigation without studies/assays. To reflect the need for metadata here,
//// this command is called `create` instead of `init`
//type InvestigationCreateArgs = 
//    | [<Mandatory>][<AltCommandLine("-i")>][<Unique>][<FileName>] Identifier of investigation_identifier:string
//    | [<Unique>] Title of title:string
//    | [<Unique>] Description of description:string
//    | [<Unique>] SubmissionDate of submission_date:string
//    | [<Unique>] PublicReleaseDate of public_release_date:string

//    interface IArgParserTemplate with
//        member this.Usage =
//            match this with
//            | Identifier        _-> "An identifier or an accession number provided by a repository. This SHOULD be locally unique. Hint: If you're unsure about this, you can use the name of the ARC or any name that roughly hints to what your experiment is about."
//            | Title             _-> "A concise name given to the investigation"
//            | Description       _-> "A textual description of the investigation"
//            | SubmissionDate    _-> "The date on which the investigation was reported to the repository"
//            | PublicReleaseDate _-> "The date on which the investigation was released publicly"

/// CLI arguments updating the arc's existing investigation file
type InvestigationUpdateArgs = 
    | [<Mandatory>][<AltCommandLine("-i")>][<Unique>][<FileName>] Identifier of investigation_identifier:string
    | [<Unique>] Title of title:string
    | [<Unique>] Description of description:string
    | [<Unique>] SubmissionDate of submission_date:string
    | [<Unique>] PublicReleaseDate of public_release_date:string
    | [<Unique>] ReplaceWithEmptyValues

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Identifier                _ -> "A identifier or an accession number provided by a repository. This SHOULD be locally unique."
            | Title                     _ -> "A concise name given to the investigation"
            | Description               _ -> "A textual description of the investigation"
            | SubmissionDate            _ -> "The date on which the investigation was reported to the repository"
            | PublicReleaseDate         _ -> "The date on which the investigation was released publicly"
            | ReplaceWithEmptyValues    _ -> "This flag can be used to delete fields from the investigation. If this flag is not set, only these fields for which a value was given will be updated."


///// CLI arguments for deleting the arc's investigation file (danger zone!)
//type InvestigationDeleteArgs =
//    | [<Mandatory>][<AltCommandLine("-i")>][<Unique>] Identifier of investigation_identifier:string

//    interface IArgParserTemplate with
//        member this.Usage =
//            match this with
//            | Identifier _-> "DANGER ZONE: In order to delete this investigation file, please provide its identifier here"

/// CLI arguments for Investigation Contacts
module InvestigationContacts = 

    /// CLI arguments for updating existing person metadata
    type PersonUpdateArgs =  
        | [<Mandatory>][<AltCommandLine("-l")>][<Unique>] LastName of last_name:string
        | [<Mandatory>][<AltCommandLine("-f")>][<Unique>] FirstName of first_name:string
        | [<AltCommandLine("-m")>][<Unique>] MidInitials of mid_initials:string
        | [<Unique>] Email of e_mail:string
        | [<Unique>] Phone of phone_number:string
        | [<Unique>] Fax of fax_number:string
        | [<Unique>] Address of adress:string
        | [<Unique>] Affiliation of affiliation:string
        | [<Unique>] ORCID of orcid:string
        | [<Unique>] Roles of roles:string
        | [<Unique>] RolesTermAccessionNumber of roles_term_accession_number:string
        | [<Unique>] RolesTermSourceREF of roles_term_source_ref:string
        | [<Unique>] ReplaceWithEmptyValues
        | [<Unique>] AddIfMissing


        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | LastName                  _ -> "The last name of a person associated with the investigation"
                | FirstName                 _ -> "The first name of a person associated with the investigation"
                | MidInitials               _ -> "The middle initials of a person associated with the investigation"
                | Email                     _ -> "The email address of a person associated with the investigation"
                | Phone                     _ -> "The telephone number of a person associated with the investigation"
                | Fax                       _ -> "The fax number of a person associated with the investigation"
                | Address                   _ -> "The address of a person associated with the investigation"
                | Affiliation               _ -> "The organization affiliation for a person associated with the investigation"
                | ORCID                     _ -> "The ORCID ID of the person"
                | Roles                     _ -> "Term to classify the role(s) performed by this person in the context of the investigation, which means that the roles reported here do not need to correspond to roles held withing their affiliated organization. Multiple annotations or values attached to one person can be provided by using a semicolon (“;”) Unicode (U0003+B) as a separator (e.g.: submitter;funder;sponsor). The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used the Term Accession Number (TAN) and Term Source REF fields below are required."
                | RolesTermAccessionNumber  _ -> "The accession number from the Term Source associated with the selected term"
                | RolesTermSourceREF        _ -> "Identifies the controlled vocabulary or ontology that this term comes from. The Source REF has to match one of the Term Source Names declared in the Ontology Source Reference section."
                | ReplaceWithEmptyValues    _ -> "This flag can be used to delete fields from the person. If this flag is not set, only these fields for which a value was given will be updated."
                | AddIfMissing              _ -> "If this flag is set, a new person will be registered with the given parameters, if it did not previously exist"

    /// CLI arguments for interactively editing existing person metadata 
    type PersonEditArgs = 
        | [<Mandatory>][<AltCommandLine("-l")>][<Unique>] LastName of last_name:string
        | [<Mandatory>][<AltCommandLine("-f")>][<Unique>] FirstName of first_name:string
        | [<AltCommandLine("-m")>][<Unique>] MidInitials of mid_initials:string
    
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | LastName                  _ -> "The last name of a person associated with the investigation"
                | FirstName                 _ -> "The first name of a person associated with the investigation"
                | MidInitials               _ -> "The middle initials of a person associated with the investigation"

    /// CLI arguments for registering person metadata 
    type PersonRegisterArgs = 
        | [<Mandatory>][<AltCommandLine("-l")>][<Unique>] LastName of last_name:string
        | [<Mandatory>][<AltCommandLine("-f")>][<Unique>] FirstName of first_name:string
        | [<AltCommandLine("-m")>][<Unique>] MidInitials of mid_initials:string
        | [<Unique>] Email of e_mail:string
        | [<Unique>] Phone of phone_number:string
        | [<Unique>] Fax of fax_number:string
        | [<Unique>] Address of adress:string
        | [<Unique>] Affiliation of affiliation:string
        | [<Unique>] ORCID of orcid:string
        | [<Unique>] Roles of roles:string
        | [<Unique>] RolesTermAccessionNumber of roles_term_accession_number:string
        | [<Unique>] RolesTermSourceREF of roles_term_source_ref:string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | LastName                  _ -> "The last name of a person associated with the investigation"
                | FirstName                 _ -> "The first name of a person associated with the investigation"
                | MidInitials               _ -> "The middle initials of a person associated with the investigation"
                | Email                     _ -> "The email address of a person associated with the investigation"
                | Phone                     _ -> "The telephone number of a person associated with the investigation"
                | Fax                       _ -> "The fax number of a person associated with the investigation"
                | Address                   _ -> "The address of a person associated with the investigation"
                | Affiliation               _ -> "The organization affiliation for a person associated with the investigation"
                | ORCID                     _ -> "The ORCID ID of the person"
                | Roles                     _ -> "Term to classify the role(s) performed by this person in the context of the investigation, which means that the roles reported here do not need to correspond to roles held withing their affiliated organization. Multiple annotations or values attached to one person can be provided by using a semicolon (“;”) Unicode (U0003+B) as a separator (e.g.: submitter;funder;sponsor). The term can be free text or from, for example, a controlled vocabulary or an ontology. If the latter source is used the Term Accession Number (TAN) and Term Source REF fields below are required."
                | RolesTermAccessionNumber  _ -> "The accession number from the Term Source associated with the selected term"
                | RolesTermSourceREF        _ -> "Identifies the controlled vocabulary or ontology that this term comes from. The Source REF has to match one of the Term Source Names declared in the Ontology Source Reference section."


    /// CLI arguments for person removal
    // Same arguments as `edit` because all metadata fields needed for identifying the person also have to be used when editing
    type PersonUnregisterArgs = PersonEditArgs

    /// CLI arguments for getting person
    // Same arguments as `edit` because all metadata fields needed for identifying the person also have to be used when editing
    type PersonShowArgs = PersonEditArgs

/// CLI arguments for Investigation Contacts
module InvestigationPublications = 

    /// CLI arguments for updating existing publication metadata
    type PublicationUpdateArgs =  
        | [<Mandatory>][<AltCommandLine("-d")>][<Unique>] DOI of doi:string
        | [<AltCommandLine("-p")>][<Unique>] PubMedID of pubmed_id:string
        | [<Unique>] AuthorList of author_list:string
        | [<Unique>] Title of publication_title:string
        | [<Unique>] Status of publication_status:string
        | [<Unique>] StatusTermAccessionNumber of publication_status_term_accession_number:string
        | [<Unique>] StatusTermSourceREF of publication_status_term_source_ref:string
        | [<Unique>] ReplaceWithEmptyValues
        | [<Unique>] AddIfMissing

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | DOI                       _ -> "A Digital Object Identifier (DOI) for that publication (where available)"
                | PubMedID                  _ -> "The PubMed IDs of the described publication(s) associated with this investigation"
                | AuthorList                _ -> "The list of authors associated with that publication"
                | Title                     _ -> "The title of publication associated with the investigation"
                | Status                    _ -> "A term describing the status of that publication (i.e. submitted, in preparation, published)"
                | StatusTermAccessionNumber _ -> "The accession number from the Term Source associated with the selected term"
                | StatusTermSourceREF       _ -> "Identifies the controlled vocabulary or ontology that this term comes from. The Source REF has to match one the Term Source Name declared in the in the Ontology Source Reference section."
                | ReplaceWithEmptyValues    _ -> "This flag can be used to delete fields from the publication. If this flag is not set, only these fields for which a value was given will be updated."
                | AddIfMissing              _ -> "If this flag is set, a new publication will be registered with the given parameters, if it did not previously exist"


    /// CLI arguments for interactively editing existing publication metadata 
    type PublicationEditArgs = 
        | [<Mandatory>][<AltCommandLine("-d")>][<Unique>] DOI of doi:string
    
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | DOI _ -> "A Digital Object Identifier (DOI) for that publication (where available)"

    /// CLI arguments for registering publication metadata 
    type PublicationRegisterArgs = 
        | [<Mandatory>][<AltCommandLine("-d")>][<Unique>] DOI of doi:string
        | [<AltCommandLine("-p")>][<Unique>] PubMedID of pubmed_id:string
        | [<Unique>] AuthorList of author_list:string
        | [<Unique>] Title of publication_title:string
        | [<Unique>] Status of publication_status:string
        | [<Unique>] StatusTermAccessionNumber of publication_status_term_accession_number:string
        | [<Unique>] StatusTermSourceREF of publication_status_term_source_ref:string

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | DOI                       _ -> "A Digital Object Identifier (DOI) for that publication (where available)"
                | PubMedID                  _ -> "The PubMed IDs of the described publication(s) associated with this investigation"
                | AuthorList                _ -> "The list of authors associated with that publication"
                | Title                     _ -> "The title of publication associated with the investigation"
                | Status                    _ -> "A term describing the status of that publication (i.e. submitted, in preparation, published)"
                | StatusTermAccessionNumber _ -> "The accession number from the Term Source associated with the selected term"
                | StatusTermSourceREF       _ -> "Identifies the controlled vocabulary or ontology that this term comes from. The Source REF has to match one of the Term Source Names declared in the in the Ontology Source Reference section."


    /// CLI arguments for publication removal
    // Same arguments as `edit` because all metadata fields needed for identifying the publication also have to be used when editing
    type PublicationUnregisterArgs = PublicationEditArgs

    /// CLI arguments for getting publication
    // Same arguments as `edit` because all metadata fields needed for identifying the publication also have to be used when editing
    type PublicationShowArgs = PublicationEditArgs
