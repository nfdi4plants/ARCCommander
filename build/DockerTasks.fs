module DockerTasks

open BlackFox.Fake
open Fake.Core
open Fake.IO
open Helpers

open Fake.Extensions.Release

let dockerImageName = "freymaurer/swate"
let dockerContainerName = "swate"

// Change target to github-packages
// https://docs.github.com/en/actions/publishing-packages/publishing-docker-images
let dockerPublish = BuildTask.create "docker-publish" [] {
    let releaseNotesPath = "RELEASE_NOTES.md"
    let port = "5000"

    ReleaseNotes.ensure()
    let newRelease = ReleaseNotes.load releaseNotesPath

    let dockerCreateImage() = run docker $"build -t {dockerContainerName} -f build/Dockerfile.publish . " ""
    let dockerTestImage() = run docker $"run -it -p {port}:{port} {dockerContainerName}" ""
    let dockerTagImage() =
        run docker $"tag {dockerContainerName}:latest {dockerImageName}:{newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch}" ""
        run docker $"tag {dockerContainerName}:latest {dockerImageName}:latest" ""
    let dockerPushImage() =
        run docker $"push {dockerImageName}:{newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch}" ""
        run docker $"push {dockerImageName}:latest" ""
    let dockerPublish() =
        Trace.trace $"Tagging image with :latest and :{newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch}"
        dockerTagImage()
        Trace.trace $"Pushing image to dockerhub with :latest and :{newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch}"
        dockerPushImage()
    // Check if next SemVer is correct
    if promptYesNo $"Is version {newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch} correct?" then
        Trace.trace "Perfect! Starting with docker publish"
        Trace.trace "Creating image"
        dockerCreateImage()
        // Check if user wants to test image
        if promptYesNo "Want to test the image?" then
            Trace.trace $"Your app on port {port} will open on localhost:{port}."
            dockerTestImage()
            // Check if user wants the image published
            if promptYesNo $"Is the image working as intended?" then
                dockerPublish()
            else
                Trace.traceErrorfn "Cancel docker-publish"
        else
            dockerPublish()
    else
        Trace.traceErrorfn "Please update your SemVer Version in %s" releaseNotesPath
}