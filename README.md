# ArcCommander

ArcCommander is a command line tool to create, manage and share your ARCs. 

## Install and start

Head over to [Releases](https://github.com/nfdi4plants/arcCommander/releases). Download the newest release for the OS you use.  
Start the arcCommander with the respective OS's command-line shell.

We strongly recommend to read the in-depth guide to the ArcCommander in this repository's [Wiki](https://github.com/nfdi4plants/arcCommander/wiki)!

### Windows

1. In Windows Explorer, head over to the folder where you downloaded the ArcCommander, e.g.
![image](https://user-images.githubusercontent.com/47781170/118627514-13e63f00-b7cc-11eb-95cb-1bf74a355cde.png)
2. You can move the .exe to a desired folder, e.g. to your personal folder
3. Add the folder with the ArcCommander to your PATH:
    - Open the Start Menu, type in `path` and click on _Edit the system environment variables_
    ![image](https://user-images.githubusercontent.com/47781170/119674721-b8a3f480-be3c-11eb-9982-e3c0fa191f05.png)
    - Click on _Environment Variables..._, click on _Path_ and on _Edit..._ in the tab _User variables for <your username>_, click on _New_ and type in the full path to your folder as seen in the example below:
    ![image](https://user-images.githubusercontent.com/47781170/119674652-a9bd4200-be3c-11eb-81f8-72f1198842ef.png)
    - this allows you to start the ArcCommander from any folder
4. Navigate to a folder in which you want to initialize an ARC
5. Open the Command Prompt (CMD) via typing in `cmd` in the folder address, press Enter
![image](https://user-images.githubusercontent.com/47781170/119675448-58618280-be3d-11eb-9d6a-4e4e38b4caa5.png)
6. Run the ArcCommander from the CMD by executing `arc`

### Linux

1. Open the shell (Click on the Dash icon -> type in "Terminal" -> Click the Terminal application icon)
2. Change to the directory where you downloaded the ArcCommander, e.g. `cd home/~/Downloads`
3. Move the ArcCommander to a folder that fits your needs (this can be the directory of your ARC) via (examplarily) `mv arc home/~/ArcCommander/`
    - Use `sudo` if you don't have write access
4. Add this folder to your PATH: type in `export PATH=$PATH:~/ArcCommander`, this allows you to start the ArcCommander from any folder
5. Change to a folder in which you want to initialize an ARC
6. Run the ArcCommander by executing `arc`

### MacOS

1. Open a Terminal (Applications -> Utilities -> Terminal)
2. Change to the directory where you downloaded the ArcCommander, e.g. `cd ~/Downloads/`
3. Change permissions to make the ArcCommander executable: `chmod a+x arc`
4. Move the ArcCommander to a place you like, e.g. `mv arc ~/Applications/`
5. Add that place to your bash_profile to make it executable via terminal from everywhere, e.g. `echo 'export PATH="$HOME/Applications/:$PATH"' >> ~/.bash_profile`
6. Start a new terminal for this to take effect.
7. Run arcCommander from the terminal by executing `arc`

_MacOS security note:_  
On first execution, MacOS will not allow arc to be run. Instead it opens a pop-up: 
> "arc" cannot be opened because it is from an unidentified developer

Open the Security Panel in system Preferences (Applications -> System Preferences -> "Security & Privacy") and click the bottom-right button "Allow Anyway" right next to 
> arc was blocked from use because it is not from an identified developer. 

Head back to the terminal and execute `arc` again. Another pop-up will ask you to confirm by clicking "Open". 

---

## Develop

The following part addresses all who want to contribute to the ArcCommander.  
If you only want to simply use the ArcCommander, head over to [Install and start](https://github.com/nfdi4plants/arcCommander#install-and-start).

### Prerequisites

- .NET SDK >= 3.1.00 (should roll forward to .net 5 if you are using that)
- To test the generated cli tool you will ned the .NET 3.1 runtime.   

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
