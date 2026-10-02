using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Extensions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.MSBuild;
using static Nuke.Common.EnvironmentInfo;

/// <summary>
/// Prepares the optional AdditionalDeps fallback for framework-dependent standalone .NET deployments.
/// </summary>
/// <remarks>
/// <c>DOTNET_ADDITIONAL_DEPS</c> adds the instrumentation package dependencies to the application's host
/// dependency graph, and <c>DOTNET_SHARED_STORE</c> supplies the runtime assets declared by that graph. This
/// target writes one package-only dependency context per supported runtime and a source-to-store copy plan to
/// tracer-home. The shipped setup scripts use that metadata to create the shared store only when the fallback
/// is needed.
/// </remarks>
partial class Build
{
    private const string AdditionalDepsFileName = "OpenTelemetry.AutoInstrumentation.AdditionalDeps.deps.json";
    private const string SharedFrameworkName = "Microsoft.NETCore.App";
    private AbsolutePath AdditionalDepsDirectory => TracerHomeDirectory / "AdditionalDeps";
    private AbsolutePath StoreSharedFrameworkDirectory => AdditionalDepsDirectory / "shared" / SharedFrameworkName;
    private AbsolutePath SharedStoreCopyPlanFilePath => AdditionalDepsDirectory / "shared-store-copy-plan.txt";

    private IEnumerable<TargetFramework> AdditionalDepsTargetFrameworks => TargetFrameworksForPublish.ExceptNetFramework();

    /// <summary>
    /// Gets the process architectures for which the customer setup scripts create shared-store roots.
    /// </summary>
    /// <remarks>
    /// Windows distributions include x86 in addition to their native platform. Other distribution formats
    /// support only their native target architecture. The set removes the duplicate for an x86 build.
    /// </remarks>
    private HashSet<MSBuildTargetPlatform> AdditionalDepsArchitectures => IsWin ? [Platform, MSBuildTargetPlatform.x86] : [Platform];

    Target PrepareAdditionalDeps => _ => _
        .Unlisted()
        .Description("Prepares AdditionalDeps contexts and the shared-store copy plan from tracer-home.")
        .DependsOn(PublishManagedProfiler)
        .Executes(() =>
        {
            AdditionalDepsDirectory.CreateOrCleanDirectory();

            // Assemblies project built as part of PublishManagedProfiler already produces the
            // SDK dependency contexts for the package dependencies shipped with the standalone distribution.
            // Those contexts are what we need for the AdditionalDeps setup.
            var assembliesProject = Solution.GetProjectByName(Projects.AutoInstrumentationAssemblies);
            var copyLines = new HashSet<string>();
            foreach (var targetFramework in AdditionalDepsTargetFrameworks)
            {
                // location of the Assemblies.deps.json for a given TFM
                var depsJsonPath = assembliesProject.Directory /
                                   "bin" /
                                   BuildConfiguration.ToString() /
                                   targetFramework.ToString() /
                                   $"{assembliesProject.Name}.deps.json";
                GenerateAdditionalDepsContext(targetFramework, depsJsonPath, copyLines);
            }

            WriteSharedStoreCopyPlan(copyLines);
        });

    /// <summary>
    /// Creates AdditionalDeps deps.json context file and appends its shared-store copy lines.
    /// </summary>
    private void GenerateAdditionalDepsContext(TargetFramework targetFramework, AbsolutePath depsJsonPath, HashSet<string> copyLines)
    {
        if (!depsJsonPath.FileExists())
        {
            throw new FileNotFoundException($"Deps.json not found for {targetFramework}.", depsJsonPath);
        }

        // Preserve all SDK-produced metadata by trimming this parsed object in place before writing it back.
        var dependencyContext = JsonNode.Parse(File.ReadAllText(depsJsonPath))?.AsObject()
                                ?? throw new InvalidOperationException($"Deps.json '{depsJsonPath}' cannot be parsed as a JSON object.");

        // The SDK dependency context has two maps keyed by the same library ID:
        //
        // "libraries": {
        //   "Example.Package/1.2.3": {
        //     "type": "package",
        //     "path": "example.package/1.2.3"
        //   }
        // },
        //
        // "targets": {
        //   ".NETCoreApp,Version=v8.0": {
        //     "Example.Package/1.2.3": {
        //       "runtime": {
        //         "lib/net8.0/Example.Package.dll": {
        //           "localPath": "Example.Package.dll"
        //         }
        //       }
        //     }
        //   }
        // }
        //
        // The libraries map identifies a library's type and NuGet package path. The sole selected target
        // is the runtime graph; its matching library entry lists every selected runtime asset. We retain
        // package libraries and their runtime assets, then remove every other library from both maps.
        var libraries = dependencyContext["libraries"]?.AsObject()
                        ?? throw new InvalidOperationException($"Deps.json '{depsJsonPath}' must contain 'libraries' property.");

        var targets = dependencyContext["targets"]?.AsObject()
                      ?? throw new InvalidOperationException($"Deps.json '{depsJsonPath}' must contain 'targets' property.");
        // Different targets can select different runtime assets. This portable project must have one unambiguous graph.
        if (targets.Count != 1)
        {
            throw new InvalidOperationException($"Deps.json '{depsJsonPath}' must contain exactly one runtime target.");
        }

        var target = targets.Single().Value?.AsObject()
                     ?? throw new InvalidOperationException($"The runtime target in deps.json '{depsJsonPath}' is not a JSON object.");

        // Walk through all the libraries:
        // 1) map every package library from tracer-home source to target store path.
        // 2) remove non-package libraries.
        foreach (var library in libraries.ToArray())
        {
            switch (library)
            {
                // 1) For a library of a 'package' type, find its source file in tracer-home and map it to the destination store file.
                case (var libraryId, JsonObject libraryMetadata) when string.Equals(
                    libraryMetadata["type"]?.GetValue<string>(),
                    "package",
                    StringComparison.OrdinalIgnoreCase):
                    var packageTarget = target[libraryId]?.AsObject()
                                        ?? throw new InvalidOperationException($"Package '{libraryId}' in '{depsJsonPath}' is missing its target entry.");
                    var packagePath = libraryMetadata["path"]?.GetValue<string>()
                                      ?? throw new InvalidOperationException($"Package '{libraryId}' in '{depsJsonPath}' is missing its NuGet package path.");
                    AddPackageCopyLines(packagePath, packageTarget, targetFramework, copyLines);

                    break;

                // 2) Remove non-package libraries. Project libraries for example are loaded from tracer-home rather than the shared store.
                case (var libraryId, JsonObject):
                    libraries.Remove(libraryId);
                    target.Remove(libraryId);
                    break;

                case (var libraryId, _):
                    throw new InvalidOperationException(
                        $"Library '{libraryId}' in deps.json '{depsJsonPath}' is not a JSON object.");
            }
        }

        WriteAdditionalDepsContext(targetFramework, dependencyContext);
    }

    /// <summary>
    /// Appends the shared-store copy lines for every runtime asset of one package.
    /// </summary>
    private void AddPackageCopyLines(
        string packagePath,
        JsonObject packageTarget,
        TargetFramework targetFramework,
        HashSet<string> copyLines)
    {
        if (packageTarget["runtime"] is not JsonObject dlls)
        {
            return;
        }

        foreach ((var dllPath, var dllJsonNode) in dlls)
        {
            var dllMetadata = dllJsonNode?.AsObject()
                                ?? throw new InvalidOperationException($"Runtime asset '{dllPath}' is not an object.");
            // The Assemblies publish output uses localPath for the flattened output file name.
            // ResolveTracerHomeSource deliberately rejects a path so a future layout change fails visibly.
            var dllLocalPath = dllMetadata["localPath"]?.GetValue<string>() ?? Path.GetFileName(dllPath);
            var sourceRelativePath = ResolveTracerHomeSource(targetFramework, dllLocalPath);

            foreach (var architecture in AdditionalDepsArchitectures)
            {
                // The .NET store layout is <architecture>/<TFM>/<NuGet package path>/<logical runtime dll path>.
                // For example above: x64/net8.0/Example.Package/1.2.3/lib/net8.0/Example.Package.dll.
                var storeRelativePath = $"{architecture.ToString().ToLowerInvariant()}/{targetFramework}/{packagePath}/{dllPath}";
                copyLines.Add($"{sourceRelativePath}|{storeRelativePath}");
            }
        }
    }

    /// <summary>
    /// Resolves a flattened publish filename to the physical file retained by tracer-home optimization.
    /// </summary>
    private string ResolveTracerHomeSource(TargetFramework targetFramework, string localPath)
    {
        if (string.IsNullOrWhiteSpace(localPath) || Path.GetFileName(localPath) != localPath)
        {
            throw new InvalidOperationException($"Unsupported local runtime asset path '{localPath}'.");
        }

        var netDirectory = TracerHomeDirectory / TargetFramework.OutputFolderNet;
        var frameworkDirectory = netDirectory / targetFramework.ToString();
        var frameworkFile = frameworkDirectory / localPath;
        // 1) First check framewework specific path: /net/<TFM>/assembly.dll
        if (frameworkFile.FileExists())
        {
            return Path.GetRelativePath(TracerHomeDirectory, frameworkFile).Replace('\\', '/');
        }

        // 2) Then check framework specific .link file: /net/<TFM>/assembly.dll.link
        var linkFile = frameworkDirectory / $"{localPath}.link";
        if (linkFile.FileExists())
        {
            var linkedFrameworkName = File.ReadAllText(linkFile).Trim();
            var linkedFramework = new TargetFramework.TargetFrameworkTypeConverter()
                .ConvertFromInvariantString(linkedFrameworkName) as TargetFramework;
            if (linkedFramework is null || !TargetFramework.Net.Contains(linkedFramework))
            {
                throw new InvalidOperationException($"Link file '{linkFile}' contains unsupported target framework '{linkedFrameworkName}'.");
            }

            var linkedFile = netDirectory / linkedFramework.ToString() / localPath;
            if (!linkedFile.FileExists())
            {
                throw new FileNotFoundException($"Link file '{linkFile}' points to a missing file.", linkedFile);
            }

            return Path.GetRelativePath(TracerHomeDirectory, linkedFile).Replace('\\', '/');
        }

        // 3) Last check common path: /net/assembly.dll
        var commonFile = netDirectory / localPath;
        if (commonFile.FileExists())
        {
            return Path.GetRelativePath(TracerHomeDirectory, commonFile).Replace('\\', '/');
        }

        throw new FileNotFoundException(
            $"Unable to find runtime dll '{localPath}' for {targetFramework} in tracer-home.",
            frameworkFile);
    }

    /// <summary>
    /// Writes one package-only dependency context in the layout consumed by DOTNET_ADDITIONAL_DEPS.
    /// </summary>
    private void WriteAdditionalDepsContext(TargetFramework targetFramework, JsonObject dependencyContext)
    {
        var sharedFrameworkVersion = targetFramework.SharedFrameworkVersion
                                     ?? throw new InvalidOperationException(
                                         $"Target framework '{targetFramework}' does not have a shared-framework version.");

        var frameworkDirectory = StoreSharedFrameworkDirectory / sharedFrameworkVersion.ToString();
        frameworkDirectory.CreateDirectory();

        File.WriteAllText(
            frameworkDirectory / AdditionalDepsFileName,
            dependencyContext.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>
    /// Writes the source-to-store copy plan shared by all generated AdditionalDeps contexts.
    /// </summary>
    private void WriteSharedStoreCopyPlan(IEnumerable<string> copyLines)
    {
        // Every row is one complete copy. LF lets the POSIX script read a Windows-built plan without a trailing CR.
        File.WriteAllText(
            SharedStoreCopyPlanFilePath,
            string.Join('\n', copyLines.OrderBy(line => line, StringComparer.OrdinalIgnoreCase)) + '\n',
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
