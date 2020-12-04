namespace IsaXLSX.InvestigationFile

open System.Collections.Generic
open FSharpSpreadsheetML
open DocumentFormat.OpenXml.Spreadsheet
open IsaXLSX.InvestigationFile
open System.Text.RegularExpressions

module IO = 

    let (|Comment|_|) (key : Option<string>) =
        key
        |> Option.bind (fun k ->
            let r = Regex.Match(k,@"(?<=Comment\[<).*(?=>\])")
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

    let rec readInvestigationItem (en:IEnumerator<Row>) identifier title description submissionDate publicReleaseDate comments remarks lineNumber = 

        if en.MoveNext() then        
            let row = en.Current
            match Row.tryGetValueAt 1u row, Row.tryGetValueAt 2u row with

            | Comment k, Some v -> 
                readInvestigationItem en identifier title description submissionDate publicReleaseDate ((k,v) :: comments) remarks (lineNumber + 1)         

            | Remark k, _  -> 
                readInvestigationItem en identifier title description submissionDate publicReleaseDate comments ((lineNumber,k) :: remarks) (lineNumber + 1)

            | Some k, Some v when k = "Investigation " + InvestigationItem.IdentifierLabel->              
                readInvestigationItem en v          title description submissionDate publicReleaseDate comments remarks (lineNumber + 1)

            | Some k, Some v when k = "Investigation " + InvestigationItem.TitleLabel ->
                readInvestigationItem en identifier v     description submissionDate publicReleaseDate comments remarks (lineNumber + 1)

            |Some k, Some v when k = "Investigation " + InvestigationItem.DescriptionLabel ->
                readInvestigationItem en identifier title v           submissionDate publicReleaseDate comments remarks (lineNumber + 1)

            | Some k, Some v when k = "Investigation " + InvestigationItem.SubmissionDateLabel ->
                readInvestigationItem en identifier title description v              publicReleaseDate comments remarks (lineNumber + 1)

            | Some k, Some v when k = "Investigation " + InvestigationItem.PublicReleaseDateLabel ->
                readInvestigationItem en identifier title description submissionDate v                 comments remarks (lineNumber + 1)

            | _ -> 
                lineNumber, remarks, InvestigationItem.create identifier title description submissionDate publicReleaseDate (comments |> List.rev)
        else
            lineNumber, remarks, InvestigationItem.create identifier title description submissionDate publicReleaseDate (comments |> List.rev)
    
    let writeInvestigationItem (investigationItem : InvestigationItem) =  

        [
            [InvestigationItem.IdentifierLabel;         investigationItem.Identifier]
            [InvestigationItem.TitleLabel;              investigationItem.Title]
            [InvestigationItem.DescriptionLabel;        investigationItem.Description]
            [InvestigationItem.SubmissionDateLabel;     investigationItem.SubmissionDate]
            [InvestigationItem.PublicReleaseDateLabel;  investigationItem.PublicReleaseDate]
            yield! (investigationItem.Comments |> List.map (fun (k,v) -> [k;v]))         
        ]
        
        