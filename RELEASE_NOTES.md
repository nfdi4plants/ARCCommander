### 0.2.2+1d186e3 (Released 2022-3-9)
* Additions:
    * latest commit #1d186e3
    * [[#16a3a60](https://github.com/nfdi4plants/arcCommander/commit/16a3a60eaa1db000630bf9d3c66199f9616f6af4)] add first draft of arc gitignore
    * [[#7471b0a](https://github.com/nfdi4plants/arcCommander/commit/7471b0a1f7832a97d8d346bfee63d53d1036ff5a)] Add .gitignore to copy when compiling
    * [[#7c41cd3](https://github.com/nfdi4plants/arcCommander/commit/7c41cd39ea4601d6108d49e6e3f45d44124c5bbc)] Add .gitignore copying functionality to `arc init` :sparkles:
    * [[#6b74ff3](https://github.com/nfdi4plants/arcCommander/commit/6b74ff34f8c3656c2334c59bdce424f543676f73)] Add functions to initiate README.mds in each folder
    * [[#88a0088](https://github.com/nfdi4plants/arcCommander/commit/88a00888a6f98389605e95b10937b5235c0f2276)] Raise error message verbosity
    * [[#37e38b6](https://github.com/nfdi4plants/arcCommander/commit/37e38b63cc3539ae50c045b9617e99149991afad)] add github release action
    * [[#62c32cd](https://github.com/nfdi4plants/arcCommander/commit/62c32cd5ecdc512c63ee419abbbc724116c8341d)] further test github release action
    * [[#2a4504e](https://github.com/nfdi4plants/arcCommander/commit/2a4504ea83daf79bafee90217f50f3df75e01530)] finish up first draft of github-release workflow
    * [[#4717886](https://github.com/nfdi4plants/arcCommander/commit/471788679934d038a8546025acb1a099267d9b16)] add github-release tag update when using releasenotes updating
    * [[#eeab473](https://github.com/nfdi4plants/arcCommander/commit/eeab473ce95e9c1d18243d0a82cf2454317ee98d)] update macos installation in readme
    * [[#5d1df31](https://github.com/nfdi4plants/arcCommander/commit/5d1df31ee8455356982413d6419421f1a112c09c)] Update ReadMe.md by adding github-release information
    * [[#c536ed5](https://github.com/nfdi4plants/arcCommander/commit/c536ed53ff9d7a63f908565aaef899c6185809e2)] make .gitignore optional in arc init
    * [[#d249629](https://github.com/nfdi4plants/arcCommander/commit/d249629d52659b24b1e8b441803bd7fbc00b23f4)] replace empty Readme.md with .gitkeep files
    * [[#d91cb60](https://github.com/nfdi4plants/arcCommander/commit/d91cb60b6d2aededdb539900bdec8d0f150ca823)] start working on automatically pushing arcs to new remotes
    * [[#f565f8f](https://github.com/nfdi4plants/arcCommander/commit/f565f8f0573b4f4ce48679f480aa1509e1651f23)] add automatic repo creation when pushing arc to gitlab
    * [[#0d1d657](https://github.com/nfdi4plants/arcCommander/commit/0d1d6573ddf5c0526ea3827a71e21ecb1092d292)] add check whether remote repo is a github repository
* Bugfixes:
    * [[#ceeea2e](https://github.com/nfdi4plants/arcCommander/commit/ceeea2e92dafc0ac5ff5d56579044f78456b8ff1)] fix assay tests
    * [[#bf3ab9e](https://github.com/nfdi4plants/arcCommander/commit/bf3ab9e56fd2bcf11f632cefa25f0f93f76a655c)] fix github-release unix file endings
    * [[#2853ec0](https://github.com/nfdi4plants/arcCommander/commit/2853ec073b3771088eb7ba5fedc73b2a81334948)] fix github-release workflow for mac

### 0.2.1+bf09a03 (Released 2022-2-25)
* Additions:
    * latest commit #bf09a03
* Bugfixes:
    * [[#bf09a03](https://github.com/nfdi4plants/arcCommander/commit/bf09a03d642f92f7867ccb386aaa8da80ae0d16a)] fix duplicate argument naming in export functions

### 0.2.0+5c6340d (Released 2022-2-25)
* Additions:
    * latest commit #5c6340d
    * [[#762e9be](https://github.com/nfdi4plants/arcCommander/commit/762e9be09a6801d920a870cf7c60638d9fc98587)] homogenize logging in authentication functions
    * [[#d06a98e](https://github.com/nfdi4plants/arcCommander/commit/d06a98ef22a1094b8590ede3b10b4e64f2c956af)] Update README.md :pencil:
    * [[#fb88d2f](https://github.com/nfdi4plants/arcCommander/commit/fb88d2f88d00d5ed1c57ab27380a4e1b82dfe96c)] Update build script to work with arguments :construction_worker:
    * [[#0f441f8](https://github.com/nfdi4plants/arcCommander/commit/0f441f89970c1d24e462002d1eb20a46ee960028)] move authentication functions to distinct subcommand tree
    * [[#8c18631](https://github.com/nfdi4plants/arcCommander/commit/8c18631aee612a17eacc6bbba2d689e0b9ba6a5e)] make logging ignore all git trace lines
    * [[#0c4f946](https://github.com/nfdi4plants/arcCommander/commit/0c4f9462660dc2db73d56188a2c3045668ad531b)] add git user metadata check to arc sync
    * [[#a5df1e3](https://github.com/nfdi4plants/arcCommander/commit/a5df1e30b5f266337349f49d74d70c61951da2e5)] add setgituser command
    * [[#c9af0ef](https://github.com/nfdi4plants/arcCommander/commit/c9af0ef2932285fe4da86c13b91f6f526d2fbe90)] add authentication command
    * [[#b895182](https://github.com/nfdi4plants/arcCommander/commit/b8951827a85cf17170a5eb3bf7902aa4edb4673e)] add logging to access token retrieval
    * [[#bcd5af8](https://github.com/nfdi4plants/arcCommander/commit/bcd5af8d56f6a7e9fba64084cee1b16cda163e55)] add token service authentication to arc get
    * [[#5caee62](https://github.com/nfdi4plants/arcCommander/commit/5caee62709204fd69cf30e452d8a12843d3cf5a3)] add configuration options for authentication
    * [[#fc5e2cd](https://github.com/nfdi4plants/arcCommander/commit/fc5e2cd36ca0c233b8141b19a5ce27a25c9afbe4)] add first draft of keycloak login
* Bugfixes:
    * [[#59f3758](https://github.com/nfdi4plants/arcCommander/commit/59f37581ac961d7ec0ad6ea8e38148c05c9a7df8)] hotfix git message handling

### 0.1.6+f588270 (Released 2022-2-25)
* Additions:
    * latest commit #f588270
    * [[#f588270](https://github.com/nfdi4plants/arcCommander/commit/f5882706185fa68158ddebae631fdc525e3d60ae)] Merge pull request #108 from nfdi4plants/loggerChanges
    * [[#941bd5c](https://github.com/nfdi4plants/arcCommander/commit/941bd5ca3cf01492393dc8e277d69ab51809dbca)] Rework folder getting function :hammer::construction:
    * [[#c51dd01](https://github.com/nfdi4plants/arcCommander/commit/c51dd016517b804bbf0ed0875db22aa8b343b2db)] Move log file to $XDG_CONFIG_DIRS
    * [[#53f82b3](https://github.com/nfdi4plants/arcCommander/commit/53f82b39000c654bd7e02d811dc978de640dea9e)] Update logging rules
    * [[#bb7635e](https://github.com/nfdi4plants/arcCommander/commit/bb7635e5170c1a0abf2a360ad6189664a75d41bb)] Merge pull request #101 from nfdi4plants/versionFR
    * [[#93b60f2](https://github.com/nfdi4plants/arcCommander/commit/93b60f2fb1ac3faacb0d15bf29f5b006ad2bc543)] Update build-and-test.yml
    * [[#fdf209a](https://github.com/nfdi4plants/arcCommander/commit/fdf209a8b72fd19a5d80cee6f00e20602a207d4a)] Update build-and-test.yml
    * [[#cf144c6](https://github.com/nfdi4plants/arcCommander/commit/cf144c6012e2fbd8a288598e9236fa9b4cc5a383)] Bump to .NET 6
* Deletions:
    * [[#881511e](https://github.com/nfdi4plants/arcCommander/commit/881511e9184ab3340db9facd0500053004f7cef3)] Remove log level prints :zap:
* Bugfixes:
    * [[#bd8a317](https://github.com/nfdi4plants/arcCommander/commit/bd8a317b947148ebe487fec96ba8d868aa62908d)] Fix bugs :bug:

### 0.1.5+6ad8fb8 (Released 2022-2-4)
* Additions:
    * latest commit #6ad8fb8
    * [[#6ad8fb8](https://github.com/nfdi4plants/arcCommander/commit/6ad8fb88904fbb32d5b56621f7f5e01c3c83433e)] Update build script for release notes extension :heavy_plus_sign:
    * [[#34be86f](https://github.com/nfdi4plants/arcCommander/commit/34be86f0b788493e69d77f21a09837bb62303a37)] Bump Fake version
    * [[#38c86be](https://github.com/nfdi4plants/arcCommander/commit/38c86be8a0ea9048993cfe91d5691ec22a498141)] Add version calling commands
    * [[#b1e87f5](https://github.com/nfdi4plants/arcCommander/commit/b1e87f5454cb7d7c216faa4ea516506ee17007e7)] Add version calling functionality :sparkles:
    * [[#e11dbcf](https://github.com/nfdi4plants/arcCommander/commit/e11dbcf8a542bf1190fe75f5ecbc2c34191a6d59)] Update build script to include version
* Deletions:
    * [[#91a3dee](https://github.com/nfdi4plants/arcCommander/commit/91a3dee82367ed06152c5c7833265579873367f8)] Delete deprecated assembly source file :fire:

#### 0.1.4 - Wednesday, January 26, 2022
* Additions:
    * latest commit #8f34df30
    * ArcCommander is now able to call external tools. :sparkles:

#### 0.1.3 - Tuesday, January 11, 2022

- Person functions for assay commands now available.
- `arc i show` now available.
- ArcCommander now logs every action.

#### 0.1.2 - Tuesday, November 2, 2021

- Publish tasks now available in build script.
- CLI scripts for ARC templates.
- Unit tests for `person update`.
- Raised .NET 5.0 target.
- Now it is possible to convert an ARC to a JSON object via JSON export.
- Global config file now gets created automatically (with default parameters).

#### 0.1.1 - Thursday, May 20, 2021

- Configurable Git LFS threshold.
- Issue templates available now.

#### 0.1.0 - Wednesday, April 28, 2021

- First release! :tada:
- New synchronize command.
- New Git commands and API.
- Now packed as dotnet tool.

#### 0.0.7-alpha - Thursday, February 11, 2021, 

- Unit tests for Investigation, Assay, Study.
- Updated build chain.
- ISA-XLSX moved to ISA.NET: https://github.com/nfdi4plants/ISADotNet

#### 0.0.6-alpha - Tuesday, January 19, 2021

- Removed FSharpSpreadsheetML to its own repository: https://github.com/CSBiology/FSharpSpreadsheetML

#### 0.0.5-alpha - Wednesday, January 6, 2021

- Global and local config file and configuration backbone added.
- SpreadsheetML refactored.
- ISA-XLSX and ISA-XLSX.IO unit tests added.

#### 0.0.4-alpha - Monday, November 9, 2020

- Improved CLIArgs.
- Assay subcommands.
- Investigation subcommands.
- Sheet functions added.

#### 0.0.3-alpha - Wednesday, October 21, 2020

- Added ISA-XLSX project.

#### 0.0.2-alpha - Monday, October 12, 2020

- Added ArcCommander project.

#### 0.0.1-alpha - Saturday, September 5, 2020

**Created repository.**
