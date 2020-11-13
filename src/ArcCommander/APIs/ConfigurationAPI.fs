namespace ArcCommander.APIs

open System

open ArcCommander

/// ArcCommander Configuration API functions that get executed by the configuration focused subcommand verbs
module ConfigurationAPI =     
    
    /// [Not Implemented] Opens the configuration file specified with (global or local) with the text editor set in globalArgs.
    let edit (arcConfiguration:ArcConfiguration) = raise (NotImplementedException())

    /// [Not Implemented] Lists all current settings specified in the configuration
    let list (arcConfiguration:ArcConfiguration) = raise (NotImplementedException())
    