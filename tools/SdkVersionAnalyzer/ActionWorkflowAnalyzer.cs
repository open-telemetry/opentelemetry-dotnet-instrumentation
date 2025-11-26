// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace SdkVersionAnalyzer;

internal static class ActionWorkflowAnalyzer
{
    public static DotnetSdkVersion? GetExpectedSdkVersionFromSampleWorkflow(string root)
    {
        var defaultWorkflow = Path.Combine(GetWorkflowsDirectory(root), "build.yml");
        var content = File.ReadAllText(defaultWorkflow);
        return ExtractDotnetSdkVersions(content).FirstOrDefault();
    }

    public static bool VerifyVersions(string root, DotnetSdkVersion expectedDotnetSdkVersion)
    {
        return FileAnalyzer.VerifyMultiple(GetWorkflows(root), VerifySdkVersions, expectedDotnetSdkVersion);
    }

    public static void ModifyVersions(string root, DotnetSdkVersion newDotnetSdkVersion)
    {
        FileAnalyzer.ModifyMultiple(GetWorkflows(root), ModifySdkVersions, newDotnetSdkVersion);
    }

    private static string ModifySdkVersions(string content, DotnetSdkVersion newDotnetSdkVersion)
    {
        using var stringReader = new StringReader(content);
        var scanner = new Scanner(stringReader, skipComments: false);
        var parser = new Parser(scanner);

        var stringBuilder = new StringBuilder();
        using var writer = new StringWriter(stringBuilder);
        var emitter = new Emitter(writer);

        // Use the parser/emitter approach to ensure comments in workflows are preserved
        while (parser.MoveNext())
        {
            var current = parser.Current;
            if (current is Scalar { IsKey: true, Value: "dotnet-version" } scalar)
            {
                emitter.Emit(scalar);
                parser.MoveNext();

                var newScalar = GetNewDotnetVersionScalar(newDotnetSdkVersion);
                emitter.Emit(newScalar);
                continue;
            }

            if (current is Scalar { Value: "" })
            {
                var newScalar = GetScalarWithExpectedFormatting();
                emitter.Emit(newScalar);
                continue;
            }

            if (current != null)
            {
                emitter.Emit(current);
            }
        }

        return stringBuilder.ToString();
    }

    private static Scalar GetScalarWithExpectedFormatting()
    {
        // Ensure empty values end up as expected in action workflow, e.g.:
        //      workflow_call:
        // and not:
        //      workflow_call: ''
        return new Scalar(new TagName("tag:yaml.org,2002:null"), string.Empty);
    }

    private static Scalar GetNewDotnetVersionScalar(DotnetSdkVersion newDotnetSdkVersion)
    {
        const char separator = '\n';
        var val = $"{newDotnetSdkVersion.Net8SdkVersion!}{separator}{newDotnetSdkVersion.Net9SdkVersion!}{separator}{newDotnetSdkVersion.Net10SdkVersion!}";

        // Use ctor with default values, apart from ScalarStyle.
        // Use ScalarStyle.Literal to get dotnet-version with value similar to below:
        //  dotnet-version: |
        //    8.0.404
        //    9.0.100
        //    10.0.100

        return new Scalar(AnchorName.Empty, TagName.Empty, val, ScalarStyle.Literal, true, true, Mark.Empty, Mark.Empty);
    }

    private static string[] GetWorkflows(string root)
    {
        var workflowsDir = GetWorkflowsDirectory(root);
        return Directory.GetFiles(workflowsDir, "*.yml");
    }

    private static string GetWorkflowsDirectory(string root)
    {
        return Path.Combine(root, ".github", "workflows");
    }

    private static bool VerifySdkVersions(string content, DotnetSdkVersion expectedDotnetSdkVersion)
    {
        foreach (var extractedSdkVersion in ExtractDotnetSdkVersions(content))
        {
            if (!VersionComparer.CompareVersions(expectedDotnetSdkVersion, extractedSdkVersion))
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<DotnetSdkVersion> ExtractDotnetSdkVersions(string content)
    {
        var workflow = new YamlStream();
        using var stringReader = new StringReader(content);
        workflow.Load(stringReader);

        var jobs = ExtractJobs(workflow);
        foreach (var job in jobs)
        {
            if (!job.Children.TryGetValue(new YamlScalarNode("steps"), out var stepsNode))
            {
                continue;
            }

            foreach (var step in (YamlSequenceNode)stepsNode)
            {
                var jobStepNode = (YamlMappingNode)step;
                if (jobStepNode.Children.TryGetValue(new YamlScalarNode("uses"), out var usesNode) && usesNode.ToString().StartsWith("actions/setup-dotnet"))
                {
                    var withNode = (YamlMappingNode)jobStepNode.Children[new YamlScalarNode("with")];
                    var dotnetVersionNode = (YamlScalarNode)withNode.Children[new YamlScalarNode("dotnet-version")];

                    var extractedVersion = ExtractVersion(dotnetVersionNode);
                    if (extractedVersion is not null)
                    {
                        yield return extractedVersion;
                    }
                }
            }
        }
    }

    private static DotnetSdkVersion? ExtractVersion(YamlScalarNode dotnetVersionNode)
    {
        // Extract versions from the node value e.g.:
        // dotnet-version: |
        //   8.0.404
        //   9.0.100
        //   10.0.100

        string? sdk8Version = null;
        string? sdk9Version = null;
        string? sdk10Version = null;

        foreach (var version in dotnetVersionNode.ToString().Split())
        {
            if (VersionComparer.IsNet8Version(version))
            {
                sdk8Version = version;
            }

            if (VersionComparer.IsNet9Version(version))
            {
                sdk9Version = version;
            }

            if (VersionComparer.IsNet10Version(version))
            {
                sdk10Version = version;
            }
        }

        if (sdk8Version is not null || sdk9Version is not null)
        {
            return new DotnetSdkVersion(sdk8Version, sdk9Version, sdk10Version);
        }

        return null;
    }

    private static IEnumerable<YamlMappingNode> ExtractJobs(YamlStream yaml)
    {
        var rootNode = yaml.Documents[0].RootNode as YamlMappingNode;
        var jobsNode = (YamlMappingNode)rootNode!.Children[new YamlScalarNode("jobs")];
        return jobsNode.Children.Select(j => (YamlMappingNode)j.Value);
    }
}
