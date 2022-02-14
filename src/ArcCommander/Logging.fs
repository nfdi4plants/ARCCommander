namespace ArcCommander

open System
open NLog
open NLog.Config
open NLog.Targets
open NLog.Conditions

// for testing purposes: commands to trigger different log levels: 
// - INFO & TRACE: `arc -v 2 init`
// - DEBUG: `arc -v 0 --version`
// - WARN: `arc a update --addifmissing` when an ARC was already initialized, an investigation was already created, a study with an id was already added, and an assay with another id than the study was already added. In the editor prompt, fill in the assay's id in the `AssayIdentifier` field and the first study's id in the `StudyIdentifier` field, save and close the prompt
// - ERROR: `arc asd`. Argu Usage prompt should not be logged (check log file)
// - FATAL: `arc a add`. Close the editor prompt without saving
/// Functions for working with the NLog logger.
module Logging =

    /// Generates an NLog config with `folderPath` being the output folder for the log file.
    let generateConfig (folderPath : string) verbosity = 
        // initialize base configuration class, can be modified
        let config = new LoggingConfiguration()

        // initialise base console target, can be modified
        let consoleTarget1 = new ColoredConsoleTarget("console")
        // new parameters for console target
        let layoutConsole1 = new Layouts.SimpleLayout (@"${message} ${exception}")
        consoleTarget1.Layout <- layoutConsole1

        // second console target for alert messages (error & fatal)
        let consoleTarget2 = new ColoredConsoleTarget("console")
        let layoutConsole2 = new Layouts.SimpleLayout (@"${level:uppercase=true}: ${message} ${exception}")
        consoleTarget2.Layout <- layoutConsole2

        // third console target for alert messages (warn)
        let consoleTarget3 = new ColoredConsoleTarget("console")
        let layoutConsole3 = new Layouts.SimpleLayout (@"WARNING: ${message} ${exception}")
        consoleTarget3.Layout <- layoutConsole3

        // initialise base file target, can be modified
        let fileTarget = new FileTarget("file")
        // new parameters for file target
        let fileName = new Layouts.SimpleLayout (System.IO.Path.Combine (folderPath, @"ArcCommander.log"))
        let layoutFile = new Layouts.SimpleLayout ("${longdate} ${logger} ${level:uppercase=true}: ${message}")
        fileTarget.FileName <- fileName
        fileTarget.Layout <- layoutFile

        config.AddTarget(consoleTarget1)
        config.AddTarget(fileTarget)

        // define rules for colors that shall differ from the default color theme
        let warnColorRule = new ConsoleRowHighlightingRule()
        warnColorRule.Condition <- ConditionParser.ParseExpression("level == LogLevel.Warn")
        warnColorRule.ForegroundColor <- ConsoleOutputColor.Yellow
        let errorColorRule = new ConsoleRowHighlightingRule()
        errorColorRule.Condition <- ConditionParser.ParseExpression("level == LogLevel.Error")
        errorColorRule.ForegroundColor <- ConsoleOutputColor.Red
        let fatalColorRule = new ConsoleRowHighlightingRule()
        fatalColorRule.Condition <- ConditionParser.ParseExpression("level == LogLevel.Fatal")
        fatalColorRule.ForegroundColor <- ConsoleOutputColor.Red
        fatalColorRule.BackgroundColor <- ConsoleOutputColor.DarkYellow

        // add the newly defined rules to the console target
        consoleTarget2.RowHighlightingRules.Add(errorColorRule)
        consoleTarget2.RowHighlightingRules.Add(fatalColorRule)
        consoleTarget3.RowHighlightingRules.Add(warnColorRule)

        // declare which results in a log in which target
        if verbosity >= 1 then config.AddRuleForOneLevel(LogLevel.Info, consoleTarget1) // info results shall be used for verbosity 1
        config.AddRuleForOneLevel(LogLevel.Info, fileTarget) // info results shall be written to log file, regardless of verbosity
        if verbosity = 2 then config.AddRuleForOneLevel(LogLevel.Trace, consoleTarget1) // trace results shall be used for verbosity 2
        config.AddRuleForOneLevel(LogLevel.Trace, fileTarget) // trace results shall be written to log file, regardless of verbosity
        config.AddRuleForOneLevel(LogLevel.Debug, consoleTarget1) // shall be used for results that shall always be printed, regardless of verbosity
        config.AddRuleForOneLevel(LogLevel.Debug, fileTarget)
        if verbosity >= 1 then config.AddRuleForOneLevel(LogLevel.Warn, consoleTarget3) // warnings shall be used for non-critical events if verbosity is above 0
        config.AddRuleForOneLevel(LogLevel.Warn, fileTarget)
        config.AddRuleForOneLevel(LogLevel.Error, consoleTarget2) // errors shall be used for critical events that lead to an abort of the desired task but still led the ArcCommander terminate successfully
        config.AddRuleForOneLevel(LogLevel.Error, fileTarget)
        config.AddRuleForOneLevel(LogLevel.Fatal, consoleTarget2) // fatal errors shall be used for critical events that cause ArcCommander exceptions leading to an unsuccessful termination
        config.AddRuleForOneLevel(LogLevel.Fatal, fileTarget) // impairing the ARC structure
   
        // activate config for logger
        LogManager.Configuration <- config

    /// Creates a new logger with the given name. Configuration details are obtained from the generateConfig function.
    let createLogger (loggerName : string) = 

        // new instance of "Logger" with activated config
        let logger = LogManager.GetLogger(loggerName)

        logger

    /// Takes a logger and an exception and separates usage and error messages. Usage messages will be printed into the console while error messages will be logged.
    let handleExceptionMessage (log : NLog.Logger) (exn : Exception) =
        // separate usage message (Argu) and error messages. Error messages shall be logged, usage messages shall not, empty error message shall not appear at all
        let isUsageMessage = exn.Message.Contains("USAGE") || exn.Message.Contains("SUBCOMMANDS")
        let isErrorMessage = exn.Message.Contains("ERROR")
        let isEmptyMessage = exn.Message = ""
        match isUsageMessage, isErrorMessage, isEmptyMessage with
        | true,true,false -> // exception message contains usage AND error messages
            let eMsg, uMsg = 
                exn.Message.Split(Environment.NewLine) // '\n' leads to parsing problems
                |> fun arr ->
                    arr |> Array.find (fun t -> t.Contains("ERROR")),
                    arr |> Array.filter (fun t -> t.Contains("ERROR") |> not) |> String.concat "\n" // Argu usage instruction shall not be logged as error
            log.Error(eMsg)
            printfn "%s" uMsg
        | true,false,false -> printfn "%s" exn.Message // exception message contains usage message but NO error message
        | false,false,true -> () // empty error message
        | _ -> log.Error(exn.Message) // everything else will be a non-empty error message
    
    /// Checks if a message (string) is empty and if it is not, applies a logging function to it.
    let checkNonLog s (logging : string -> unit) = if s <> "" then logging s
    
    /// Deletes unwanted new lines at the end of an output.
    let rec reviseOutput (output : string) = 
        if output = null then ""
        elif output.EndsWith('\n') then reviseOutput (output.[0 .. output.Length - 2])
        else output
    
    /// Checks if an error message coming from CMD not being able to call a program with the given name.
    let matchCmdErrMsg (errMsg : string) = errMsg.Contains("is not recognized as an internal or external command")
    
    /// Checks if an error message coming from Bash not being able to call a program with the given name.
    let matchBashErrMsg (errMsg : string) = errMsg.Contains("bash: ") && errMsg.Contains("command not found") || errMsg.Contains("No such file or directory")