module ProjectInfo

open Fake.Core
open Helpers
open Fake.IO

// run this here to make sure a RELEASE_NOTES.md exists, otherwise "let release = ReleaseNotes.load "RELEASE_NOTES.md" will fail.
Fake.Extensions.Release.ReleaseNotes.ensure()

let project = "ArcCommander"

let testProject = Path.getFullName "tests/ArcCommander.Tests.NetCore/ArcCommander.Tests.NetCore.fsproj"

let summary = "ArcCommander is a command line tool to create, manage and share your ARCs."

let solutionFile  = Path.getFullName "ArcCommander.sln"

let configuration = "Release"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "nfdi4plants"
let gitHome = sprintf "%s/%s" "https://github.com" gitOwner

let gitName = "arcCommander"

let website = "/arcCommander"

let pkgDir = Path.getFullName "pkg"

let publishDir = "publish"

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let projectRepo = "https://github.com/nfdi4plants/arcCommander"

let stableVersion = SemVer.parse release.NugetVersion

let stableVersionTag = (sprintf "%i.%i.%i" stableVersion.Major stableVersion.Minor stableVersion.Patch )

let mutable prereleaseSuffix = ""

let mutable prereleaseTag = ""

let mutable isPrerelease = false