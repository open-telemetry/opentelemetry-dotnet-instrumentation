using Nuke.Common;
using Nuke.Common.IO;
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
            // copy createLogPath.sh
            CopyFileToDirectory(
                RootDirectory / "build" / "artifacts" / "createLogPath.sh",
                TracerHomeDirectory,
                FileExistsPolicy.Overwrite);

            // Create home directory
            CopyFileToDirectory(
                NativeProfilerProject.Directory / "bin" / $"{ArtifactNames.NativeProfiler}.dylib",
                TracerHomeDirectory,
                FileExistsPolicy.Overwrite);
        });
}
