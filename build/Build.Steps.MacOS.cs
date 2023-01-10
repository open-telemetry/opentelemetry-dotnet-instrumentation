using Nuke.Common;
using Nuke.Common.IO;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;

partial class Build
{
    Target CompileNativeSrcMacOs => _ => _
        .Unlisted()
        .After(CompileManagedSrc)
        .OnlyWhenStatic(() => IsOsx)
        .Executes(() =>
        {
            var nativeProjectDirectory = NativeProfilerProject.Directory;
            CMake.Value(arguments: ".", workingDirectory: nativeProjectDirectory);
            Make.Value(workingDirectory: nativeProjectDirectory);
        });

    Target PublishNativeProfilerMacOs => _ => _
        .Unlisted()
        .OnlyWhenStatic(() => IsOsx)
        .After(CompileNativeSrc, PublishManagedProfiler)
        .Executes(() =>
        {
            // Create home directory
            var source = NativeProfilerProject.Directory / "bin" / $"{NativeProfilerProject.Name}.dylib";
            var dest = TracerHomeDirectory / "osx-x64";
            Log.Information($"Copying '{source}' to '{dest}'");

            CopyFileToDirectory(source, dest, FileExistsPolicy.Overwrite);
        });
}
