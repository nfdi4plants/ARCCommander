# ArcCommander

ArcCommander is a command line tool to create, manage and share your ARCs. 

The general command line structure is designed as either

```powershell
arc <top-level-command> <top-level-command-args>
```

e.g.

```powershell
arc init
```

or

```powershell
arc <object> <subcommand-verb> <subcommand-verb-args>
```

where `<object>` is one of the following:

 - `assay` 
 - `study` 
 - `investigation`
 - `configuration`

and `<subcommand-verb>` models what to do with the object, e.g

```powershell
arc study init
```

will initialize a new empty study in the ARC.

### Subcommand verbs

while not all object subcommands support all verbs, here is a list of the verbs and what to expect when they are applicable for an `<object>`:

- arc `<object>` **init** : will initialize a new empty `<object>` in the ARC.
- arc `<object>` **create** will create a new `<object>` with the passed arguments in the ARC.
- arc `<object>` **update** will update an existing `<object>` with the passed arguments.
- arc `<object>` **edit** will open an existing `<object>` with a text editor. Arguments passed for this verb will be pre-set in the editor.
- arc `<object>` **register** will register an existing `<object>` in the ARC with the passed arguments.
- arc `<object>` **add** is the combination of create/init+update and register, meaning it will create a new `<object>` in the ARC and subsequently register it with the passed arguments
- arc `<object>` **remove** will remove the `<object>` from the ARC's register.
- arc `<object>` **move** will move the `<object>` from source to target register.
- arc `<object>` **list** will print all `<object>s` registered in the ARC.
- arc `<object>` **delete** will delete the `<object>`.
- arc `<object>` **set** will set a single value in the `<object>`.
- arc `<object>` **unset** will remove a single value from the `<object>`.

### CLI argument help

Here is the current help dump from the command line tool:

#### Top level:

```powershell
USAGE: arc [--help] [--workingdir <working directory>] [--verbosity <verbosity>] [<subcommand> [<options>]]

SUBCOMMANDS:

    init <init args>      Initializes basic folder structure
    investigation, i <verb and args>
                          Investigation file functions
    study, s <verb and args>
                          Study functions
    assay, a <verb and args>
                          Assay functions
    configuration, config <verb and args>
                          Configuration editing

    Use 'arc <subcommand> --help' for additional information.

OPTIONS:

    --workingdir, -p <working directory>
                          Set the base directory of your ARC
    --verbosity, -v <verbosity>
                          Sets the amount of additional printed information: 0->No information, 1 (Default) -> Basic
                          Information, 2 -> Additional information
    --help                display this list of options.
```

<br>

#### investigation:

```powershell
USAGE: arc investigation [--help] [<subcommand> [<options>]]

SUBCOMMANDS:

    create <create args>  Create a new investigation with the given metadata
    update <update args>  Update the arc's investigation with the given metdadata
    edit <edit args>      Open an editor window to directly edit the arc's investigation file
    delete <delete args>  Delete the arc's investigation file (danger zone!)

    Use 'arc <subcommand> --help' for additional information.

OPTIONS:

    --help                display this list of options.
```

##### investigation create

```powershell
USAGE: arc investigation create [--help] --identifier <investigation identifier> [--title <title>] [--description <description>] [--submissiondate <submission date>]
                                [--publicreleasedate <public release date>]

OPTIONS:

    --identifier <investigation identifier>
                          Identifier of the investigation
    --title <title>       Title of the investigation
    --description <description>
                          Description of the investigation
    --submissiondate <submission date>
                          Submission Date of the investigation
    --publicreleasedate <public release date>
                          Public Release Date of the investigation
    --help                display this list of options.
```

##### investigation update

```powershell
USAGE: arc investigation update [--help] --identifier <investigation identifier> [--title <title>] [--description <description>] [--submissiondate <submission date>]
                                [--publicreleasedate <public release date>]

OPTIONS:

    --identifier <investigation identifier>
                          Identifier of the investigation
    --title <title>       Title of the investigation
    --description <description>
                          Description of the investigation
    --submissiondate <submission date>
                          Submission Date of the investigation
    --publicreleasedate <public release date>
                          Public Release Date of the investigation
    --help                display this list of options.
```

##### investigation edit

```powershell
USAGE: arc investigation edit [--help] --identifier <investigation identifier> [--title <title>] [--description <description>] [--submissiondate <submission date>] [--publicreleasedate <public release date>]

OPTIONS:

    --identifier <investigation identifier>
                          Identifier of the investigation
    --title <title>       Title of the investigation
    --description <description>
                          Description of the investigation
    --submissiondate <submission date>
                          Submission Date of the investigation
    --publicreleasedate <public release date>
                          Public Release Date of the investigation
    --help                display this list of options.
```

##### investigation delete

```powershell
USAGE: arc investigation delete [--help] --identifier <investigation identifier>

OPTIONS:

    --identifier <investigation identifier>
                          Identifier of the investigation
    --help                display this list of options.
```

<br>

#### study:

```powershell
USAGE: arc study [--help] [<subcommand> [<options>]]

SUBCOMMANDS:

    init <init args>      Initialize a new empty study file in the arc
    update <update args>  Update an existing study in the arc with the given study metadata
    edit <edit args>      Open and edit an existing study in the arc with a text editor. Arguments passed for this command will be pre-set in the editor.
    register <register args>
                          Register an existing study in the arc with the given assay metadata.
    add <add args>        Create a new study file in the arc and subsequently register it with the given study metadata
    remove <remove args>  Remove a study from the arc
    list                  List all studies registered in the arc

    Use 'arc <subcommand> --help' for additional information.

OPTIONS:

    --help                display this list of options.
```

##### study init

```powershell
USAGE: arc study init [--help] --identifier <study identifier>

OPTIONS:

    --identifier <study identifier>
                          Identifier of the study, will be used as the file name of the study file
    --help                display this list of options.
```

##### study update

```powershell
USAGE: arc study update [--help] --identifier <study identifier> [--title <title>] [--description <description>] [--submissiondate <submission date>] [--publicreleasedate <public release date>]

OPTIONS:

    --identifier <study identifier>
                          Identifier of the study
    --title <title>       Title of the study
    --description <description>
                          Description of the study
    --submissiondate <submission date>
                          Submission Date of the study
    --publicreleasedate <public release date>
                          Public Release Date of the study
    --help                display this list of options.
```

##### study edit

```powershell
USAGE: arc study edit [--help] --identifier <study identifier> [--title <title>] [--description <description>] [--submissiondate <submission date>] [--publicreleasedate <public release date>]

OPTIONS:

    --identifier <study identifier>
                          Identifier of the study
    --title <title>       Title of the study
    --description <description>
                          Description of the study
    --submissiondate <submission date>
                          Submission Date of the study
    --publicreleasedate <public release date>
                          Public Release Date of the study
    --help                display this list of options.
```

##### study register

```powershell
USAGE: arc study register [--help] --identifier <study identifier> [--title <title>] [--description <description>] [--submissiondate <submission date>] [--publicreleasedate <public release date>]

OPTIONS:

    --identifier <study identifier>
                          Identifier of the study
    --title <title>       Title of the study
    --description <description>
                          Description of the study
    --submissiondate <submission date>
                          Submission Date of the study
    --publicreleasedate <public release date>
                          Public Release Date of the study
    --help                display this list of options.
```

##### study add

```powershell
USAGE: arc study add [--help] --identifier <study identifier> [--title <title>] [--description <description>] [--submissiondate <submission date>] [--publicreleasedate <public release date>]

OPTIONS:

    --identifier <study identifier>
                          Identifier of the study
    --title <title>       Title of the study
    --description <description>
                          Description of the study
    --submissiondate <submission date>
                          Submission Date of the study
    --publicreleasedate <public release date>
                          Public Release Date of the study
    --help                display this list of options.
```

##### study remove

```powershell
USAGE: arc study remove [--help] --identifier <study identifier>

OPTIONS:

    --identifier <study identifier>
                          Identifier of the study, will be used as the file name of the study file
    --help                display this list of options.
```

<br>

#### assay:

```powershell
USAGE: arc assay [--help] [<subcommand> [<options>]]

SUBCOMMANDS:

    init <init args>      Initialize a new empty assay and associated folder structure in the arc.
    update <update args>  Update an existing assay in the arc with the given assay metadata
    edit <edit args>      Open and edit an existing assay in the arc with a text editor. Arguments passed for this command will be pre-set in the editor.
    register <register args>
                          Register an existing assay in the arc with the given assay metadata.
    add <add args>        Create a new assay file and associated folder structure in the arc and subsequently register it with the given assay metadata
    remove <remove args>  Remove an assay from the given studys' assay register
    move <move args>      Move an assay from one study to another
    list                  List all assays registered in the arc

    Use 'arc <subcommand> --help' for additional information.

OPTIONS:

    --help                display this list of options.
```

##### assay init

```powershell
USAGE: arc assay init [--help] --assayidentifier <assay identifier>

OPTIONS:

    --assayidentifier, -a <assay identifier>
                          identifier of the assay, will be used as name of the root folder of the new assay folder structure
    --help                display this list of options.
```

##### assay update

```powershell
USAGE: arc assay update [--help] --studyidentifier <string> --assayidentifier <string> [--measurementtype <measurement type>] [--measurementtypetermaccessionnumber <measurement type accession>]
                        [--measurementtypetermsourceref <measurement type term source>] [--technologytype <technology type>] [--technologytypetermaccessionnumber <technology type accession>]
                        [--technologytypetermsourceref <technology type term source>] [--technologyplatform <technology platform>]

OPTIONS:

    --studyidentifier, -s <string>
                          Name of the study in which the assay is situated
    --assayidentifier, -a <string>
                          Name of the assay of interest
    --measurementtype <measurement type>
                          Measurement type of the assay
    --measurementtypetermaccessionnumber <measurement type accession>
                          Measurement type Term Accession Number of the assay
    --measurementtypetermsourceref <measurement type term source>
                          Measurement type Term Source REF of the assay
    --technologytype <technology type>
                          Technology Type of the assay
    --technologytypetermaccessionnumber <technology type accession>
                          Technology Type Term Accession Number of the assay
    --technologytypetermsourceref <technology type term source>
                          Technology Type Term Source REF of the assay
    --technologyplatform <technology platform>
                          Technology Platform of the assay
    --help                display this list of options.
```

##### assay edit

```powershell
USAGE: arc assay edit [--help] --studyidentifier <string> --assayidentifier <string> [--measurementtype <measurement type>] [--measurementtypetermaccessionnumber <measurement type accession>]
                      [--measurementtypetermsourceref <measurement type term source>] [--technologytype <technology type>] [--technologytypetermaccessionnumber <technology type accession>]
                      [--technologytypetermsourceref <technology type term source>] [--technologyplatform <technology platform>]

OPTIONS:

    --studyidentifier, -s <string>
                          Name of the study in which the assay is situated
    --assayidentifier, -a <string>
                          Name of the assay of interest
    --measurementtype <measurement type>
                          Measurement type of the assay
    --measurementtypetermaccessionnumber <measurement type accession>
                          Measurement type Term Accession Number of the assay
    --measurementtypetermsourceref <measurement type term source>
                          Measurement type Term Source REF of the assay
    --technologytype <technology type>
                          Technology Type of the assay
    --technologytypetermaccessionnumber <technology type accession>
                          Technology Type Term Accession Number of the assay
    --technologytypetermsourceref <technology type term source>
                          Technology Type Term Source REF of the assay
    --technologyplatform <technology platform>
                          Technology Platform of the assay
    --help                display this list of options.
```

##### assay register

```powershell
USAGE: arc assay register [--help] --studyidentifier <string> --assayidentifier <string> [--measurementtype <measurement type>] [--measurementtypetermaccessionnumber <measurement type accession>]
                          [--measurementtypetermsourceref <measurement type term source>] [--technologytype <technology type>] [--technologytypetermaccessionnumber <technology type accession>]
                          [--technologytypetermsourceref <technology type term source>] [--technologyplatform <technology platform>]

OPTIONS:

    --studyidentifier, -s <string>
                          Name of the study in which the assay is situated
    --assayidentifier, -a <string>
                          Name of the assay of interest
    --measurementtype <measurement type>
                          Measurement type of the assay
    --measurementtypetermaccessionnumber <measurement type accession>
                          Measurement type Term Accession Number of the assay
    --measurementtypetermsourceref <measurement type term source>
                          Measurement type Term Source REF of the assay
    --technologytype <technology type>
                          Technology Type of the assay
    --technologytypetermaccessionnumber <technology type accession>
                          Technology Type Term Accession Number of the assay
    --technologytypetermsourceref <technology type term source>
                          Technology Type Term Source REF of the assay
    --technologyplatform <technology platform>
                          Technology Platform of the assay
    --help                display this list of options.
```

##### assay add

```powershell
USAGE: arc assay add [--help] --studyidentifier <string> --assayidentifier <string> [--measurementtype <measurement type>] [--measurementtypetermaccessionnumber <measurement type accession>]
                     [--measurementtypetermsourceref <measurement type term source>] [--technologytype <technology type>] [--technologytypetermaccessionnumber <technology type accession>]
                     [--technologytypetermsourceref <technology type term source>] [--technologyplatform <technology platform>]

OPTIONS:

    --studyidentifier, -s <string>
                          Name of the study in which the assay is situated
    --assayidentifier, -a <string>
                          Name of the assay of interest
    --measurementtype <measurement type>
                          Measurement type of the assay
    --measurementtypetermaccessionnumber <measurement type accession>
                          Measurement type Term Accession Number of the assay
    --measurementtypetermsourceref <measurement type term source>
                          Measurement type Term Source REF of the assay
    --technologytype <technology type>
                          Technology Type of the assay
    --technologytypetermaccessionnumber <technology type accession>
                          Technology Type Term Accession Number of the assay
    --technologytypetermsourceref <technology type term source>
                          Technology Type Term Source REF of the assay
    --technologyplatform <technology platform>
                          Technology Platform of the assay
    --help                display this list of options.
```

##### assay move

```powershell
USAGE: arc assay move [--help] --studyidentifier <study identifier> --assayidentifier <assay identifier> --targetstudyidentifier <target study identifier>

OPTIONS:

    --studyidentifier, -s <study identifier>
                          Name of the study in which the assay is situated
    --assayidentifier, -a <assay identifier>
                          Name of the assay of interest
    --targetstudyidentifier, -t <target study identifier>
                          Target study
    --help                display this list of options.
```

##### assay remove

```powershell
USAGE: arc assay remove [--help] --studyidentifier <study identifier> --assayidentifier <assay identifier>

OPTIONS:

    --studyidentifier, -s <study identifier>
                          identifier of the study in which the assay is registered
    --assayidentifier, -a <assay identifier>
                          identifier of the assay of interest
    --help                display this list of options.
```

<br>

#### configuration:

```powershell
USAGE: arc configuration [--help] [<subcommand> [<options>]]

SUBCOMMANDS:

    edit <edit args>      Open and edit an existing assay in the arc with a text editor. Arguments passed for this
                          command will be pre-set in the editor.
    list <list args>      List all assays registered in the arc.
    set <set args>        Assign the given value to the given name.
    unset <unset args>    Remove the value bound to the given name.

    Use 'arc <subcommand> --help' for additional information.

OPTIONS:

    --help                display this list of options.
```

##### configuration list

```powershell
USAGE: arc configuration list [--help] [--local] [--global]

OPTIONS:

    --local, -l           Lists the local settings for this arc
    --global, -g          Lists the global settings of the arccommander
    --help                display this list of options.
```

##### configuration edit

```powershell
USAGE: arc configuration edit [--help] [--local] [--global]

OPTIONS:

    --local, -l           Edit the local settings for this arc
    --global, -g          Edit the global settings of the arccommander
    --help                display this list of options.
```

##### configuration set

```powershell
USAGE: arc configuration set [--help] [--local] [--global] --name <string> --value <string>

OPTIONS:

    --local, -l           Set the the value of the name locally for this arc
    --global, -g          Set the the value of the name globally for the arccommander
    --name, -n <string>   The name of the setting in 'Section.Key' format
    --value, -v <string>  The new value of the setting
    --help                display this list of options. list of options.
```

##### configuration unset

```powershell
USAGE: arc configuration unset [--help] [--local] [--global] --name <string>

OPTIONS:

    --local, -l           Unset the the value of the name locally for this arc
    --global, -g          Unset the the value of the name globally for the arccommander
    --name, -n <string>   The name of the setting in 'Section.Key' format
    --help                display this list of options.
```