namespace ISA


module DataModel =

    module InvestigationFile = 

        type ISAItem =
            abstract member Header : string
            abstract member KeyPrefix : string
            abstract member KeyValues : unit -> (string*string) list

        type TermSource (?name,?file,?version,?description) =
            member val Name = defaultArg name "" with get,set
            member val File = defaultArg file "" with get,set
            member val Version = defaultArg version "" with get,set
            member val Description = defaultArg description "" with get,set
    
        type Publication (?pubMedID,?doi,?authorList,?title,?status,?termAccessionNumber,?termSourceREF) =
            member val PubMedID = defaultArg pubMedID "" with get,set
            member val DOI = defaultArg doi "" with get,set
            member val AuthorList = defaultArg authorList "" with get,set
            member val Title = defaultArg title "" with get,set
            member val Status = defaultArg status "" with get,set
            member val TermAccessionNumber = defaultArg termAccessionNumber "" with get,set
            member val TermSourceREF = defaultArg termSourceREF "" with get,set
    
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
    
        type DesignDescriptor (?designType,?typeTermAccessionNumber,?typeTermSourceREF) =
            member val DesignType = defaultArg designType "" with get,set
            member val TypeTermAccessionNumber = defaultArg typeTermAccessionNumber "" with get,set
            member val TypeTermSourceREF = defaultArg typeTermSourceREF "" with get,set
    
        type Factor (?name,?factorType,?typeTermAccessionNumber,?typeTermSourceREF) =
            member val Name = defaultArg name "" with get,set
            member val FactorType = defaultArg factorType "" with get,set
            member val TypeTermAccessionNumber = defaultArg typeTermAccessionNumber "" with get,set
            member val TypeTermSourceREF = defaultArg typeTermSourceREF "" with get,set
    
        type Assay (?measurementType,?measurementTypeTermAccessionNumber,?measurementTypeTermSourceREF,?technologyType,?technologyTypeTermAccessionNumber,?technologyTypeTermSourceREF,?technologyPlatform,?fileName) =
            member val MeasurementType = defaultArg measurementType "" with get,set
            member val MeasurementTypeTermAccessionNumber = defaultArg measurementTypeTermAccessionNumber "" with get,set
            member val MeasurementTypeTermSourceREF = defaultArg measurementTypeTermSourceREF "" with get,set
            member val TechnologyType = defaultArg technologyType "" with get,set
            member val TechnologyTypeTermAccessionNumber = defaultArg technologyTypeTermAccessionNumber "" with get,set
            member val TechnologyTypeTermSourceREF = defaultArg technologyTypeTermSourceREF "" with get,set
            member val TechnologyPlatform = defaultArg technologyPlatform "" with get,set
            member val FileName = defaultArg fileName "" with get,set
    
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
    
        type InvestigationItem (?identifier,?title,?description,?submissionDate,?publicReleaseDate) =
            member val Identifier = defaultArg identifier "" with get,set
            member val Title = defaultArg title "" with get,set
            member val Description = defaultArg description "" with get,set
            member val SubmissionDate = defaultArg submissionDate "" with get,set
            member val PublicReleaseDate = defaultArg publicReleaseDate "" with get,set
    
        type StudyItem (?identifier,?title,?description,?submissionDate,?publicReleaseDate,?fileName) =
            member val Identifier = defaultArg identifier "" with get,set
            member val Title = defaultArg title "" with get,set
            member val Description = defaultArg description "" with get,set
            member val SubmissionDate = defaultArg submissionDate "" with get,set
            member val PublicReleaseDate = defaultArg publicReleaseDate "" with get,set
            member val FileName = defaultArg fileName "" with get,set
    
        type Study (?info:StudyItem,?designDescriptors:DesignDescriptor [],?publications:Publication [],?factors:Factor [],?assays:Assay[],?protocols:Protocol[],?contacts:Person[]) =
            member val Info = defaultArg info (StudyItem()) with get,set
            member val DesignDescriptors = defaultArg designDescriptors [||] with get,set
            member val Publications = defaultArg publications [||] with get,set
            member val Factors = defaultArg factors [||] with get,set
            member val Assays = defaultArg assays [||] with get,set
            member val Protocols = defaultArg protocols [||] with get,set
            member val Contacts = defaultArg contacts [||] with get,set
    
    
