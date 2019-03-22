#load "local:?path=./configurations.cake"

//NuGet
Task("NugetPack")
.Description($"Packing Nuspecs")
  .Does(() =>
{
    var nugetVersion = gitVersionResult?.NuGetVersion ?? GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = false
    }).NuGetVersion;

    EnsureDirectoryExists(nupkgsDir);
    NuGetPack(GetFiles($"{nuspecsDir}*.nuspec"), new NuGetPackSettings
        {
            OutputDirectory = nupkgsDir,
            Properties = new Dictionary<string, string>
            {
                { "Configuration", configuration }
            },
            Version = nugetVersion
        });
})
.OnError(exception=>{
     if(TeamCity.IsRunningOnTeamCity)
   {
      TeamCity.BuildProblem($"NugetPack Failed {exception}","NugetPack");
   }
});

Task("NugetPush")
.Description($"Pushing Packages")
.Does(() =>
  {
    // Push the package.
    NuGetPush(GetFiles($"{nupkgsDir}*.nupkg"), new NuGetPushSettings {
        Source = "http://prtproget.faroeurope.com/nuget/Default",
        ApiKey = progetApiKey
    });
  });

Task("Restore-NuGet-Packages")
.Description($"Restoring NuGet Packages")
    .Does(() =>
    {
        NuGetRestore(solutionName);
    });