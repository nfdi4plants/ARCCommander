namespace ISA

open System
open System.Reflection
open System.Collections.Generic

module DataModel =

    module InvestigationFile = 

        type IsIdentifierAttribute() =    
            inherit Attribute()

        type LabelAttribute(l:string) =    
            inherit Attribute()
            member this.Label = l

        type ISAItem =
            abstract member Header : string
            abstract member KeyPrefix : string

        type TermSource (?name,?file,?version,?description) =
            [<Label("Name")>][<IsIdentifier>]
            member val Name = defaultArg name "" with get,set
            [<Label("File")>]
            member val File = defaultArg file "" with get,set
            [<Label("Version")>]
            member val Version = defaultArg version "" with get,set
            [<Label("Description")>]
            member val Description = defaultArg description "" with get,set
            interface ISAItem with
                member this.Header = "ONTOLOGY SOURCE REFERENCE"
                member this.KeyPrefix = "Term Source"

        type Publication (?pubMedID,?doi,?authorList,?title,?status,?statusTermAccessionNumber,?statusTermSourceREF) =
            [<Label("PubMed ID")>]
            member val PubMedID = defaultArg pubMedID "" with get,set
            [<Label("DOI")>][<IsIdentifier>]
            member val DOI = defaultArg doi "" with get,set
            [<Label("Author List")>]
            member val AuthorList = defaultArg authorList "" with get,set
            [<Label("Title")>]
            member val Title = defaultArg title "" with get,set
            [<Label("Status")>]
            member val Status = defaultArg status "" with get,set
            [<Label("Status Term Accession Number")>]
            member val StatusTermAccessionNumber = defaultArg statusTermAccessionNumber "" with get,set
            [<Label("Status Term Source REF")>]
            member val StatusTermSourceREF = defaultArg statusTermSourceREF "" with get,set
            interface ISAItem with
                member this.Header = "PUBLICATIONS"
                member this.KeyPrefix = "Publication"

        type Person (?lastName,?firstName,?midInitials,?email,?phone,?fax,?address,?affiliation,?roles,?rolesTermAccessionNumber,?rolesTermSourceREF) =
            [<Label("Last Name")>][<IsIdentifier>]
            member val LastName = defaultArg lastName "" with get,set
            [<Label("First Name")>][<IsIdentifier>]
            member val FirstName = defaultArg firstName "" with get,set
            [<Label("Mid Initials")>]
            member val MidInitials = defaultArg midInitials "" with get,set
            [<Label("Email")>]
            member val Email = defaultArg email "" with get,set
            [<Label("Phone")>]
            member val Phone = defaultArg phone "" with get,set
            [<Label("Fax")>]
            member val Fax = defaultArg fax "" with get,set
            [<Label("Address")>]
            member val Address = defaultArg address "" with get,set
            [<Label("Affiliation")>]
            member val Affiliation = defaultArg affiliation "" with get,set
            [<Label("Roles")>]
            member val Roles = defaultArg roles "" with get,set
            [<Label("Roles Term Accession Number")>]
            member val RolesTermAccessionNumber = defaultArg rolesTermAccessionNumber "" with get,set
            [<Label("Roles Term Source REF")>]
            member val RolesTermSourceREF = defaultArg rolesTermSourceREF "" with get,set
            interface ISAItem with
                member this.Header = "CONTACTS"
                member this.KeyPrefix = "Person"



        type Design (?designType,?typeTermAccessionNumber,?typeTermSourceREF) =
            [<Label("Type")>][<IsIdentifier>]
            member val DesignType = defaultArg designType "" with get,set
            [<Label("Type Term Accession Number")>]
            member val TypeTermAccessionNumber = defaultArg typeTermAccessionNumber "" with get,set
            [<Label("Type Term Source REF")>]
            member val TypeTermSourceREF = defaultArg typeTermSourceREF "" with get,set
            interface ISAItem with
                member this.Header = "DESIGN DESCRIPTORS"
                member this.KeyPrefix = "Design"



        type Factor (?name,?factorType,?typeTermAccessionNumber,?typeTermSourceREF) =
            [<Label("Name")>][<IsIdentifier>]
            member val Name = defaultArg name "" with get,set
            [<Label("Type")>]
            member val FactorType = defaultArg factorType "" with get,set
            [<Label("Type Term Accession Number")>]
            member val TypeTermAccessionNumber = defaultArg typeTermAccessionNumber "" with get,set
            [<Label("Type Term Source REF")>]
            member val TypeTermSourceREF = defaultArg typeTermSourceREF "" with get,set
            interface ISAItem with
                member this.Header = "FACTORS"
                member this.KeyPrefix = "Factor"



        type Assay (?measurementType,?measurementTypeTermAccessionNumber,?measurementTypeTermSourceREF,?technologyType,?technologyTypeTermAccessionNumber,?technologyTypeTermSourceREF,?technologyPlatform,?fileName) =
            [<Label("Measurement Type")>]
            member val MeasurementType = defaultArg measurementType "" with get,set
            [<Label("Measurement Type Term Accession Number")>]
            member val MeasurementTypeTermAccessionNumber = defaultArg measurementTypeTermAccessionNumber "" with get,set
            [<Label("Measurement Type Term Source REF")>]
            member val MeasurementTypeTermSourceREF = defaultArg measurementTypeTermSourceREF "" with get,set
            [<Label("Technology Type")>]
            member val TechnologyType = defaultArg technologyType "" with get,set
            [<Label("Technology Type Term Accession Number")>]
            member val TechnologyTypeTermAccessionNumber = defaultArg technologyTypeTermAccessionNumber "" with get,set
            [<Label("Technology Type Term Source REF")>]
            member val TechnologyTypeTermSourceREF = defaultArg technologyTypeTermSourceREF "" with get,set
            [<Label("Technology Platform")>]
            member val TechnologyPlatform = defaultArg technologyPlatform "" with get,set
            [<Label("File Name")>][<IsIdentifier>]
            member val FileName = defaultArg fileName "" with get,set
            interface ISAItem with
                member this.Header = "ASSAYS"
                member this.KeyPrefix = "Assay"



        type Protocol (?name,?protocolType,?typeTermAccessionNumber,?typeTermSourceREF,?description,?uri,?version,?parametersName,?parametersTermAccessionNumber,?parametersTermSourceREF,?componentsName,?componentsType,?componentsTypeTermAccessionNumber,?componentsTypeTermSourceREF) =
            [<Label("Name")>][<IsIdentifier>]
            member val Name = defaultArg name "" with get,set
            [<Label("Type")>]
            member val ProtocolType = defaultArg protocolType "" with get,set
            [<Label("Type Term Accession Number")>]
            member val TypeTermAccessionNumber = defaultArg typeTermAccessionNumber "" with get,set
            [<Label("Type Term Source REF")>]
            member val TypeTermSourceREF = defaultArg typeTermSourceREF "" with get,set
            [<Label("Description")>]
            member val Description = defaultArg description "" with get,set
            [<Label("URI")>]
            member val URI = defaultArg uri "" with get,set
            [<Label("Version")>]
            member val Version = defaultArg version "" with get,set
            [<Label("Parameters Name")>]
            member val ParametersName = defaultArg parametersName "" with get,set
            [<Label("Parameters Term Accession Number")>]
            member val ParametersTermAccessionNumber = defaultArg parametersTermAccessionNumber "" with get,set
            [<Label("Parameters Term Source REF")>]
            member val ParametersTermSourceREF = defaultArg parametersTermSourceREF "" with get,set
            [<Label("Components Name")>]
            member val ComponentsName = defaultArg componentsName "" with get,set
            [<Label("Components Type")>]
            member val ComponentsType = defaultArg componentsType "" with get,set
            [<Label("Components Type Term Accession Number")>]
            member val ComponentsTypeTermAccessionNumber = defaultArg componentsTypeTermAccessionNumber "" with get,set
            [<Label("Components Type Term Source REF")>]
            member val ComponentsTypeTermSourceREF = defaultArg componentsTypeTermSourceREF "" with get,set
            interface ISAItem with
                member this.Header = "PROTOCOLS"
                member this.KeyPrefix = "Protocol"



        type InvestigationItem (?identifier,?title,?description,?submissionDate,?publicReleaseDate) =
            [<Label("Identifier")>][<IsIdentifier>]
            member val Identifier = defaultArg identifier "" with get,set
            [<Label("Title")>]
            member val Title = defaultArg title "" with get,set
            [<Label("Description")>]
            member val Description = defaultArg description "" with get,set
            [<Label("Submission Date")>]
            member val SubmissionDate = defaultArg submissionDate "" with get,set
            [<Label("Public Release Date")>]
            member val PublicReleaseDate = defaultArg publicReleaseDate "" with get,set
            interface ISAItem with
                member this.Header = "INVESTIGATION"
                member this.KeyPrefix = "Investigation"



        type StudyItem (?identifier,?title,?description,?submissionDate,?publicReleaseDate,?fileName) =
            [<Label("Identifier")>][<IsIdentifier>]
            member val Identifier = defaultArg identifier "" with get,set
            [<Label("Title")>]
            member val Title = defaultArg title "" with get,set
            [<Label("Description")>]
            member val Description = defaultArg description "" with get,set
            [<Label("Submission Date")>]
            member val SubmissionDate = defaultArg submissionDate "" with get,set
            [<Label("Public Release Date")>]
            member val PublicReleaseDate = defaultArg publicReleaseDate "" with get,set
            [<Label("File Name")>]
            member val FileName = defaultArg fileName "" with get,set
            interface ISAItem with
                member this.Header = "STUDY"
                member this.KeyPrefix = "Study"
    
        let private tryGetCustomAttribute<'a> (findAncestor:bool) (propInfo :PropertyInfo) =   
            let attributeType = typeof<'a>
            let attrib = propInfo.GetCustomAttribute(attributeType, findAncestor)
            match box attrib with
            | (:? 'a) as customAttribute -> Some(unbox<'a> customAttribute)
            | _ -> None

        let getKeyValues (item:'T) =
            let schemaType = typeof<'T>
            schemaType.GetProperties()
            |> Array.choose ( fun memb -> 
                match tryGetCustomAttribute<LabelAttribute> true memb with 
                | Some x -> Some (x.Label,memb.GetValue(item) |> string)
                | None   -> None
                )

        let getIdentificationKeyValues (item:'T) =
            let schemaType = typeof<'T>
            schemaType.GetProperties()
            |> Array.choose ( fun memb -> 
                match tryGetCustomAttribute<IsIdentifierAttribute> true memb,tryGetCustomAttribute<LabelAttribute> true memb with 
                | Some _ , Some x -> Some (x.Label,memb.GetValue(item))
                | _   -> None
                )

        let setKeyValue (kv:KeyValuePair<string,string>) (item:'T) =
            let schemaType = typeof<'T>
            schemaType.GetProperties()
            |> Array.iter ( fun memb -> 
                match tryGetCustomAttribute<LabelAttribute> true memb with 
                | Some x when x.Label = kv.Key -> memb.SetValue(item,kv.Value)
                | _  -> ()
                )
            item