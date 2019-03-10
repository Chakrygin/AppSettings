var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");


var artifactsDirectory = "./artifacts";
var testresultsDirectory = "./testresults";


var solutionFile = "./*.sln";
var srcProjectFiles = "./src/**/*.csproj";
var testsProjectFiles = "./tests/**/*.Tests.csproj";
var testresultsFiles = System.IO.Path.Combine(testresultsDirectory, "*.trx");


var MSBuildSettings = new DotNetCoreMSBuildSettings()
    .HideLogo()
    .WithProperty("NoWarn", "NU5105");


Task("GitVersion")
    .Does(() => {
        var result = GitVersion(new GitVersionSettings {
            NoFetch = !BuildSystem.IsLocalBuild
        });

        var version = result.SemVer;
        var fileVersion = result.AssemblySemFileVer;
        var informationalVersion = result.InformationalVersion;

        var repositoryBranch = result.BranchName;
        var repositoryCommit = result.Sha;

        Information($"Version: {version}");
        Information($"FileVersion: {fileVersion}");
        Information($"InformationalVersion: {informationalVersion}");

        Information($"RepositoryBranch: {repositoryBranch}");
        Information($"RepositoryCommit: {repositoryCommit}");

        MSBuildSettings
            .SetVersion(version)
            .SetFileVersion(fileVersion)
            .SetInformationalVersion(informationalVersion)
            .WithProperty("RepositoryBranch", repositoryBranch)
            .WithProperty("RepositoryCommit", repositoryCommit);
    });


Task("Clean")
    .Does(() => {
        var settings = new DeleteDirectorySettings {Recursive = true};

        if (DirectoryExists(artifactsDirectory)) {
            DeleteDirectory(artifactsDirectory, settings);
        }

        if (DirectoryExists(testresultsDirectory)) {
            DeleteDirectory(testresultsDirectory, settings);
        }
    })
    .DoesForEach(GetFiles(solutionFile), file => {
        DotNetCoreClean(file.FullPath, new DotNetCoreCleanSettings {
            Configuration = configuration,
            MSBuildSettings = MSBuildSettings,
        });
    });

Task("Restore")
    .DoesForEach(GetFiles(solutionFile), file => {
        DotNetCoreRestore(file.FullPath, new DotNetCoreRestoreSettings {
            NoDependencies = true,
            MSBuildSettings = MSBuildSettings,
        });
    });

Task("Build")
    .DoesForEach(GetFiles(solutionFile), file => {
        DotNetCoreBuild(file.FullPath, new DotNetCoreBuildSettings {
            NoRestore = true,
            Configuration = configuration,
            MSBuildSettings = MSBuildSettings,
        });
    });

Task("Test")
    .DeferOnError()
    .DoesForEach(GetFiles(testsProjectFiles), file => {
        DotNetCoreTest(file.FullPath, new DotNetCoreTestSettings {
            NoBuild = true,
            Logger = "trx",
            ResultsDirectory = testresultsDirectory,
            Configuration = configuration,
        });
    })
    .Does(() => {
        foreach (var file in GetFiles(testresultsFiles)) {
            if (AppVeyor.IsRunningOnAppVeyor) {
                AppVeyor.UploadTestResults(file.FullPath, AppVeyorTestResultsType.MSTest);
            }
            else {
                Information(file.FullPath);
            }
        }
    });

Task("Pack")
    .DoesForEach(GetFiles(srcProjectFiles), file => {
        DotNetCorePack(file.FullPath, new DotNetCorePackSettings {
            NoBuild = true,
            OutputDirectory = artifactsDirectory,
            Configuration = configuration,
            MSBuildSettings = MSBuildSettings,
        });
    });


Task("Default")
    .IsDependentOn("GitVersion")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");


RunTarget(target);
