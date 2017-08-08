#I @"tools/FAKE/tools"
#r "FakeLib.dll"

open System
open System.IO
open System.Text

open Fake
open Fake.DotNetCli
open Fake.Testing

// Variables
let configuration = "Release"

// Directories
let output = __SOURCE_DIRECTORY__  @@ "build"
let outputTests = output @@ "TestResults"
let outputBinaries = output @@ "binaries"
let outputNuGet = output @@ "nuget"
let outputBinariesNet45 = outputBinaries @@ "net45"
let outputBinariesNetStandard = outputBinaries @@ "netstandard1.0"

Target "Clean" (fun _ ->
    CleanDir output
    CleanDir outputTests
    CleanDir outputBinaries
    CleanDir outputNuGet
    CleanDir outputBinariesNet45
    CleanDir outputBinariesNetStandard

    CleanDirs !! "./**/bin"
    CleanDirs !! "./**/obj"
)

Target "RestorePackages" (fun _ ->
    DotNetCli.Restore
        (fun p -> 
            { p with
                Project = "./src/Reactive.Streams.sln"
                NoCache = false })
)

Target "Build" (fun _ ->
    if (isWindows) then
        let projects = !! "./**/*.csproj"

        let runSingleProject project =
            DotNetCli.Build
                (fun p -> 
                    { p with
                        Project = project
                        Configuration = configuration })

        projects |> Seq.iter (runSingleProject)
    else
        DotNetCli.Build
            (fun p -> 
                { p with
                    Project = "./src/api/Reactive.Streams/Reactive.Streams.csproj"
                    Framework = "netstandard1.0"
                    Configuration = configuration })
)

Target "RunTests" (fun _ ->
    let projects = !! "./src/**/Reactive.Streams.Example.Unicast.Tests.csproj"
                   ++ "./src/**/Reactive.Streams.TCK.Tests.csproj"

    let runSingleProject project =
        DotNetCli.Test
            (fun p -> 
                { p with
                    Project = project
                    Configuration = configuration })

    projects |> Seq.iter (runSingleProject)
)

//--------------------------------------------------------------------------------
// Nuget targets 
//--------------------------------------------------------------------------------

Target "CreateNuget" (fun _ ->
    let versionSuffix = getBuildParamOrDefault "versionsuffix" ""

    DotNetCli.Pack
        (fun p -> 
            { p with
                Project = "./src/api/Reactive.Streams/Reactive.Streams.csproj"
                Configuration = configuration
                AdditionalArgs = ["--include-symbols"]
                VersionSuffix = versionSuffix
                OutputPath = outputNuGet })

    DotNetCli.Pack
        (fun p -> 
            { p with
                Project = "./src/TCK/Reactive.Streams.TCK/Reactive.Streams.TCK.csproj"
                Configuration = configuration
                AdditionalArgs = ["--include-symbols /p:NuspecFile=Reactive.Streams.TCK.nuspec"]
                VersionSuffix = versionSuffix
                OutputPath = outputNuGet })
)

Target "PublishNuget" (fun _ ->
    let projects = !! "./build/nuget/*.nupkg" -- "./build/nuget/*.symbols.nupkg"
    let apiKey = getBuildParamOrDefault "nugetkey" ""
    let source = getBuildParamOrDefault "nugetpublishurl" ""
    let symbolSource = getBuildParamOrDefault "symbolspublishurl" ""

    let runSingleProject project =
        DotNetCli.RunCommand
            (fun p -> 
                { p with 
                    TimeOut = TimeSpan.FromMinutes 10. })
            (sprintf "nuget push %s --api-key %s --source %s --symbol-source %s" project apiKey source symbolSource)

    projects |> Seq.iter (runSingleProject)
)

//--------------------------------------------------------------------------------
// Help 
//--------------------------------------------------------------------------------

Target "Help" <| fun _ ->
    List.iter printfn [
      "usage:"
      "/build [target]"
      ""
      " Targets for building:"
      " * Build      Builds"
      " * Nuget      Create and optionally publish nugets packages"
      " * RunTests   Runs tests"
      " * All        Builds, run tests, creates and optionally publish nuget packages"
      ""
      " Other Targets"
      " * Help       Display this help" 
      ""]

Target "HelpNuget" <| fun _ ->
    List.iter printfn [
      "usage: "
      "build Nuget [nugetkey=<key> [nugetpublishurl=<url>]] "
      "            [symbolspublishurl=<url>] "
      ""
      "In order to publish a nuget package, keys must be specified."
      "If a key is not specified the nuget packages will only be created on disk"
      "After a build you can find them in build/nuget"
      ""
      "For pushing nuget packages to nuget.org and symbols to symbolsource.org"
      "you need to specify nugetkey=<key>"
      "   build Nuget nugetKey=<key for nuget.org>"
      ""
      "For pushing the ordinary nuget packages to another place than nuget.org specify the url"
      "  nugetkey=<key>  nugetpublishurl=<url>  "
      ""
      "For pushing symbols packages specify:"
      "  symbolskey=<key>  symbolspublishurl=<url> "
      ""
      "Examples:"
      "  build Nuget                      Build nuget packages to the build/nuget folder"
      ""
      "  build Nuget versionsuffix=beta1  Build nuget packages with the custom version suffix"
      ""
      "  build Nuget nugetkey=123         Build and publish to nuget.org and symbolsource.org"
      ""
      "  build Nuget nugetprerelease=dev nugetkey=123 nugetpublishurl=http://abcsymbolspublishurl=http://xyz"
      ""]

//--------------------------------------------------------------------------------
//  Target dependencies
//--------------------------------------------------------------------------------

Target "BuildRelease" DoNothing
Target "All" DoNothing
Target "Nuget" DoNothing

// build dependencies
"Clean" ==> "RestorePackages" ==> "Build" ==> "BuildRelease"

// tests dependencies
"Clean" ==> "RestorePackages" ==> "Build" ==> "RunTests"

// nuget dependencies
"Clean" ==> "RestorePackages" ==> "Build" ==> "CreateNuget"
"CreateNuget" ==> "PublishNuget"
"PublishNuget" ==> "Nuget"

// all
"BuildRelease" ==> "All"
"RunTests" ==> "All"

RunTargetOrDefault "Help"