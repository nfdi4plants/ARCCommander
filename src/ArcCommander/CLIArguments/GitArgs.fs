namespace ArcCommander.CLIArguments
open Argu


/// TO-DO: Argumente anpassen
type GitUpdateArgs =
    | [<Unique>][<AltCommandLine("-r")>] RepositoryAdress of repository_adress:string
    | [<Unique>][<AltCommandLine("-m")>] CommitMessage of commit_message:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | RepositoryAdress _    ->  "Github Adress"
            | CommitMessage _       ->  "Commit Message"