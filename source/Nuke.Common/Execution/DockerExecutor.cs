// // Copyright 2021 Maintainers of NUKE.
// // Distributed under the MIT License.
// // https://github.com/nuke-build/nuke/blob/master/LICENSE
//
// using System;
// using System.Collections;
// using System.IO;
// using System.Linq;
// using System.Text;
// using Nuke.Common.IO;
// using Nuke.Common.ProjectModel;
// using Nuke.Common.Tooling;
// using Nuke.Common.Tools.Docker;
// using Nuke.Common.Tools.DotNet;
// using Serilog;
//
// namespace Nuke.Common.Execution
// {
//     internal class DockerExecutor
//     {
//         public static void Execute(ExecutableTarget target)
//         {
//             PullImageIfRequired(target);
//
//             PublishBuildProject(target);
//
//             CopyPackageReferences();
//
//             RunDocker(target);
//         }
//
//         private static void RunDocker(ExecutableTarget target)
//         {
//             var workingDirectory = GetWorkingDirectory(target.DockerRunTargetSettings.DockerPlatform);
//             var path = GetPathToPublishedBuildProject(target.DockerRunTargetSettings.DockerPlatform);
//             var args = GetArgs(target, path, workingDirectory);
//             var envFile = GetEnvFile(workingDirectory);
//             var volumes = new[] { $"{NukeBuild.RootDirectory}:{workingDirectory}" };
//
//             try
//             {
//                 Log.Information("Executing target {Target} in a new docker container based on {Image}", target.Name, target.DockerRunTargetSettings.Image);
//
//                 DockerTasks.DockerRun(settings => settings
//                     .EnableRm()
//                     .SetImage(target.DockerRunTargetSettings.Image)
//                     .SetVolume(volumes)
//                     .AddVolume(volumes)
//                     .SetCommand("dotnet")
//                     .SetPlatform(target.DockerRunTargetSettings.DockerPlatform)
//                     .SetWorkdir(workingDirectory)
//                     .SetEnvFile(envFile)
//                     .SetArgs(args.Concat(target.DockerRunTargetSettings.Args)));
//             }
//             finally
//             {
//                 File.Delete(envFile);
//             }
//         }
//
//         private static string[] GetArgs(ExecutableTarget target, string path, string workingDirectory)
//         {
//             return new[]
//                    {
//                        path,
//                        "--nologo",
//                        "--skip",
//                        "--target", $"\"{target.Name}\"",
//                        "--root", workingDirectory
//                    };
//         }
//
//         private static string GetPathToPublishedBuildProject(string platform)
//         {
//             return PathConstruction.Combine(
//                 GetWorkingDirectory(platform),
//                 GetRelativePathToPublishedBuildProject(platform));
//         }
//
//         private static RelativePath GetRelativePathToPublishedBuildProject(string platform)
//         {
//             return IsWindowsContainer(platform)
//                 ? (RelativePath) NukeBuild.RootDirectory.GetWinRelativePathTo(PublishedBuildDirectory / Path.GetFileName(NukeBuild.BuildAssemblyFile))
//                 : (RelativePath) NukeBuild.RootDirectory.GetUnixRelativePathTo(PublishedBuildDirectory / Path.GetFileName(NukeBuild.BuildAssemblyFile));
//         }
//
//         private static string GetWorkingDirectory(string platform)
//         {
//             return IsWindowsContainer(platform) ? "c:\\Build" : "/build";
//         }
//
//         private static bool IsWindowsContainer(string platform)
//         {
//             return platform.StartsWith("win", StringComparison.OrdinalIgnoreCase);
//         }
//
//
//         private static void PullImageIfRequired(ExecutableTarget target)
//         {
//             if (target.DockerRunTargetSettings.PullImage)
//             {
//                 Log.Information("Pulling image {Image}", target.DockerRunTargetSettings.Image);
//                 DockerTasks.DockerPull(settings => settings
//                     .SetName(target.DockerRunTargetSettings.Image));
//             }
//         }
//
//         private static string GetEnvFile(string workingDirectory)
//         {
//             //todo: this will probably fail with an env var value with an ampersand in it
//             //todo: this will probably fail with an env var value with new lines in it
//             var stringBuilder = new StringBuilder();
//             var dictionaryEntries = Environment.GetEnvironmentVariables()
//                 .Cast<DictionaryEntry>()
//                 .Where(env =>
//                 {
//                     var key = (string)env.Key;
//                     return !key.Contains(" ")
//                            && key != "USERPROFILE"
//                            && key != "USERNAME"
//                            && key != "LOCALAPPDATA"
//                            && key != "APPDATA"
//                            && key != "TEMP"
//                            && key != "TMP"
//                            && key != "HOMEPATH";
//                 });
//
//             foreach (var env in dictionaryEntries)
//                 stringBuilder.AppendLine($"{env.Key}={env.Value}");
//
//             stringBuilder.AppendLine("DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1");
//             stringBuilder.AppendLine("DOTNET_CLI_TELEMETRY_OPTOUT=1");
//             stringBuilder.AppendLine($"DOTNET_CLI_HOME={workingDirectory}");
//             stringBuilder.AppendLine($"TEMP=/build/{NukeBuild.RootDirectory.GetUnixRelativePathTo(NukeBuild.TemporaryDirectory)}");
//             stringBuilder.AppendLine($"TMP=/build/{NukeBuild.RootDirectory.GetUnixRelativePathTo(NukeBuild.TemporaryDirectory)}");
//             stringBuilder.AppendLine($"{RunningInDockerEnvironmentVariable}=1");
//
//             //without this, errors crash the process (on an m1 mac at least) with
//             //Failed to create CoreCLR, HRESULT: 0x80004005
//             //https://github.com/PowerShell/PowerShell/issues/13166#issuecomment-713034137
//             stringBuilder.AppendLine($"COMPlus_EnableDiagnostics=0");
//
//             var tempFile = Path.GetTempFileName();
//             File.WriteAllText(tempFile, stringBuilder.ToString());
//             return tempFile;
//         }
//
//         public static string RunningInDockerEnvironmentVariable = "NUKE_RUNNING_IN_DOCKER";
//         private static AbsolutePath PublishedBuildDirectory = NukeBuild.TemporaryDirectory / "nukebuild";
//     }
// }
