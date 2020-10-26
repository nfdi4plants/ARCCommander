# ArcCommander

Tool to manage your ARCs

## Future functionalities of the ArcCommander

### Init and create structure
* `initARC` : Creates basic ARC folder structure
* `addAssay` : Creates assay sub-folder structure and registers assay and study name in isa.investigation.xlxs at the root level.
* `addWorkflow` : Creates workflow sub-folder structure
* `initCodeCapsule` : Initialize a code capsule of currently supported scripting languages (python, F#, R!)

### Update investigation (isa.investigation.xlxs)
* `setTitel` : Sets the iIvestigation Titel
* `getTitel` : Gets the Investigation Titel
* `setIdentifier` : Sets the Investigation Identifier
* `getIdentifier` : Gets the Investigation Identifier
* `setDescription` : Sets the Investigation Description
* `getDescription` : Gets the Investigation Description

* `setSubmissionDate`  : Sets the Investigation Submission Date
* `getSubmissionDate`  : Gets the Investigation Submission Date
* `setPublicReleaseDate` : Sets the Investigation Public Release Date
* `getPublicReleaseDate` : Gets the Investigation Public Release Date

* `addOntologyReference` : Registers an ontology term source 
* `removeOntologyReference` : Unregisters a ontology term source 


* `addContact` : Registers an investigation contact
* `removeContact` : Unregisters an investigation contact

* `moveAssay` : Moves an assay into a study

* `addStudy` : Add a study 
* `updateStudyByIdentifier` : Update given field of a study identified by study Identifier
* `updateStudyByTitle` : Update given field of a study identified by study title
* `removeStudy` : Remove a study



### Sharing and versioning
* `commit` : Saves the changes made on the ARC to the local repository

