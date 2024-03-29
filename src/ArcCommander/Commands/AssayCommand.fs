﻿namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments
open AssayContacts

/// Assay object subcommand verbs
type AssayCommand = 
    //Additions
    | [<CliPrefix(CliPrefix.None)>] Init        of init_args :          ParseResults<AssayInitArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args :      ParseResults<AssayRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Add         of add_args :           ParseResults<AssayAddArgs>
    //Removals
    | [<CliPrefix(CliPrefix.None)>] Delete      of delete_args :        ParseResults<AssayDeleteArgs>
    | [<CliPrefix(CliPrefix.None)>] Unregister  of unregister_args :    ParseResults<AssayUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove      of remove_args :        ParseResults<AssayRemoveArgs>
    //Modifications
    | [<CliPrefix(CliPrefix.None)>] Update      of update_args :        ParseResults<AssayUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args :          ParseResults<AssayEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Move        of move_args :          ParseResults<AssayMoveArgs>
    //Retrievals
    | [<CliPrefix(CliPrefix.None)>] Show        of show_args :          ParseResults<AssayShowArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List
    | [<CliPrefix(CliPrefix.None)>] Export      of export_args :        ParseResults<AssayExportArgs>

    | [<CliPrefix(CliPrefix.None)>] Person      of person_verbs :       ParseResults<AssayPersonCommand>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init              _ -> "Initialize a new empty assay and associated folder structure in the ARC"
            | Register          _ -> "Register an existing assay in the ARC with the given assay metadata"
            | Add               _ -> "Create a new assay file and associated folder structure in the ARC and subsequently register it with the given assay metadata"
            
            | Delete            _ -> "Delete the given assays folder and its underlying file structure"
            | Unregister        _ -> "Unregister an assay from the given study's assay register in the investigation file"
            | Remove            _ -> "Both unregister an assay from the investigation file and delete its folders and files"

            | Update            _ -> "Update an existing assay in the ARC with the given assay metadata"
            | Edit              _ -> "Open and edit an existing assay in the ARC with a text editor. Arguments passed for this command will be pre-set in the editor."
            | Move              _ -> "Move an assay from one study to another"

            | Show              _ -> "Gets the values of an existing assay"
            | List              _ -> "List all assays registered in the ARC"
            | Export            _ -> "Export a specific assay to json"

            | Person            _ -> "Person functions"

and AssayPersonCommand =
    
    | [<CliPrefix(CliPrefix.None)>] Update      of update_args :        ParseResults<PersonUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args :          ParseResults<PersonEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args :      ParseResults<PersonRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Unregister  of unregister_args :    ParseResults<PersonUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Show        of show_args :          ParseResults<PersonShowArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update            _ -> "Update an existing person in this assay with the given person metadata. The person is identified by the full name (first name, last name, mid initials)."
            | Edit              _ -> "Open and edit an existing person in this assay with a text editor. The person is identified by the full name (first name, last name, mid initials)."
            | Register          _ -> "Register a person in this assay study with the given assay metadata"
            | Unregister        _ -> "Unregister a person from the given investigation study. The person is identified by the full name (first name, last name, mid initials)."
            | Show              _ -> "Get the metadata of a person registered in this assay"
            | List              _ -> "List all persons registered in this assay"