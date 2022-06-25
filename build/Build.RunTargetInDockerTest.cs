// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Serilog;

partial class Build
{
    [UsedImplicitly]
    Target RunTargetInDockerImageTest => _ => _
        .DockerRun(_ => _
            .SetImage("mcr.microsoft.com/dotnet/sdk:6.0")
            .When(RuntimeInformation.OSArchitecture == Architecture.Arm64, _ => _
                .SetPlatform("linux/arm64")
                .SetDotNetRuntime("linux-arm64"))
            .When(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) , _ => _
                .SetPlatform("windows/amd64")
                .SetDotNetRuntime("win-x64"))
            //this next "when" block isn't strictly necessary, as these are the defaults
            .When(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) , _ => _
                .SetPlatform("linux/amd64")
                .SetDotNetRuntime("linux-x64"))
        )
        .Executes(() =>
        {
            //Console.WriteLine("This should be running in a linux docker container");
            Log.Information("Hello, the computer name is {Name}", Environment.MachineName);
        });
}
