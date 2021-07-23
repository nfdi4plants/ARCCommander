namespace ArcCommander.CLIArguments
open Argu

type GitInitArgs =
    | [<Unique>][<AltCommandLine("-r")>] RepositoryAdress of repository_adress:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | RepositoryAdress _    ->  "Github Adress"

/// TO-DO: Argumente anpassen
type GitSyncArgs =
    | [<Unique>][<AltCommandLine("-r")>] RepositoryAdress of repository_adress:string
    | [<Unique>][<AltCommandLine("-m")>] CommitMessage of commit_message:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | RepositoryAdress _    ->  "Github Adress"
            | CommitMessage _       ->  "Commit Message"

type GitDiffArgs =
    | [<Unique>][<AltCommandLine("-d")>] Diff

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Diff _                -> "Shows the differences between this arc's files locally and one the respective server."

and GitDiff