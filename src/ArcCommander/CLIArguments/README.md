# ArcCommander CLI arguments

Files in this folder model the command line arguments of both the top level and the object subcommand verbs
The files contained here have the following naming convention:

`<object_or_top-level-command>Args.fs`,

where `<object_or_top-level-command>` can be either of the object subcommands (e.g. `Assay`), or a top level command (the only one we have right now is `Arc`).

In the case of modelling object subcommand verb arguments, model types like this:

```F#
type <object><verb>Args = ...
```

example: `AssayUpdateArgs` models the CLI arguments for the `assay` subcommand verb `update`

the same counts for toplevel commands : `ArcInitArgs`.