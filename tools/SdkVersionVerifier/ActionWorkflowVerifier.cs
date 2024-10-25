// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using YamlDotNet.RepresentationModel;

namespace SdkVersionVerifier;

internal static class ActionWorkflowVerifier
{
    public static DotnetSdkVersion? GetExpectedSdkVersionFromSampleWorkflow(string root)
    {
        var defaultWorkflow = Path.Combine(GetWorkflowsDirectory(root), "build.yml");
        return ExtractDotnetSdkVersions(File.ReadAllText(defaultWorkflow)).FirstOrDefault();
    }

    public static bool VerifyVersions(string root, DotnetSdkVersion expectedDotnetSdkVersion)
    {
        var workflowsDir = GetWorkflowsDirectory(root);
        var workflows = Directory.GetFiles(workflowsDir, "*.yml");

        return FileVerifier.VerifyMultiple(workflows, VerifySdkVersions, expectedDotnetSdkVersion);
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
        //   6.0.427
        //   7.0.410
        //   8.0.403

        string? sdk6Version = null;
        string? sdk7Version = null;
        string? sdk8Version = null;

        foreach (var version in dotnetVersionNode.ToString().Split())
        {
            if (version.StartsWith('6'))
            {
                sdk6Version = version;
            }

            if (version.StartsWith('7'))
            {
                sdk7Version = version;
            }

            if (version.StartsWith('8'))
            {
                sdk8Version = version;
            }
        }

        if (sdk6Version is not null || sdk7Version is not null || sdk8Version is not null)
        {
            return new DotnetSdkVersion(sdk6Version, sdk7Version, sdk8Version);
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
