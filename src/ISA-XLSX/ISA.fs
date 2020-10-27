namespace ISA


module DataModel =

    module InvestigationFile = 

        type ISAItem =
            abstract member Header : string
            abstract member KeyPrefix : string
            abstract member KeyValues : unit -> (string*string) list
            abstract member KeyValuesOfInterest : unit -> (string*string) list

        type TermSource (?name,?file,?version,?description) =
            member val Name = defaultArg name "" with get,set
            member val File = defaultArg file "" with get,set
            member val Version = defaultArg version "" with get,set
            member val Description = defaultArg description "" with get,set
            interface ISAItem with
                member this.Header = "ONTOLOGY SOURCE REFERENCE"
                member this.KeyPrefix = "Term Source"
                member this.KeyValues () = 
                    [
                    "Name",this.Name
                    "File",this.File
                    "Version",this.Version
                    "Description",this.Description
                    ]
                member this.KeyValuesOfInterest () = ["Name",this.Name]


        type Publication (?pubMedID,?doi,?authorList,?title,?status,?statusTermAccessionNumber,?statusTermSourceREF) =
            member val PubMedID = defaultArg pubMedID "" with get,set
            member val DOI = defaultArg doi "" with get,set
            member val AuthorList = defaultArg authorList "" with get,set
            member val Title = defaultArg title "" with get,set
            member val Status = defaultArg status "" with get,set
            member val StatusTermAccessionNumber = defaultArg statusTermAccessionNumber "" with get,set
            member val StatusTermSourceREF = defaultArg statusTermSourceREF "" with get,set
            interface ISAItem with
                member this.Header = "PUBLICATIONS"
                member this.KeyPrefix = "Publication"
                member this.KeyValues () = 
                    [
                    "PubMed ID",this.PubMedID
                    "DOI",this.DOI
                    "Author List",this.AuthorList
                    "Title",this.Title
                    "Status",this.Status
                    "Status Term Accession Number",this.StatusTermAccessionNumber
                    "Status Term Source REF",this.StatusTermSourceREF
                    ]
                member this.KeyValuesOfInterest () = ["DOI",this.DOI]

        type Person (?lastName,?firstName,?midInitials,?email,?phone,?fax,?address,?affiliation,?roles,?rolesTermAccessionNumber,?rolesTermSourceREF) =
            member val LastName = defaultArg lastName "" with get,set
            member val FirstName = defaultArg firstName "" with get,set
            member val MidInitials = defaultArg midInitials "" with get,set
            member val Email = defaultArg email "" with get,set
            member val Phone = defaultArg phone "" with get,set
            member val Fax = defaultArg fax "" with get,set
            member val Address = defaultArg address "" with get,set
            member val Affiliation = defaultArg affiliation "" with get,set
            member val Roles = defaultArg roles "" with get,set
            member val RolesTermAccessionNumber = defaultArg rolesTermAccessionNumber "" with get,set
            member val RolesTermSourceREF = defaultArg rolesTermSourceREF "" with get,set
            interface ISAItem with
                member this.Header = "CONTACTS"
                member this.KeyPrefix = "Person"
                member this.KeyValues () = 
                    [
                    "Last Name",this.LastName
                    "First Name",this.FirstName
                    "Mid Initials",this.MidInitials
                    "Email",this.Email
                    "Phone",this.Phone
                    "Fax",this.Fax
                    "Address",this.Address
                    "Affiliation",this.Affiliation
                    "Roles",this.Roles
                    "Roles Term Accession Number",this.RolesTermAccessionNumber
                    "Roles Term Source REF",this.RolesTermSourceREF
                    ]
                member this.KeyValuesOfInterest () = ["First Name",this.FirstName; "Last Name",this.LastName]


        type Design (?designType,?typeTermAccessionNumber,?typeTermSourceREF) =
            member val DesignType = defaultArg designType "" with get,set
            member val TypeTermAccessionNumber = defaultArg typeTermAccessionNumber "" with get,set
            member val TypeTermSourceREF = defaultArg typeTermSourceREF "" with get,set
            interface ISAItem with
                member this.Header = "DESIGN DESCRIPTORS"
                member this.KeyPrefix = "Design"
                member this.KeyValues () = 
                    [
                    "Type",this.DesignType
                    "Type Term Accession Number",this.TypeTermAccessionNumber
                    "Type Term Source REF",this.TypeTermSourceREF
                    ]
                member this.KeyValuesOfInterest () = ["Type Term Accession Number",this.TypeTermAccessionNumber]

        type Factor (?name,?factorType,?typeTermAccessionNumber,?typeTermSourceREF) =
            member val Name = defaultArg name "" with get,set
            member val FactorType = defaultArg factorType "" with get,set
            member val TypeTermAccessionNumber = defaultArg typeTermAccessionNumber "" with get,set
            member val TypeTermSourceREF = defaultArg typeTermSourceREF "" with get,set
            interface ISAItem with
                member this.Header = "FACTORS"
                member this.KeyPrefix = "Factor"
                member this.KeyValues () = 
                    [
                    "Name",this.Name
                    "Type",this.FactorType
                    "Type Term Accession Number",this.TypeTermAccessionNumber
                    "Type Term Source REF",this.TypeTermSourceREF
                    ]
                member this.KeyValuesOfInterest () = ["Name",this.Name]

        type Assay (?measurementType,?measurementTypeTermAccessionNumber,?measurementTypeTermSourceREF,?technologyType,?technologyTypeTermAccessionNumber,?technologyTypeTermSourceREF,?technologyPlatform,?fileName) =
            member val MeasurementType = defaultArg measurementType "" with get,set
            member val MeasurementTypeTermAccessionNumber = defaultArg measurementTypeTermAccessionNumber "" with get,set
            member val MeasurementTypeTermSourceREF = defaultArg measurementTypeTermSourceREF "" with get,set
            member val TechnologyType = defaultArg technologyType "" with get,set
            member val TechnologyTypeTermAccessionNumber = defaultArg technologyTypeTermAccessionNumber "" with get,set
            member val TechnologyTypeTermSourceREF = defaultArg technologyTypeTermSourceREF "" with get,set
            member val TechnologyPlatform = defaultArg technologyPlatform "" with get,set
            member val FileName = defaultArg fileName "" with get,set
            interface ISAItem with
                member this.Header = "ASSAYS"
                member this.KeyPrefix = "Assay"
                member this.KeyValues () = 
                    [
                    "Measurement Type",this.MeasurementType
                    "Measurement Type Term Accession Number",this.MeasurementTypeTermAccessionNumber
                    "Measurement Type Term Source REF",this.MeasurementTypeTermSourceREF
                    "Technology Type",this.TechnologyType
                    "Technology Type Term Accession Number",this.TechnologyTypeTermAccessionNumber
                    "Technology Type Term Source REF",this.TechnologyTypeTermSourceREF
                    "Technology Platform",this.TechnologyPlatform
                    "File Name",this.FileName
                    ]
                member this.KeyValuesOfInterest () = ["File Name",this.FileName]

        type Protocol (?name,?protocolType,?typeTermAccessionNumber,?typeTermSourceREF,?description,?uri,?version,?parametersName,?parametersTermAccessionNumber,?parametersTermSourceREF,?componentsName,?componentsType,?componentsTypeTermAccessionNumber,?componentsTypeTermSourceREF) =
            member val Name = defaultArg name "" with get,set
            member val ProtocolType = defaultArg protocolType "" with get,set
            member val TypeTermAccessionNumber = defaultArg typeTermAccessionNumber "" with get,set
            member val TypeTermSourceREF = defaultArg typeTermSourceREF "" with get,set
            member val Description = defaultArg description "" with get,set
            member val URI = defaultArg uri "" with get,set
            member val Version = defaultArg version "" with get,set
            member val ParametersName = defaultArg parametersName "" with get,set
            member val ParametersTermAccessionNumber = defaultArg parametersTermAccessionNumber "" with get,set
            member val ParametersTermSourceREF = defaultArg parametersTermSourceREF "" with get,set
            member val ComponentsName = defaultArg componentsName "" with get,set
            member val ComponentsType = defaultArg componentsType "" with get,set
            member val ComponentsTypeTermAccessionNumber = defaultArg componentsTypeTermAccessionNumber "" with get,set
            member val ComponentsTypeTermSourceREF = defaultArg componentsTypeTermSourceREF "" with get,set
            interface ISAItem with
                member this.Header = "PROTOCOLS"
                member this.KeyPrefix = "Protocol"
                member this.KeyValues () = 
                    [
                    "Name",this.Name
                    "Type",this.ProtocolType
                    "Type Term Accession Number",this.TypeTermAccessionNumber
                    "Type Term Source REF",this.TypeTermSourceREF
                    "Description",this.Description
                    "URI",this.URI
                    "Version",this.Version
                    "Parameters Name",this.ParametersName
                    "Parameters Term Accession Number",this.ParametersTermAccessionNumber
                    "Parameters Term Source REF",this.ParametersTermSourceREF
                    "Components Name",this.ComponentsName
                    "Components Type",this.ComponentsType
                    "Components Type Term Accession Number",this.ComponentsTypeTermAccessionNumber
                    "Components Type Term Source REF",this.ComponentsTypeTermSourceREF
                    ]
                member this.KeyValuesOfInterest () = ["Name",this.Name]

        type InvestigationItem (?identifier,?title,?description,?submissionDate,?publicReleaseDate) =
            member val Identifier = defaultArg identifier "" with get,set
            member val Title = defaultArg title "" with get,set
            member val Description = defaultArg description "" with get,set
            member val SubmissionDate = defaultArg submissionDate "" with get,set
            member val PublicReleaseDate = defaultArg publicReleaseDate "" with get,set
            interface ISAItem with
                member this.Header = "INVESTIGATION"
                member this.KeyPrefix = "Investigation"
                member this.KeyValues () = 
                    [
                    "Identifier",this.Identifier
                    "Title",this.Title
                    "Description",this.Description
                    "Submission Date",this.SubmissionDate
                    "Public Release Date",this.PublicReleaseDate
                    ]
                member this.KeyValuesOfInterest () = ["Identifier",this.Identifier]

        type StudyItem (?identifier,?title,?description,?submissionDate,?publicReleaseDate,?fileName) =
            member val Identifier = defaultArg identifier "" with get,set
            member val Title = defaultArg title "" with get,set
            member val Description = defaultArg description "" with get,set
            member val SubmissionDate = defaultArg submissionDate "" with get,set
            member val PublicReleaseDate = defaultArg publicReleaseDate "" with get,set
            member val FileName = defaultArg fileName "" with get,set
            interface ISAItem with
                member this.Header = "STUDY"
                member this.KeyPrefix = "Study"
                member this.KeyValues () = 
                    [
                    "Identifier",this.Identifier
                    "Title",this.Title
                    "Description",this.Description
                    "Submission Date",this.SubmissionDate
                    "Public Release Date",this.PublicReleaseDate
                    "File Name",this.FileName
                    ]
                member this.KeyValuesOfInterest () = ["Identifier",this.Identifier]
    
