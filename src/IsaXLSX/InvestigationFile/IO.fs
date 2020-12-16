namespace IsaXLSX.InvestigationFile

open System.Collections.Generic
open FSharpSpreadsheetML
open DocumentFormat.OpenXml.Spreadsheet
open IsaXLSX.InvestigationFile
open System.Text.RegularExpressions

module Array = 
    
    let ofIndexedSeq (s : seq<int*string>) = 
        Array.init 
            (Seq.maxBy fst s |> fst |> (+) 1)
            (fun i -> 
                match Seq.tryFind (fst >> (=) i) s with
                | Some (i,x) -> x
                | None -> ""
                
            )

    let trySkip i a =
        try
            Array.skip i a
            |> Some
        with
        | _ -> None

    let itemWithDefault i d a = 
        match Array.tryItem i a with
        | Some v -> v
        | None -> d


module IO = 


    

    let (|Comment|_|) (key : Option<string>) =
        key
        |> Option.bind (fun k ->
            let r = Regex.Match(k,@"(?<=Comment[<\[]).*(?=[>\]])")
            if r.Success then Some r.Value
            else None
        )
    
    let (|Remark|_|) (key : Option<string>) =
        key
        |> Option.bind (fun k ->
            let r = Regex.Match(k,@"(?<=#).*")
            if r.Success then Some r.Value
            else None
        )

    let readInvestigationItem (en:IEnumerator<Row>) lineNumber =
        let rec loop identifier title description submissionDate publicReleaseDate comments remarks lineNumber = 

            if en.MoveNext() then    

                let row = en.Current
                
                match Row.tryGetValueAt 1u row, Row.tryGetValueAt 2u row with

                | Comment k, Some v -> 
                    loop identifier title description submissionDate publicReleaseDate ((k,v) :: comments) remarks (lineNumber + 1)         
                | Comment k, None -> 
                    loop identifier title description submissionDate publicReleaseDate ((k,"") :: comments) remarks (lineNumber + 1)         

                | Remark k, _  -> 
                    loop identifier title description submissionDate publicReleaseDate comments ((lineNumber,k) :: remarks) (lineNumber + 1)

                | Some k, Some identifier when k = "Investigation " + InvestigationItem.IdentifierLabel->              
                    loop identifier title description submissionDate publicReleaseDate comments remarks (lineNumber + 1)

                | Some k, Some title when k = "Investigation " + InvestigationItem.TitleLabel ->
                    loop identifier title description submissionDate publicReleaseDate comments remarks (lineNumber + 1)

                |Some k, Some description when k = "Investigation " + InvestigationItem.DescriptionLabel ->
                    loop identifier title description submissionDate publicReleaseDate comments remarks (lineNumber + 1)

                | Some k, Some submissionDate when k = "Investigation " + InvestigationItem.SubmissionDateLabel ->
                    loop identifier title description submissionDate publicReleaseDate comments remarks (lineNumber + 1)

                | Some k, Some publicReleaseDate when k = "Investigation " + InvestigationItem.PublicReleaseDateLabel ->
                    loop identifier title description submissionDate publicReleaseDate comments remarks (lineNumber + 1)

                | Some k, _ -> Some k,lineNumber, remarks, InvestigationItem.create identifier title description submissionDate publicReleaseDate (comments |> List.rev)
                | _ -> 
                    None,lineNumber, remarks, InvestigationItem.create identifier title description submissionDate publicReleaseDate (comments |> List.rev)
            else
                None,lineNumber, remarks, InvestigationItem.create identifier title description submissionDate publicReleaseDate (comments |> List.rev)
        loop "" "" "" "" "" [] [] lineNumber

    let readStudyItem (en:IEnumerator<Row>) lineNumber =
        let rec loop identifier title description submissionDate publicReleaseDate fileName comments remarks lineNumber = 

            if en.MoveNext() then        
                let row = en.Current
                match Row.tryGetValueAt 1u row, Row.tryGetValueAt 2u row with

                | Comment k, Some v -> 
                    loop identifier title description submissionDate publicReleaseDate fileName ((k,v) :: comments) remarks (lineNumber + 1)         
                | Comment k, None -> 
                    loop identifier title description submissionDate publicReleaseDate fileName ((k,"") :: comments) remarks (lineNumber + 1)         

                | Remark k, _  -> 
                    loop identifier title description submissionDate publicReleaseDate fileName comments ((lineNumber,k) :: remarks) (lineNumber + 1)

                | Some k, Some identifier when k = "Study " + StudyItem.IdentifierLabel->              
                    loop identifier  title description submissionDate publicReleaseDate fileName comments remarks (lineNumber + 1)

                | Some k, Some title when k = "Study " + StudyItem.TitleLabel ->
                    loop identifier title description submissionDate publicReleaseDate fileName comments remarks (lineNumber + 1)

                |Some k, Some description when k = "Study " + StudyItem.DescriptionLabel ->
                    loop identifier title description submissionDate publicReleaseDate fileName comments remarks (lineNumber + 1)

                | Some k, Some submissionDate when k = "Study " + StudyItem.SubmissionDateLabel ->
                    loop identifier title description submissionDate publicReleaseDate fileName comments remarks (lineNumber + 1)

                | Some k, Some publicReleaseDate when k = "Study " + StudyItem.PublicReleaseDateLabel ->
                    loop identifier title description submissionDate publicReleaseDate fileName comments remarks (lineNumber + 1)

                | Some k, Some fileName when k = "Study " + StudyItem.FileNameLabel ->
                    loop identifier title description submissionDate publicReleaseDate fileName comments remarks (lineNumber + 1)

                | Some k, _ -> Some k,lineNumber, remarks, StudyItem.create identifier title description submissionDate publicReleaseDate fileName (comments |> List.rev)
                | _ -> 
                    None,lineNumber, remarks, StudyItem.create identifier title description submissionDate publicReleaseDate fileName (comments |> List.rev)
            else
                None,lineNumber, remarks, StudyItem.create identifier title description submissionDate publicReleaseDate fileName (comments |> List.rev)
        loop "" "" "" "" "" "" [] [] lineNumber

    let writeInvestigationItem (investigationItem : InvestigationItem) =  

        [
            [InvestigationItem.IdentifierLabel;         investigationItem.Identifier]
            [InvestigationItem.TitleLabel;              investigationItem.Title]
            [InvestigationItem.DescriptionLabel;        investigationItem.Description]
            [InvestigationItem.SubmissionDateLabel;     investigationItem.SubmissionDate]
            [InvestigationItem.PublicReleaseDateLabel;  investigationItem.PublicReleaseDate]
            yield! (investigationItem.Comments |> List.map (fun (k,v) -> [k;v]))         
        ]
       
    //let readAssays (en:IEnumerator<Row>) (prefix : string) lineNumber =
    //    let rec loop (en:IEnumerator<Row>) (prefix : string)
    //        measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs 
    //        technologyType technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms
    //        identifiers
    //        comments remarks lineNumber = 
        
    //        let create () = 
    //            let length =
    //                [|measurementTypes;measurementTypeTermAccessionNumbers;measurementTypeTermSourceREFs;technologyType;technologyTypeTermAccessionNumbers;technologyTypeTermSourceREFs;technologyPlatforms;identifiers|]
    //                |> Array.map Array.length
    //                |> Array.max
            
    //            List.init length (fun i ->
    //                Assay.create 
    //                    (Array.itemWithDefault i "" measurementTypes)
    //                    (Array.itemWithDefault i "" measurementTypeTermAccessionNumbers)
    //                    (Array.itemWithDefault i "" measurementTypeTermSourceREFs)
    //                    (Array.itemWithDefault i "" technologyType)
    //                    (Array.itemWithDefault i "" technologyTypeTermAccessionNumbers)
    //                    (Array.itemWithDefault i "" technologyTypeTermSourceREFs)
    //                    (Array.itemWithDefault i "" technologyPlatforms)
    //                    (Array.itemWithDefault i "" identifiers)
    //                    (List.map (fun (key,values) -> key,Array.itemWithDefault i "" values) comments)           
    //            )

    //        if en.MoveNext() then        
    //            let row = en.Current |> Row.getIndexedValues |> Seq.map (fun (i,v) -> int i - 1,v) |> Array.ofIndexedSeq
    //            match Array.tryItem 0 row , Array.trySkip 1 row with

    //            | Comment k, Some v -> 
    //                loop 
    //                    en prefix measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs 
    //                    technologyType technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms
    //                    identifiers ((k,v) :: comments) remarks (lineNumber + 1)         

    //            | Remark k, _  -> 
    //                loop 
    //                    en prefix measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs 
    //                    technologyType technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms
    //                    identifiers comments ((lineNumber,k) :: remarks) (lineNumber + 1)

    //            | Some k, Some v when k = prefix + " " + Assay.MeasurementTypeLabel ->              
    //                loop 
    //                    en prefix v measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs 
    //                    technologyType technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms
    //                    identifiers comments remarks (lineNumber + 1)

    //            | Some k, Some v when k = prefix + " " + Assay.MeasurementTypeTermAccessionNumberLabel ->              
    //                loop 
    //                    en prefix measurementTypes v measurementTypeTermSourceREFs 
    //                    technologyType technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms
    //                    identifiers comments remarks (lineNumber + 1)

    //            | Some k, Some v when k = prefix + " " + Assay.MeasurementTypeTermSourceREFLabel ->              
    //                loop 
    //                    en prefix measurementTypes measurementTypeTermAccessionNumbers v 
    //                    technologyType technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms
    //                    identifiers comments remarks (lineNumber + 1)

    //            | Some k, Some v when k = prefix + " " + Assay.TechnologyTypeLabel ->              
    //                loop 
    //                    en prefix measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs 
    //                    v technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms
    //                    identifiers comments remarks (lineNumber + 1)

    //            | Some k, Some v when k = prefix + " " + Assay.TechnologyTypeTermAccessionNumberLabel ->              
    //                loop 
    //                    en prefix measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs 
    //                    technologyType v technologyTypeTermSourceREFs technologyPlatforms
    //                    identifiers comments remarks (lineNumber + 1)

    //            | Some k, Some v when k = prefix + " " + Assay.TechnologyTypeTermSourceREFLabel ->              
    //                loop 
    //                    en prefix measurementTypes technologyTypeTermSourceREFs measurementTypeTermSourceREFs 
    //                    technologyType technologyTypeTermAccessionNumbers v technologyPlatforms
    //                    identifiers comments remarks (lineNumber + 1)

    //            | Some k, Some v when k = prefix + " " + Assay.TechnologyPlatformLabel ->              
    //                loop 
    //                    en prefix measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs 
    //                    technologyType technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs v
    //                    identifiers comments remarks (lineNumber + 1)

    //            | Some k, Some v when k = prefix + " " + Assay.FileNameLabel ->              
    //                loop 
    //                    en prefix measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs 
    //                    technologyType technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms
    //                    v comments remarks (lineNumber + 1)

    //            | Some k, _ -> Some k,lineNumber,remarks,create ()
    //            | _ -> None, lineNumber,remarks,create ()
    //        else
    //            None,lineNumber,remarks,create ()
    //    loop en prefix [||] [||] [||] [||] [||] [||] [||] [||] [] [] lineNumber

    let readTermSources (en:IEnumerator<Row>) lineNumber =
        let rec loop 
            names files versions descriptions
            comments remarks lineNumber = 

            let create () = 
                let length =
                    [|names;files;versions;descriptions|]
                    |> Array.map Array.length
                    |> Array.max

                List.init length (fun i ->
                    TermSource.create
                        (Array.itemWithDefault i "" names)
                        (Array.itemWithDefault i "" files)
                        (Array.itemWithDefault i "" versions)
                        (Array.itemWithDefault i "" descriptions)
                        (List.map (fun (key,values) -> key,Array.itemWithDefault i "" values) comments)
                )
            if en.MoveNext() then  
                let row = en.Current |> Row.getIndexedValues |> Seq.map (fun (i,v) -> int i - 1,v) |> Array.ofIndexedSeq

                Array.iter (printfn "termRow: %s") row
                match Array.tryItem 0 row , Array.trySkip 1 row with

                | Comment k, Some v -> 
                    loop 
                        names files versions descriptions
                        ((k,v) :: comments) remarks (lineNumber + 1)

                | Remark k, _  -> 
                    loop 
                        names files versions descriptions
                        comments ((lineNumber,k) :: remarks) (lineNumber + 1)

                | Some k, Some names when k = "Term Source " + TermSource.NameLabel -> 
                    loop 
                        names files versions descriptions
                        comments remarks (lineNumber + 1)

                | Some k, Some files when k = "Term Source " + TermSource.FileLabel -> 
                    loop 
                        names files versions descriptions
                        comments remarks (lineNumber + 1)

                | Some k, Some versions when k = "Term Source " + TermSource.VersionLabel -> 
                    loop 
                        names files versions descriptions
                        comments remarks (lineNumber + 1)

                | Some k, Some descriptions when k = "Term Source " + TermSource.DescriptionLabel -> 
                    loop 
                        names files versions descriptions
                        comments remarks (lineNumber + 1)

                | Some k, _ -> Some k,lineNumber,remarks,create ()
                | _ -> None, lineNumber,remarks,create ()
            else
                None,lineNumber,remarks,create ()
        loop [||] [||] [||] [||] [] [] lineNumber



    let readPublications (en:IEnumerator<Row>) (prefix : string) lineNumber =
        let rec loop 
            pubMedIDs dois authorLists titles statuss statusTermAccessionNumbers statusTermSourceREFs
            comments remarks lineNumber = 

            let create () = 
                let length =
                    [|pubMedIDs;dois;authorLists;titles;statuss;statusTermAccessionNumbers;statusTermSourceREFs|]
                    |> Array.map Array.length
                    |> Array.max

                List.init length (fun i ->
                    Publication.create
                        (Array.itemWithDefault i "" pubMedIDs)
                        (Array.itemWithDefault i "" dois)
                        (Array.itemWithDefault i "" authorLists)
                        (Array.itemWithDefault i "" titles)
                        (Array.itemWithDefault i "" statuss)
                        (Array.itemWithDefault i "" statusTermAccessionNumbers)
                        (Array.itemWithDefault i "" statusTermSourceREFs)
                        (List.map (fun (key,values) -> key,Array.itemWithDefault i "" values) comments)
                )
            if en.MoveNext() then  
                let row = en.Current |> Row.getIndexedValues |> Seq.map (fun (i,v) -> int i - 1,v) |> Array.ofIndexedSeq

                match Array.tryItem 0 row , Array.trySkip 1 row with

                | Comment k, Some v -> 
                    loop 
                        pubMedIDs dois authorLists titles statuss statusTermAccessionNumbers statusTermSourceREFs
                        ((k,v) :: comments) remarks (lineNumber + 1)

                | Remark k, _  -> 
                    loop 
                        pubMedIDs dois authorLists titles statuss statusTermAccessionNumbers statusTermSourceREFs
                        comments ((lineNumber,k) :: remarks) (lineNumber + 1)

                | Some k, Some pubMedIDs when k = prefix + " " + Publication.PubMedIDLabel -> 
                    loop 
                        pubMedIDs dois authorLists titles statuss statusTermAccessionNumbers statusTermSourceREFs
                        comments remarks (lineNumber + 1)
                | Some k, Some pubMedIDs when k = prefix.Replace(" Publication","") + " " + Publication.PubMedIDLabel ->               
                    loop 
                        pubMedIDs dois authorLists titles statuss statusTermAccessionNumbers statusTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some dois when k = prefix + " " + Publication.DOILabel -> 
                    loop 
                        pubMedIDs dois authorLists titles statuss statusTermAccessionNumbers statusTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some authorLists when k = prefix + " " + Publication.AuthorListLabel -> 
                    loop 
                        pubMedIDs dois authorLists titles statuss statusTermAccessionNumbers statusTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some titles when k = prefix + " " + Publication.TitleLabel -> 
                    loop 
                        pubMedIDs dois authorLists titles statuss statusTermAccessionNumbers statusTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some statuss when k = prefix + " " + Publication.StatusLabel -> 
                    loop 
                        pubMedIDs dois authorLists titles statuss statusTermAccessionNumbers statusTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some statusTermAccessionNumbers when k = prefix + " " + Publication.StatusTermAccessionNumberLabel -> 
                    loop 
                        pubMedIDs dois authorLists titles statuss statusTermAccessionNumbers statusTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some statusTermSourceREFs when k = prefix + " " + Publication.StatusTermSourceREFLabel -> 
                    loop 
                        pubMedIDs dois authorLists titles statuss statusTermAccessionNumbers statusTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, _ -> Some k,lineNumber,remarks,create ()
                | _ -> None, lineNumber,remarks,create ()
            else
                None,lineNumber,remarks,create ()
        loop [||] [||] [||] [||] [||] [||] [||] [] [] lineNumber



    let readPersons (en:IEnumerator<Row>) (prefix : string) lineNumber =
        let rec loop 
            lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
            comments remarks lineNumber = 

            let create () = 
                let length =
                    [|lastNames;firstNames;midInitialss;emails;phones;faxs;addresss;affiliations;roless;rolesTermAccessionNumbers;rolesTermSourceREFs|]
                    |> Array.map Array.length
                    |> Array.max

                List.init length (fun i ->
                    Person.create
                        (Array.itemWithDefault i "" lastNames)
                        (Array.itemWithDefault i "" firstNames)
                        (Array.itemWithDefault i "" midInitialss)
                        (Array.itemWithDefault i "" emails)
                        (Array.itemWithDefault i "" phones)
                        (Array.itemWithDefault i "" faxs)
                        (Array.itemWithDefault i "" addresss)
                        (Array.itemWithDefault i "" affiliations)
                        (Array.itemWithDefault i "" roless)
                        (Array.itemWithDefault i "" rolesTermAccessionNumbers)
                        (Array.itemWithDefault i "" rolesTermSourceREFs)
                        (List.map (fun (key,values) -> key,Array.itemWithDefault i "" values) comments)
                )
            if en.MoveNext() then  
                let row = en.Current |> Row.getIndexedValues |> Seq.map (fun (i,v) -> int i - 1,v) |> Array.ofIndexedSeq
                match Array.tryItem 0 row , Array.trySkip 1 row with

                | Comment k, Some v -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        ((k,v) :: comments) remarks (lineNumber + 1)

                | Remark k, _  -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments ((lineNumber,k) :: remarks) (lineNumber + 1)

                | Some k, Some lastNames when k = prefix + " " + Person.LastNameLabel -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some firstNames when k = prefix + " " + Person.FirstNameLabel -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some midInitialss when k = prefix + " " + Person.MidInitialsLabel -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some emails when k = prefix + " " + Person.EmailLabel -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some phones when k = prefix + " " + Person.PhoneLabel -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some faxs when k = prefix + " " + Person.FaxLabel -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some addresss when k = prefix + " " + Person.AddressLabel -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some affiliations when k = prefix + " " + Person.AffiliationLabel -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some roless when k = prefix + " " + Person.RolesLabel -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some rolesTermAccessionNumbers when k = prefix + " " + Person.RolesTermAccessionNumberLabel -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some rolesTermSourceREFs when k = prefix + " " + Person.RolesTermSourceREFLabel -> 
                    loop 
                        lastNames firstNames midInitialss emails phones faxs addresss affiliations roless rolesTermAccessionNumbers rolesTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, _ -> Some k,lineNumber,remarks,create ()
                | _ -> None, lineNumber,remarks,create ()
            else
                None,lineNumber,remarks,create ()
        loop [||] [||] [||] [||] [||] [||] [||] [||] [||] [||] [||] [] [] lineNumber



    let readDesigns (en:IEnumerator<Row>) (prefix : string) lineNumber =
        let rec loop 
            designTypes typeTermAccessionNumbers typeTermSourceREFs
            comments remarks lineNumber = 

            let create () = 
                let length =
                    [|designTypes;typeTermAccessionNumbers;typeTermSourceREFs|]
                    |> Array.map Array.length
                    |> Array.max

                List.init length (fun i ->
                    Design.create
                        (Array.itemWithDefault i "" designTypes)
                        (Array.itemWithDefault i "" typeTermAccessionNumbers)
                        (Array.itemWithDefault i "" typeTermSourceREFs)
                        (List.map (fun (key,values) -> key,Array.itemWithDefault i "" values) comments)
                )
            if en.MoveNext() then  
                let row = en.Current |> Row.getIndexedValues |> Seq.map (fun (i,v) -> int i - 1,v) |> Array.ofIndexedSeq
                match Array.tryItem 0 row , Array.trySkip 1 row with

                | Comment k, Some v -> 
                    loop 
                        designTypes typeTermAccessionNumbers typeTermSourceREFs
                        ((k,v) :: comments) remarks (lineNumber + 1)

                | Remark k, _  -> 
                    loop 
                        designTypes typeTermAccessionNumbers typeTermSourceREFs
                        comments ((lineNumber,k) :: remarks) (lineNumber + 1)

                | Some k, Some designTypes when k = prefix + " " + Design.DesignTypeLabel -> 
                    loop 
                        designTypes typeTermAccessionNumbers typeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some typeTermAccessionNumbers when k = prefix + " " + Design.TypeTermAccessionNumberLabel -> 
                    loop 
                        designTypes typeTermAccessionNumbers typeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some typeTermSourceREFs when k = prefix + " " + Design.TypeTermSourceREFLabel -> 
                    loop 
                        designTypes typeTermAccessionNumbers typeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, _ -> Some k,lineNumber,remarks,create ()
                | _ -> None, lineNumber,remarks,create ()
            else
                None,lineNumber,remarks,create ()
        loop [||] [||] [||] [] [] lineNumber



    let readFactors (en:IEnumerator<Row>) (prefix : string) lineNumber =
        let rec loop 
            names factorTypes typeTermAccessionNumbers typeTermSourceREFs
            comments remarks lineNumber = 

            let create () = 
                let length =
                    [|names;factorTypes;typeTermAccessionNumbers;typeTermSourceREFs|]
                    |> Array.map Array.length
                    |> Array.max

                List.init length (fun i ->
                    Factor.create
                        (Array.itemWithDefault i "" names)
                        (Array.itemWithDefault i "" factorTypes)
                        (Array.itemWithDefault i "" typeTermAccessionNumbers)
                        (Array.itemWithDefault i "" typeTermSourceREFs)
                        (List.map (fun (key,values) -> key,Array.itemWithDefault i "" values) comments)
                )
            if en.MoveNext() then  
                let row = en.Current |> Row.getIndexedValues |> Seq.map (fun (i,v) -> int i - 1,v) |> Array.ofIndexedSeq
                match Array.tryItem 0 row , Array.trySkip 1 row with

                | Comment k, Some v -> 
                    loop 
                        names factorTypes typeTermAccessionNumbers typeTermSourceREFs
                        ((k,v) :: comments) remarks (lineNumber + 1)

                | Remark k, _  -> 
                    loop 
                        names factorTypes typeTermAccessionNumbers typeTermSourceREFs
                        comments ((lineNumber,k) :: remarks) (lineNumber + 1)

                | Some k, Some names when k = prefix + " " + Factor.NameLabel -> 
                    loop 
                        names factorTypes typeTermAccessionNumbers typeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some factorTypes when k = prefix + " " + Factor.FactorTypeLabel -> 
                    loop 
                        names factorTypes typeTermAccessionNumbers typeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some typeTermAccessionNumbers when k = prefix + " " + Factor.TypeTermAccessionNumberLabel -> 
                    loop 
                        names factorTypes typeTermAccessionNumbers typeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some typeTermSourceREFs when k = prefix + " " + Factor.TypeTermSourceREFLabel -> 
                    loop 
                        names factorTypes typeTermAccessionNumbers typeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, _ -> Some k,lineNumber,remarks,create ()
                | _ -> None, lineNumber,remarks,create ()
            else
                None,lineNumber,remarks,create ()
        loop [||] [||] [||] [||] [] [] lineNumber



    let readAssays (en:IEnumerator<Row>) (prefix : string) lineNumber =
        let rec loop 
            measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs technologyTypes technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms fileNames
            comments remarks lineNumber = 

            let create () = 
                let length =
                    [|measurementTypes;measurementTypeTermAccessionNumbers;measurementTypeTermSourceREFs;technologyTypes;technologyTypeTermAccessionNumbers;technologyTypeTermSourceREFs;technologyPlatforms;fileNames|]
                    |> Array.map Array.length
                    |> Array.max

                List.init length (fun i ->
                    Assay.create
                        (Array.itemWithDefault i "" measurementTypes)
                        (Array.itemWithDefault i "" measurementTypeTermAccessionNumbers)
                        (Array.itemWithDefault i "" measurementTypeTermSourceREFs)
                        (Array.itemWithDefault i "" technologyTypes)
                        (Array.itemWithDefault i "" technologyTypeTermAccessionNumbers)
                        (Array.itemWithDefault i "" technologyTypeTermSourceREFs)
                        (Array.itemWithDefault i "" technologyPlatforms)
                        (Array.itemWithDefault i "" fileNames)
                        (List.map (fun (key,values) -> key,Array.itemWithDefault i "" values) comments)
                )
            if en.MoveNext() then  
                let row = en.Current |> Row.getIndexedValues |> Seq.map (fun (i,v) -> int i - 1,v) |> Array.ofIndexedSeq
                match Array.tryItem 0 row , Array.trySkip 1 row with

                | Comment k, Some v -> 
                    loop 
                        measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs technologyTypes technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms fileNames
                        ((k,v) :: comments) remarks (lineNumber + 1)

                | Remark k, _  -> 
                    loop 
                        measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs technologyTypes technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms fileNames
                        comments ((lineNumber,k) :: remarks) (lineNumber + 1)

                | Some k, Some measurementTypes when k = prefix + " " + Assay.MeasurementTypeLabel -> 
                    loop 
                        measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs technologyTypes technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms fileNames
                        comments remarks (lineNumber + 1)

                | Some k, Some measurementTypeTermAccessionNumbers when k = prefix + " " + Assay.MeasurementTypeTermAccessionNumberLabel -> 
                    loop 
                        measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs technologyTypes technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms fileNames
                        comments remarks (lineNumber + 1)

                | Some k, Some measurementTypeTermSourceREFs when k = prefix + " " + Assay.MeasurementTypeTermSourceREFLabel -> 
                    loop 
                        measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs technologyTypes technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms fileNames
                        comments remarks (lineNumber + 1)

                | Some k, Some technologyTypes when k = prefix + " " + Assay.TechnologyTypeLabel -> 
                    loop 
                        measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs technologyTypes technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms fileNames
                        comments remarks (lineNumber + 1)

                | Some k, Some technologyTypeTermAccessionNumbers when k = prefix + " " + Assay.TechnologyTypeTermAccessionNumberLabel -> 
                    loop 
                        measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs technologyTypes technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms fileNames
                        comments remarks (lineNumber + 1)

                | Some k, Some technologyTypeTermSourceREFs when k = prefix + " " + Assay.TechnologyTypeTermSourceREFLabel -> 
                    loop 
                        measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs technologyTypes technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms fileNames
                        comments remarks (lineNumber + 1)

                | Some k, Some technologyPlatforms when k = prefix + " " + Assay.TechnologyPlatformLabel -> 
                    loop 
                        measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs technologyTypes technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms fileNames
                        comments remarks (lineNumber + 1)

                | Some k, Some fileNames when k = prefix + " " + Assay.FileNameLabel -> 
                    loop 
                        measurementTypes measurementTypeTermAccessionNumbers measurementTypeTermSourceREFs technologyTypes technologyTypeTermAccessionNumbers technologyTypeTermSourceREFs technologyPlatforms fileNames
                        comments remarks (lineNumber + 1)

                | Some k, _ -> Some k,lineNumber,remarks,create ()
                | _ -> None, lineNumber,remarks,create ()
            else
                None,lineNumber,remarks,create ()
        loop [||] [||] [||] [||] [||] [||] [||] [||] [] [] lineNumber



    let readProtocols (en:IEnumerator<Row>) (prefix : string) lineNumber =
        let rec loop 
            names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
            comments remarks lineNumber = 

            let create () = 
                let length =
                    [|names;protocolTypes;typeTermAccessionNumbers;typeTermSourceREFs;descriptions;uris;versions;parametersNames;parametersTermAccessionNumbers;parametersTermSourceREFs;componentsNames;componentsTypes;componentsTypeTermAccessionNumbers;componentsTypeTermSourceREFs|]
                    |> Array.map Array.length
                    |> Array.max

                List.init length (fun i ->
                    Protocol.create
                        (Array.itemWithDefault i "" names)
                        (Array.itemWithDefault i "" protocolTypes)
                        (Array.itemWithDefault i "" typeTermAccessionNumbers)
                        (Array.itemWithDefault i "" typeTermSourceREFs)
                        (Array.itemWithDefault i "" descriptions)
                        (Array.itemWithDefault i "" uris)
                        (Array.itemWithDefault i "" versions)
                        (Array.itemWithDefault i "" parametersNames)
                        (Array.itemWithDefault i "" parametersTermAccessionNumbers)
                        (Array.itemWithDefault i "" parametersTermSourceREFs)
                        (Array.itemWithDefault i "" componentsNames)
                        (Array.itemWithDefault i "" componentsTypes)
                        (Array.itemWithDefault i "" componentsTypeTermAccessionNumbers)
                        (Array.itemWithDefault i "" componentsTypeTermSourceREFs)
                        (List.map (fun (key,values) -> key,Array.itemWithDefault i "" values) comments)
                )
            if en.MoveNext() then  
                let row = en.Current |> Row.getIndexedValues |> Seq.map (fun (i,v) -> int i - 1,v) |> Array.ofIndexedSeq
                match Array.tryItem 0 row , Array.trySkip 1 row with

                | Comment k, Some v -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        ((k,v) :: comments) remarks (lineNumber + 1)

                | Remark k, _  -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments ((lineNumber,k) :: remarks) (lineNumber + 1)

                | Some k, Some names when k = prefix + " " + Protocol.NameLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some protocolTypes when k = prefix + " " + Protocol.ProtocolTypeLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some typeTermAccessionNumbers when k = prefix + " " + Protocol.TypeTermAccessionNumberLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some typeTermSourceREFs when k = prefix + " " + Protocol.TypeTermSourceREFLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some descriptions when k = prefix + " " + Protocol.DescriptionLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some uris when k = prefix + " " + Protocol.URILabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some versions when k = prefix + " " + Protocol.VersionLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some parametersNames when k = prefix + " " + Protocol.ParametersNameLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some parametersTermAccessionNumbers when k = prefix + " " + Protocol.ParametersTermAccessionNumberLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some parametersTermSourceREFs when k = prefix + " " + Protocol.ParametersTermSourceREFLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some componentsNames when k = prefix + " " + Protocol.ComponentsNameLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some componentsTypes when k = prefix + " " + Protocol.ComponentsTypeLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some componentsTypeTermAccessionNumbers when k = prefix + " " + Protocol.ComponentsTypeTermAccessionNumberLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, Some componentsTypeTermSourceREFs when k = prefix + " " + Protocol.ComponentsTypeTermSourceREFLabel -> 
                    loop 
                        names protocolTypes typeTermAccessionNumbers typeTermSourceREFs descriptions uris versions parametersNames parametersTermAccessionNumbers parametersTermSourceREFs componentsNames componentsTypes componentsTypeTermAccessionNumbers componentsTypeTermSourceREFs
                        comments remarks (lineNumber + 1)

                | Some k, _ -> Some k,lineNumber,remarks,create ()
                | _ -> None, lineNumber,remarks,create ()
            else
                None,lineNumber,remarks,create ()
        loop [||] [||] [||] [||] [||] [||] [||] [||] [||] [||] [||] [||] [||] [||] [] [] lineNumber

    let readStudy (en:IEnumerator<Row>) lineNumber = 

        let rec loop lastLine studyItem designDescriptors publications factors assays protocols contacts remarks lineNumber =
           
            match lastLine with

            | Some k when k = Study.DesignDescriptorsLabel -> 
                let currentLine,lineNumber,newRemarks,designDescriptors = readDesigns en "Study Design" (lineNumber + 1)         
                loop currentLine studyItem designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = Study.PublicationsLabel -> 
                let currentLine,lineNumber,newRemarks,publications = readPublications en "Study Publication" (lineNumber + 1)       
                loop currentLine studyItem designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = Study.FactorsLabel -> 
                let currentLine,lineNumber,newRemarks,factors = readFactors en "Study Factor" (lineNumber + 1)       
                loop currentLine studyItem designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = Study.AssaysLabel -> 
                let currentLine,lineNumber,newRemarks,assays = readAssays en "Study Assay" (lineNumber + 1)       
                loop currentLine studyItem designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = Study.ProtocolsLabel -> 
                let currentLine,lineNumber,newRemarks,protocols = readProtocols en "Study Protocol" (lineNumber + 1)  
                loop currentLine studyItem designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = Study.ContactsLabel -> 
                let currentLine,lineNumber,newRemarks,contacts = readPersons en "Study Person" (lineNumber + 1)  
                loop currentLine studyItem designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | k -> 
                k,lineNumber,remarks, Study.create studyItem designDescriptors publications factors assays protocols contacts

    
        let currentLine,lineNumber,remarks,item = readStudyItem en (lineNumber + 1) 
        loop currentLine item [] [] [] [] [] [] remarks lineNumber

    let readInvestigation (path:string) =

        let doc = 
            Spreadsheet.fromFile path false
        let en =
            doc
            |> Spreadsheet.getRowsBySheetIndex 0u
            |> fun s -> s.GetEnumerator()
               
        
        let emptyInvestigationItem = InvestigationItem.create "" "" "" "" "" []

        let rec loop lastLine ontologySourceReferences investigationItem publications contacts studies remarks lineNumber =
            printfn "Start l: %O" lastLine
            match lastLine with

            | Some k when k = Investigation.OntologySourceReferenceLabel -> 
                let currentLine,lineNumber,newRemarks,ontologySourceReferences = readTermSources en (lineNumber + 1)         
                loop currentLine ontologySourceReferences investigationItem publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = Investigation.InvestigationLabel -> 
                let currentLine,lineNumber,newRemarks,investigationItem = readInvestigationItem en (lineNumber + 1)       
                loop currentLine ontologySourceReferences investigationItem publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = Investigation.PublicationsLabel -> 
                let currentLine,lineNumber,newRemarks,publications = readPublications en "Investigation Publication" (lineNumber + 1)       
                loop currentLine ontologySourceReferences investigationItem publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = Investigation.ContactsLabel -> 
                let currentLine,lineNumber,newRemarks,contacts = readPersons en "Investigation Person" (lineNumber + 1)       
                loop currentLine ontologySourceReferences investigationItem publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = Investigation.StudyLabel -> 
                let currentLine,lineNumber,newRemarks,study = readStudy en (lineNumber + 1)  
                loop currentLine ontologySourceReferences investigationItem publications contacts (study::studies) (List.append remarks newRemarks) lineNumber

            | k -> 
                Investigation.create ontologySourceReferences investigationItem publications contacts (List.rev studies) remarks
    
        if en.MoveNext () then
            let currentLine = en.Current |> Row.tryGetValueAt 1u
            let i = loop currentLine [] emptyInvestigationItem [] [] [] [] 1
            Spreadsheet.close doc
            i
        else
            Spreadsheet.close doc
            failwith "emptyInvestigationFile"
        