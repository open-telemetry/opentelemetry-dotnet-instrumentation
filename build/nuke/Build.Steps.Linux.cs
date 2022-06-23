using Nuke.Common;
using Nuke.Common.IO;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;

partial class Build
{
    Target CompileNativeSrcLinux => _ => _
        .Unlisted()
        .After(CompileManagedSrc)
        .OnlyWhenStatic(() => IsLinux)
        .Executes(() =>
        {
            var buildDirectory = NativeProfilerProject.Directory / "build";
            EnsureExistingDirectory(buildDirectory);

            CMake.Value(
                arguments: "../ -DCMAKE_BUILD_TYPE=Release",
                workingDirectory: buildDirectory);
            Make.Value(workingDirectory: buildDirectory);
        });

    Target CompileNativeTestsLinux => _ => _
        .Unlisted()
        .After(CompileNativeSrc)
        .OnlyWhenStatic(() => IsLinux)
        .Executes(() =>
        {
            // TODO: Compile Linux native tests
            Log.Error("Linux native tests are currently not supported.");
        });

    Target PublishNativeProfilerLinux => _ => _
        .Unlisted()
        .OnlyWhenStatic(() => IsLinux)
        .After(CompileNativeSrc, PublishManagedProfiler)
        .Executes(() =>
        {
            // copy createLogPath.sh
            CopyFileToDirectory(
                RootDirectory / "build" / "artifacts" / "createLogPath.sh",
                TracerHomeDirectory,
                FileExistsPolicy.Overwrite);

            // Copy Native file
            CopyFileToDirectory(
                NativeProfilerProject.Directory / "build" / "bin" / $"{NativeProfilerProject.Name}.so",
                TracerHomeDirectory,
                FileExistsPolicy.Overwrite);
        });

    Target RunNativeTestsLinux => _ => _
        .Unlisted()
        .After(CompileNativeSrcLinux)
        .After(CompileNativeTestsLinux)
        .After(PublishManagedProfiler)
        .OnlyWhenStatic(() => IsLinux)
        .Executes(() =>
        {
            // TODO: Run Linux native tests
            Log.Error("Linux native tests are currently not supported.");
        });
}
