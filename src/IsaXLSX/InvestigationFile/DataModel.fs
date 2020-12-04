namespace IsaXLSX.InvestigationFile

open System


type InvestigationItem =
    {
    Identifier : string
    Title : string
    Description : string
    SubmissionDate : string
    PublicReleaseDate : string
    Comments : (string*string) list
    }

    static member create identifier title description submissionDate publicReleaseDate comments =
        {
        Identifier = identifier
        Title = title
        Description = description
        SubmissionDate = submissionDate
        PublicReleaseDate = publicReleaseDate
        Comments = comments        
        }
    
    static member IdentifierLabel           = "Identifier"
    static member TitleLabel                = "Title"
    static member DescriptionLabel          = "Description"
    static member SubmissionDateLabel       = "Submission Date"
    static member PublicReleaseDateLabel    = "Public Release Date"
