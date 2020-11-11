namespace ArcCommander.Commands

open Argu 
open ArcCommander.CLIArguments

type InvestigationCommand = 
    
    | [<CliPrefix(CliPrefix.None)>] Create of create_args: ParseResults<InvestigationCreateArgs>
    | [<CliPrefix(CliPrefix.None)>] Update of update_args: ParseResults<InvestigationUpdateArgs>
    | [<CliPrefix(CliPrefix.None)>] Edit of edit_args: ParseResults<InvestigationEditArgs>
    | [<CliPrefix(CliPrefix.None)>] Delete of delete_args: ParseResults<InvestigationDeleteArgs>
    
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Create            _ -> "Create a new investigation with the given metadata"
            | Update            _ -> "Update the arc's investigation with the given metdadata"
            | Edit              _ -> "Open an editor window to directly edit the arc's investigation file"
            | Delete            _ -> "Delete the arc's investigation file (danger zone!)"
