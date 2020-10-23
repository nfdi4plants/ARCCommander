# ArcCommander

Tool to manage your ARCs

## Future functionalities of the ArcCommander

### Init and create structure
`initARC` : Creates basic ARC folder structure
`addAssay` : Creates assay sub-folder structure and registers assay and study name in isa.investigation.xlxs at the root level.
`addWorkflow` : Creates workflow sub-folder structure
`initCodeCapsule` : Initialize a code capsule of currently supported scripting languages (python, F#, R!)

### Update investigation (isa.investigation.xlxs)
`setTitel` : Sets the iIvestigation Titel
`getTitel` : Gets the Investigation Titel
`setIdentifier` : Sets the Investigation Identifier
`addContact` : Registers an investigation contact

### Sharing and versioning
`commit` : Saves the changes made on the ARC to the local repository

