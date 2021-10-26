namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments

open StudyContacts
open StudyPublications
open StudyDesignDescriptors
open StudyFactors
open StudyProtocols

/// Study object subcommand verbs
type StudyCommand =

    | [<CliPrefix(CliPrefix.None)>] Init        of init_args            : ParseResults<StudyInitArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args        : ParseResults<StudyRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Add         of add_args             : ParseResults<StudyAddArgs>

    | [<CliPrefix(CliPrefix.None)>] Delete      of delete_args          : ParseResults<StudyDeleteArgs>
    | [<CliPrefix(CliPrefix.None)>] Unregister  of unregister_args      : ParseResults<StudyUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Remove      of remove_args          : ParseResults<StudyRemoveArgs>

    | [<CliPrefix(CliPrefix.None)>] Update      of update_args          : ParseResults<StudyUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args            : ParseResults<StudyEditArgs>

    | [<CliPrefix(CliPrefix.None)>] Show        of show_args            : ParseResults<StudyShowArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List 

    | [<CliPrefix(CliPrefix.None)>] Person      of person_verbs         : ParseResults<StudyPersonCommand>
    | [<CliPrefix(CliPrefix.None)>] Publication of publication_verbs    : ParseResults<StudyPublicationCommand>
    | [<CliPrefix(CliPrefix.None)>] Design      of design_verbs         : ParseResults<StudyDesignCommand>
    | [<CliPrefix(CliPrefix.None)>] Factor      of factor_verbs         : ParseResults<StudyFactorCommand>
    | [<CliPrefix(CliPrefix.None)>] Protocol    of protocol_verbs       : ParseResults<StudyProtocolCommand>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init          _ -> "Initialize a new empty study file in the ARC"
            | Register      _ -> "Register an existing study in the ARC with the given assay metadata"
            | Add           _ -> "Create a new study file in the ARC and subsequently register it with the given study metadata"
            | Delete        _ -> "Delete a study from the ARC file structure"
            | Unregister    _ -> "Unregister a study from the ARC investigation file"
            | Remove        _ -> "Remove a study from the ARC"
            | Update        _ -> "Update an existing study in the ARC with the given study metadata"
            | Edit          _ -> "Open and edit an existing study in the ARC with a text editor. Arguments passed for this command will be pre-set in the editor."
            | Show          _ -> "Get the values of a study"
            | List          _ -> "List all studies registered in the ARC"
            | Person        _ -> "Person functions"
            | Publication   _ -> "Publication functions"
            | Design        _ -> "Design functions"
            | Factor        _ -> "Factor functions"
            | Protocol      _ -> "Protocol functions"

and StudyPersonCommand =

    | [<CliPrefix(CliPrefix.None)>] Update      of update_args:     ParseResults<PersonUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args:       ParseResults<PersonEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args:   ParseResults<PersonRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Unregister  of unregister_args: ParseResults<PersonUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Show        of show_args:       ParseResults<PersonShowArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update            _ -> "Update an existing person in the ARC investigation study with the given person metadata. The person is identified by the full name (first name, last name, mid initials)."
            | Edit              _ -> "Open and edit an existing person in the ARC investigation study with a text editor. The person is identified by the full name (first name, last name, mid initials)."
            | Register          _ -> "Register a person in the ARC investigation study with the given assay metadata"
            | Unregister        _ -> "Unregister a person from the given investigation study. The person is identified by the full name (first name, last name, mid initials)."
            | Show              _ -> "Get the metadata of a person registered in the ARC investigation study"
            | List              _ -> "List all persons registered in the ARC investigation"

and StudyPublicationCommand =

    | [<CliPrefix(CliPrefix.None)>] Update      of update_args:     ParseResults<PublicationUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args:       ParseResults<PublicationEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args:   ParseResults<PublicationRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Unregister  of unregister_args: ParseResults<PublicationUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Show        of show_args:       ParseResults<PublicationShowArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update            _ -> "Update an existing publication in the ARC investigation study with the given publication metadata. The publication is identified by the DOI."
            | Edit              _ -> "Open and edit an existing publication in the ARC investigation study with a text editor. The publication is identified by the DOI."
            | Register          _ -> "Register a publication in the ARC investigation study with the given assay metadata"
            | Unregister        _ -> "Unregister a publication from the given investigation study. The publication is identified by the DOI."
            | Show              _ -> "Get the metadata of a publication registered in the ARC investigation study"
            | List              _ -> "List all publication registered in the ARC investigation study"

and StudyDesignCommand =

    | [<CliPrefix(CliPrefix.None)>] Update      of update_args:     ParseResults<DesignUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args:       ParseResults<DesignEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args:   ParseResults<DesignRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Unregister  of unregister_args: ParseResults<DesignUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Show        of show_args:       ParseResults<DesignShowArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update            _ -> "Update an existing design in the ARC investigation study with the given design metadata. The design is identified by the design type."
            | Edit              _ -> "Open and edit an existing design in the ARC investigation study with a text editor. The design is identified by the design type."
            | Register          _ -> "Register a design in the ARC investigation study with the given assay metadata"
            | Unregister        _ -> "Unregister a design from the given investigation study. The design is identified by the design type."
            | Show              _ -> "Get the metadata of a design registered in the ARC investigation study"
            | List              _ -> "List all designs registered in the ARC investigation study"

and StudyFactorCommand =

    | [<CliPrefix(CliPrefix.None)>] Update      of update_args:     ParseResults<FactorUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args:       ParseResults<FactorEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args:   ParseResults<FactorRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Unregister  of unregister_args: ParseResults<FactorUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Show        of show_args:           ParseResults<FactorShowArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update            _ -> "Update an existing factor in the ARC investigation study with the given factor metadata. The factor is identified by name."
            | Edit              _ -> "Open and edit an existing factor in the ARC investigation study with a text editor. The factor is identified by name."
            | Register          _ -> "Register a factor in the ARC investigation study with the given assay metadata"
            | Unregister        _ -> "Unregister a factor from the given investigation study. The factor is identified by name."
            | Show              _ -> "Get the metadata of a factor registered in the ARC investigation study"
            | List              _ -> "List all factor registered in the ARC investigation study"

and StudyProtocolCommand =

    | [<CliPrefix(CliPrefix.None)>] Update      of update_args:     ParseResults<ProtocolUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit        of edit_args:       ParseResults<ProtocolEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Register    of register_args:   ParseResults<ProtocolRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Unregister  of unregister_args: ParseResults<ProtocolUnregisterArgs>
    | [<CliPrefix(CliPrefix.None)>] Load        of load_args:       ParseResults<ProtocolLoadArgs>
    | [<CliPrefix(CliPrefix.None)>] Show        of show_args:       ParseResults<ProtocolShowArgs>
    | [<CliPrefix(CliPrefix.None)>] [<SubCommand()>] List

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Update            _ -> "Update an existing protocol in the ARC investigation study with the given protocol metadata. The protocol is identified by name."
            | Edit              _ -> "Open and edit an existing protocol in the ARC investigation study with a text editor. The protocol is identified by name."
            | Register          _ -> "Register a protocol in the ARC investigation study with the given assay metadata"
            | Unregister        _ -> "Unregister a protocol from the given investigation study. The protocol is identified by name."
            | Load              _ -> "Load a protocol from an ISA JSON file and add it to the study"
            | Show              _ -> "Get the metadata of a protocol registered in the arc investigation study"
            | List              _ -> "List all protocol registered in the ARC investigation study"
