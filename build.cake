#load "local:?path=./cake/configurations.cake"
#load "local:?path=./cake/nuget.cake"
///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");
var progetApiKey = Argument("progetApiKey", EnvironmentVariable("NUGET_API_KEY") ?? "<NULL>");

GitVersion gitVersionResult;
///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
   // Executed BEFORE the first task.
   Information("Running tasks...");
});

Teardown(ctx =>
{
   // Executed AFTER the last task.
   Information("Finished running tasks.");
});

TaskSetup(setupContext =>
{
   if(TeamCity.IsRunningOnTeamCity)
   {
      TeamCity.WriteStartBuildBlock(setupContext.Task.Description ?? setupContext.Task.Name);
      TeamCity.WriteStartProgress(setupContext.Task.Description ?? setupContext.Task.Name);
   }
});

TaskTeardown(teardownContext =>
{
   if(TeamCity.IsRunningOnTeamCity)
   {
      TeamCity.WriteEndProgress(teardownContext.Task.Description ?? teardownContext.Task.Name);
      TeamCity.WriteEndBuildBlock(teardownContext.Task.Description ?? teardownContext.Task.Name);
   }
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("CleanBuild")
.Description($"Cleaning {solutionBuildDir}")
.Does(()=>{
   Information($"Cleaning {solutionBuildDir}");
   CleanDirectory(solutionBuildDir);
});

Task("CleanCode")
.Description($"Cleaning {codeDir}")
.Does(()=>{
   Information($"Cleaning {codeDir}");
   CleanDirectories(codeDir);
});
Task("CleanPackages")
.Description($"Cleaning {packagesDir}")
.Does(()=>{
   Information($"Cleaning {packagesDir}");
   CleanDirectory(packagesDir);
});



Task("GitVersion")
.Description("Running GitVersion")
.Does(()=>{
    gitVersionResult = GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true
    });
    Information($"Setting assembly version to {gitVersionResult.AssemblySemVer}");
    Information($"Setting assembly file version to {gitVersionResult.FullSemVer}");
    Information($"Setting informational version to {gitVersionResult.InformationalVersion}");
      if(TeamCity.IsRunningOnTeamCity)
   {
      TeamCity.SetBuildNumber(gitVersionResult.FullSemVer);
   }
}).OnError(exception =>{
    if(TeamCity.IsRunningOnTeamCity)
   {
      TeamCity.BuildProblem("GitVersion Failed","GitVersion");
   }
});;

Task("Build")
.Description($"Building {solutionName}")
.IsDependentOn("Restore-NuGet-Packages")
.Does(() => {  
   Information($"Building {solutionName}");
   MSBuild(solutionName, settings =>
            settings.SetConfiguration(configuration)
                .SetVerbosity(Verbosity.Minimal));
})
.OnError(exception =>{
    if(TeamCity.IsRunningOnTeamCity)
   {
      TeamCity.BuildProblem("Build Failed","BuildStep");
   }
});

Task("CleanAll")
.Description("Cleanning All Directories")
.IsDependentOn("CleanBuild")
.IsDependentOn("CleanCode")
.IsDependentOn("CleanPackages");

Task("CI-Build")
.IsDependentOn("CleanAll")
.IsDependentOn("GitVersion")
.IsDependentOn("Build")
.IsDependentOn("NugetPack")
.IsDependentOn("NugetPush")
.OnError(exception =>{
    if(TeamCity.IsRunningOnTeamCity)
   {
      TeamCity.BuildProblem("CI Build Failed","CI Build");
   }
});

RunTarget(target);