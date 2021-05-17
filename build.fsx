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
let outputBinariesNet45 = outputBinaries @@ "net452"
let outputBinariesNetStandard = outputBinaries @@ "netstandard2.0"

// Configuration values for tests
let testNetFrameworkVersion = "net461"
let testNetCoreVersion = "netcoreapp3.1"

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

module internal ResultHandling =
    let (|OK|Failure|) = function
        | 0 -> OK
        | x -> Failure x

    let buildErrorMessage = function
        | OK -> None
        | Failure errorCode ->
            Some (sprintf "xUnit2 reported an error (Error Code %d)" errorCode)

    let failBuildWithMessage = function
        | DontFailBuild -> traceError
        | _ -> (fun m -> raise(FailedTestsException m))

    let failBuildIfXUnitReportedError errorLevel =
        buildErrorMessage
        >> Option.iter (failBuildWithMessage errorLevel)

Target "RunTests" (fun _ ->    
    let projects = 
        match (isWindows) with 
                            | true -> !! "./src/**/*.Tests.*sproj"
                            | _ -> !! "./src/**/*.Tests.*sproj" // if you need to filter specs for Linux vs. Windows, do it here
    
    let runSingleProject project =
        let arguments =
            (sprintf "test -c Release --no-build --logger:trx --logger:\"console;verbosity=normal\" --framework %s --results-directory \"%s\" -- -parallel none" testNetFrameworkVersion outputTests)

        let result = ExecProcess(fun info ->
            info.FileName <- "dotnet"
            info.WorkingDirectory <- (Directory.GetParent project).FullName
            info.Arguments <- arguments) (TimeSpan.FromMinutes 30.0) 
        
        ResultHandling.failBuildIfXUnitReportedError TestRunnerErrorLevel.Error result

    CreateDir outputTests
    projects |> Seq.iter (runSingleProject)
)

Target "RunTestsNetCore" (fun _ ->
    let projects = 
        match (isWindows) with 
                        | true -> !! "./src/**/*.Tests.*sproj"
                        | _ -> !! "./src/**/*.Tests.*sproj" // if you need to filter specs for Linux vs. Windows, do it here
     
    let runSingleProject project =
        let arguments =
            (sprintf "test -c Release --no-build --logger:trx --logger:\"console;verbosity=normal\" --framework %s --results-directory \"%s\" -- -parallel none" testNetCoreVersion outputTests)

        let result = ExecProcess(fun info ->
            info.FileName <- "dotnet"
            info.WorkingDirectory <- (Directory.GetParent project).FullName
            info.Arguments <- arguments) (TimeSpan.FromMinutes 30.0) 

        ResultHandling.failBuildIfXUnitReportedError TestRunnerErrorLevel.Error result

    CreateDir outputTests
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
    let rec publishPackage url apiKey trialsLeft packageFile =
        tracefn "Pushing %s Attempts left: %d" (FullName packageFile) trialsLeft
        try 
            DotNetCli.RunCommand
                (fun p -> 
                    { p with 
                        TimeOut = TimeSpan.FromMinutes 10. })
                (sprintf "nuget push %s --api-key %s --source %s" packageFile apiKey url)
        with exn -> 
            if (trialsLeft > 0) then (publishPackage url apiKey (trialsLeft-1) packageFile)
            else raise exn

    let shouldPushNugetPackages = hasBuildParam "nugetkey"
    
    if (shouldPushNugetPackages) then
        printfn "Pushing nuget packages"
        let projects = !! "./build/nuget/*.nupkg" -- "./build/nuget/*.symbols.nupkg"
        for package in projects do
            try
                publishPackage (getBuildParamOrDefault "nugetpublishurl" "https://api.nuget.org/v3/index.json") (getBuildParam "nugetkey") 3 package
            with exn ->
                printfn "%s" exn.Message
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
      " * Build             Builds"
      " * Nuget             Create and optionally publish nugets packages"
      " * RunTests          Runs .NET Framework tests"
      " * RunTestsNetCore   Runs .NET Core tests"
      " * All               Builds, run tests, creates and optionally publish nuget packages"
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
"Clean" ==> "RestorePackages" ==> "Build"
"Build" ==> "BuildRelease"

// tests dependencies
"Build" ==> "RunTests"
"Build" ==> "RunTestsNetCore"

// nuget dependencies
"BuildRelease" ==> "CreateNuget"  ==> "PublishNuget" ==> "Nuget"

// all
"BuildRelease" ==> "All"
"RunTests" ==> "All"
"RunTestsNetCore" ==> "All"
"CreateNuget" ==> "All"

RunTargetOrDefault "Help"