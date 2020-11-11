# ArcCommander API

Files in this folder model the API calls that are executed by a command or object subcommand.
The files contained here have the following naming convention:

`<object_or_top-level-command>API.fs`,

where `<object_or_top-level-command>` can be either of the object subcommands (e.g. `Assay`), or a top level command (the only one we have right now is `Arc`).

Following conventions should apply:

 - The API should contain all functions that correspond to the subcommands of the target command in lowercase, e.g. if the command has two subcommands, the API must contain two functions with the same name in lowercase.
 - Add all API functions from the start as stubs raising `NotImplementedExceptions` and populate the function bodies afterwards.