module Helpers

open Fake.Core
open System.Runtime.InteropServices

let initializeContext () =
    let execContext = Context.FakeExecutionContext.Create false "build.fsx" [ ]
    Context.setExecutionContext (Context.RuntimeContext.Fake execContext)

module Proc =
    module Parallel =
        open System

        let locker = obj()

        let colors =
            [| ConsoleColor.Blue
               ConsoleColor.Yellow
               ConsoleColor.Magenta
               ConsoleColor.Cyan
               ConsoleColor.DarkBlue
               ConsoleColor.DarkYellow
               ConsoleColor.DarkMagenta
               ConsoleColor.DarkCyan |]

        let print color (colored: string) (line: string) =
            lock locker
                (fun () ->
                    let currentColor = Console.ForegroundColor
                    Console.ForegroundColor <- color
                    Console.Write colored
                    Console.ForegroundColor <- currentColor
                    Console.WriteLine line)

        let onStdout index name (line: string) =
            let color = colors.[index % colors.Length]
            if isNull line then
                print color $"{name}: --- END ---" ""
            else if String.isNotNullOrEmpty line then
                print color $"{name}: " line

        let onStderr name (line: string) =
            let color = ConsoleColor.Red
            if isNull line |> not then
                print color $"{name}: " line

        let redirect (index, (name, createProcess)) =
            createProcess
            |> CreateProcess.redirectOutputIfNotRedirected
            |> CreateProcess.withOutputEvents (onStdout index name) (onStderr name)

        let printStarting indexed =
            for (index, (name, c: CreateProcess<_>)) in indexed do
                let color = colors.[index % colors.Length]
                let wd =
                    c.WorkingDirectory
                    |> Option.defaultValue ""
                let exe = c.Command.Executable
                let args = c.Command.Arguments.ToStartInfo
                print color $"{name}: {wd}> {exe} {args}" ""

        let run cs =
            cs
            |> Seq.toArray
            |> Array.indexed
            |> fun x -> printStarting x; x
            |> Array.map redirect
            |> Array.Parallel.map Proc.run

/// user interaction prompts for critical build tasks where you may want to interrupt when you see wrong inputs.
[<AutoOpen>]
module MessagePrompts =

    let prompt (msg:string) =
        System.Console.Write(msg)
        System.Console.ReadLine().Trim()
        |> function | "" -> None | s -> Some s
        |> Option.map (fun s -> s.Replace ("\"","\\\""))

    let rec promptYesNo msg =
        match prompt (sprintf "%s [Yn]: " msg) with
        | Some "Y" | Some "y" -> true
        | Some "N" | Some "n" -> false
        | _ -> System.Console.WriteLine("Sorry, invalid answer"); promptYesNo msg

    let releaseMsg = """This will stage all uncommitted changes, push them to the origin and bump the release version to the latest number in the RELEASE_NOTES.md file. 
        Do you want to continue?"""

    let releaseDocsMsg = """This will push the docs to gh-pages. Remember building the docs prior to this. Do you want to continue?"""

let createProcess exe arg dir =
    CreateProcess.fromRawCommandLine exe arg
    |> CreateProcess.withWorkingDirectory dir
    |> CreateProcess.ensureExitCode

let dotnet = createProcess "dotnet"

let docker = createProcess "docker"
    
let dockerCompose = createProcess "docker-compose"

let run proc arg dir =
    proc arg dir
    |> Proc.run
    |> ignore

let runParallel processes =
    processes
    |> Proc.Parallel.run
    |> ignore

let runOrDefault args =
    Trace.trace (sprintf "%A" args)
    try
        match args with
        | [| target |] -> Target.runOrDefault target
        | arr when args.Length > 1 ->
            Target.run 0 (Array.head arr) ( Array.tail arr |> List.ofArray )
        | _ -> Target.runOrDefault "Build" 
        0
    with e ->
        printfn "%A" e
        1
