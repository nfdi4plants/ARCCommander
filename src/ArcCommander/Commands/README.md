# ArcCommander commands

Files in this folder model both the top level command and the object subcommand verbs
The files contained here have the following naming convention:

`<object_or_top-level-command>Command.fs`,

where `<object_or_top-level-command>` can be either of the object subcommand (e.g. `Assay`), or a top level command (the only one we have right now is `Arc`).

In the case of modelling object subcommand verbs, model types like this:

```F#
type <object>Command =
| <verb1> of <verb1>_args: ParseResults<<object><verb1>Args>
| <verb2> of <verb2>_args: ParseResults<<object><verb2>Args>
```

example: 
```F#
type AssayCommand = 
    | [<CliPrefix(CliPrefix.None)>] Init of init_args:  ParseResults<AssayInitArgs>
```

In the case of adding an object subcommand to the top level type `ArcCommand`:

```F#
type ArcCommand =
| <object> of verb_and_args: ParseResults<<object>Command>
```


```F#
type Arc =
    ...
    | [<AltCommandLine("i")>][<CliPrefix(CliPrefix.None)>] Investigation of verb_and_args:ParseResults<InvestigationCommand>
    ...
```
