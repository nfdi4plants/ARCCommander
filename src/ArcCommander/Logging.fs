namespace ArcCommander

open NLog
open NLog.Config
open NLog.Targets
open NLog.Conditions

/// Functions for working with the NLog logger.
module Logging =

    /// Generates an NLog config with `folderPath` being the output folder for the log file.
    let generateConfig (folderPath : string) verbosity = 
        // initialize base configuration class, can be modified
        let config = new LoggingConfiguration()

        // initialise base console target, can be modified
        let consoleTarget = new ColoredConsoleTarget("console")
        // new parameters for console target
        let layoutConsole = new Layouts.SimpleLayout (@"${message} ${exception}")
        consoleTarget.Layout <- layoutConsole

        // initialise base file target, can be modified
        let fileTarget = new FileTarget("file")
        // new parameters for file target
        let fileName = new Layouts.SimpleLayout (System.IO.Path.Combine (folderPath, @"ArcCommander.log"))
        let layoutFile = new Layouts.SimpleLayout ("${longdate} ${logger} ${level:uppercase=true} ${message} ${exception}")
        fileTarget.FileName <- fileName
        fileTarget.Layout <- layoutFile

        config.AddTarget(consoleTarget)
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
        consoleTarget.RowHighlightingRules.Add(warnColorRule)
        consoleTarget.RowHighlightingRules.Add(errorColorRule)
        consoleTarget.RowHighlightingRules.Add(fatalColorRule)

        // declare which results in a log in which target
        if verbosity >= 1 then config.AddRuleForOneLevel(LogLevel.Info, consoleTarget) // info results shall be used for verbosity 1
        config.AddRuleForOneLevel(LogLevel.Info, fileTarget) // info results shall be written to log file, regardless of verbosity
        if verbosity = 2 then config.AddRuleForOneLevel(LogLevel.Trace, consoleTarget) // trace results shall be used for verbosity 2
        config.AddRuleForOneLevel(LogLevel.Trace, fileTarget) // trace results shall be written to log file, regardless of verbosity
        config.AddRuleForOneLevel(LogLevel.Debug, consoleTarget) // shall be used for results that shall always be printed, regardless of verbosity
        config.AddRuleForOneLevel(LogLevel.Debug, fileTarget)
        config.AddRuleForOneLevel(LogLevel.Warn, consoleTarget) // warnings shall be used for non-critical events
        config.AddRuleForOneLevel(LogLevel.Warn, fileTarget)
        config.AddRuleForOneLevel(LogLevel.Error, consoleTarget) // errors shall be used for critical events that lead to an abort of the desired task but still led the ArcCommander terminate successfully
        config.AddRuleForOneLevel(LogLevel.Error, fileTarget)
        config.AddRuleForOneLevel(LogLevel.Fatal, consoleTarget) // fatal errors shall be used for critical events that cause ArcCommander exceptions leading to an unsuccessful termination
        config.AddRuleForOneLevel(LogLevel.Fatal, fileTarget) // impairing the ARC structure
   
        // activate config for logger
        LogManager.Configuration <- config

    /// Creates a new logger with the given name. Configuration details are obtained from the generateConfig function.
    let createLogger (loggerName : string) = 

        // new instance of "Logger" with activated config
        let logger = LogManager.GetLogger(loggerName)

        logger