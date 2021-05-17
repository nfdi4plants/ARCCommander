# ArcCommander

ArcCommander is a command line tool to create, manage and share your ARCs. 

## Install and start

Head over to [Releases](https://github.com/nfdi4plants/arcCommander/releases). Download the newest release for the OS you use. Extract the .zip into a folder of your choice.  
Start the arcCommander with the respective OS's command-line shell.

We strongly recommend to read the in-depth guide to the ArcCommander in this repository's [Wiki](https://github.com/nfdi4plants/arcCommander/wiki)!

## Develop

The following part addresses all who want to contribute to the ArcCommander.

### Prerequisites

- .NET SDK >= 3.1.00 (should roll forward to .net 5 if you are using that)

### Build

Check the [build.fsx file](https://github.com/nfdi4plants/arcCommander/blob/developer/build.fsx) to take a look at the  build targets. Here are some examples:

#### via dotnet cli

- run `dotnet tool restore` once to restore local tools needed in the buildchain

- run `dotnet fake build -t <YourBuildTargetHere>` to run the buildchain of `<YourBuildTargetHere>`

    Examples:

    - `dotnet fake build` run the default buildchain (clean artifacts, build projects, copy binaries to /bin)

    - `dotnet fake build -t runTests` (clean artifacts, build projects, copy binaries to /bin, run unit tests)

#### using the shell scripts

```shell
# Windows

# Build only
./build.cmd

# Full release buildchain: build, test, pack, build the docs, push a git tag, publsih the nuget package, release the docs
./build.cmd -t release

# The same for prerelease versions:
./build.cmd -t prerelease


# Linux/mac

# Build only
build.sh

# Full release buildchain: build, test, pack, build the docs, push a git tag, publsih the nuget package, release the docs
build.sh -t release

# The same for prerelease versions:
build.sh -t prerelease

```

#### testing the binary

After running the default build target, binaries of the arcCommander tool will lie in ./bin/ArcCommander. To run the binary, either use the `ArcCommander.exe` file on windows or `dotnet ArcCommander.dll` on linux.