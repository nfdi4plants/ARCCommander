namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing
open IsaXLSX.InvestigationFile

/// ArcCommander Investigation API functions that get executed by the investigation focused subcommand verbs
module InvestigationAPI =

    /// Creates an investigation file in the arc from the given investigation metadata contained in cliArgs that contains no studies or assays.
    let create (arcConfiguration:ArcConfiguration) (investigationArgs : Map<string,Argument>) =
           
        let investigationInfo = 
            InvestigationInfo.create
                (getFieldValueByName "Identifier" investigationArgs)
                (getFieldValueByName "Title" investigationArgs)
                (getFieldValueByName "Description" investigationArgs)
                (getFieldValueByName "SubmissionDate" investigationArgs)
                (getFieldValueByName "PublicReleaseDate" investigationArgs)
                []

        let investigation = Investigation.create [] investigationInfo [] [] [] []

        let investigationFilePath = IsaModelConfiguration.tryGetInvestigationFilePath arcConfiguration |> Option.get
                          
        IO.toFile investigationFilePath investigation

    /// [Not Implemented] Updates the existing investigation file in the arc with the given investigation metadata contained in cliArgs.
    let update (arcConfiguration:ArcConfiguration) (investigationArgs : Map<string,Argument>) = raise (NotImplementedException())
       
    /// [Not Implemented] Opens the existing investigation file in the arc with the text editor set in globalArgs, additionally setting the given investigation metadata contained in cliArgs.
    let edit (arcConfiguration:ArcConfiguration) (investigationArgs : Map<string,Argument>) = raise (NotImplementedException())
       
    /// [Not Implemented] Deletes the existing investigation file in the arc
    let delete (arcConfiguration:ArcConfiguration) (investigationArgs : Map<string,Argument>) = raise (NotImplementedException())
