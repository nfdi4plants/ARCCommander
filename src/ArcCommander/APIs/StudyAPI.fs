namespace ArcCommander.APIs

open System

open ArcCommander
open ArcCommander.ArgumentProcessing

open ISA.DataModel.InvestigationFile

/// ArcCommander Study API functions that get executed by the study focused subcommand verbs
module StudyAPI =

    /// [Not Implemented] Initializes a new empty study file in the arc.
    let init (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())
    
    /// [Not Implemented] Updates an existing study file in the arc with the given study metadata contained in cliArgs.
    let update (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())
    
    /// [Not Implemented] Opens an existing study file in the arc with the text editor set in globalArgs, additionally setting the given study metadata contained in cliArgs.
    let edit (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())
    
    /// Registers an existing study in the arc's investigation file with the given study metadata contained in cliArgs.
    let register (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) =

        let study = isaItemOfParameters (StudyItem()) cliArgs

        let investigationFilePath = IO.findInvestigationFile globalArgs.["WorkingDir"]          
        
        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true
        if ISA_XLSX.IO.ISA_Investigation.studyExists study.Identifier doc then
            printfn "Study %s already exists" study.Identifier
        else 
            ISA_XLSX.IO.ISA_Investigation.addStudy study doc |> ignore
        doc.Save()
        doc.Close()

    /// [Not Implemented] Creates a new study file in the arc and registers it in the arc's investigation file with the given study metadata contained in cliArgs.
    let add (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())
    
    /// [Not Implemented] Removes a study file from the arc's investigation file study register
    let remove (globalArgs:Map<string,string>) (cliArgs : Map<string,string>) = raise (NotImplementedException())

    /// Lists all study identifiers registered in this arc's investigation file
    let list (globalArgs:Map<string,string>) =

        printfn "Start study list"
        
        let investigationFilePath = IO.findInvestigationFile globalArgs.["WorkingDir"]
        printfn "InvestigationFile: %s"  investigationFilePath

        let doc = FSharpSpreadsheetML.Spreadsheet.fromFile investigationFilePath true

        let studies = ISA_XLSX.IO.ISA_Investigation.getStudies doc
        
        if Seq.isEmpty studies then 
            printfn "The Investigation contains no studies"
        else 
            studies
            |> Seq.iter (fun s ->
            
                printfn "Study: %s" s.Identifier
            )
