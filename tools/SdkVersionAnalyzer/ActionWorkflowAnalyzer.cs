// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace SdkVersionAnalyzer;

internal static class ActionWorkflowAnalyzer
{
    public static DotnetSdkVersion? GetExpectedSdkVersionFromSetupDotnetAction(string root)
    {
        var setupDotnetAction = Path.Combine(GetActionsDirectory(root), "setup-dotnet", "action.yml");
        var content = File.ReadAllText(setupDotnetAction);
        return ExtractDotnetSdkVersions(content).FirstOrDefault();
    }

    public static bool VerifyVersions(string root, DotnetSdkVersion expectedDotnetSdkVersion)
    {
        return FileAnalyzer.VerifyMultiple(GetYamlFiles(root), VerifySdkVersions, expectedDotnetSdkVersion);
    }

    public static void ModifyVersions(string root, DotnetSdkVersion newDotnetSdkVersion)
    {
        FileAnalyzer.ModifyMultiple(GetYamlFiles(root), ModifySdkVersions, newDotnetSdkVersion);
    }

    private static string ModifySdkVersions(string content, DotnetSdkVersion newDotnetSdkVersion)
    {
        using var stringReader = new StringReader(content);
        var scanner = new Scanner(stringReader, skipComments: false);
        var parser = new Parser(scanner);
        List<(int Start, int End, string Replacement)> replacements = [];

        while (parser.MoveNext())
        {
            var current = parser.Current;
            if (current is Scalar { IsKey: true, Value: "dotnet-version" } scalar)
            {
                if (!parser.MoveNext() || parser.Current is not Scalar valueScalar)
                {
                    throw new InvalidOperationException("dotnet-version key must have a scalar value.");
                }

                replacements.Add((
                    checked((int)valueScalar.Start.Index),
                    checked((int)valueScalar.End.Index),
                    GetNewDotnetVersionScalar(content, scalar, valueScalar, newDotnetSdkVersion)));
            }
        }

        if (replacements.Count == 0)
        {
            return content;
        }

        var stringBuilder = new System.Text.StringBuilder(content.Length);
        var currentPosition = 0;

        foreach (var (start, end, replacement) in replacements.OrderBy(r => r.Start))
        {
            stringBuilder.Append(content, currentPosition, start - currentPosition);
            stringBuilder.Append(replacement);
            currentPosition = end;
        }

        stringBuilder.Append(content, currentPosition, content.Length - currentPosition);
        return stringBuilder.ToString();
    }

    private static string GetNewDotnetVersionScalar(string content, Scalar keyScalar, Scalar valueScalar, DotnetSdkVersion newDotnetSdkVersion)
    {
        var newline = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var indentation = GetLineIndentation(content, checked((int)keyScalar.Start.Index)) + "  ";
        var replacement = string.Join(
            newline,
            [
                "|",
                $"{indentation}{newDotnetSdkVersion.Net8SdkVersion!}",
                $"{indentation}{newDotnetSdkVersion.Net9SdkVersion!}",
                $"{indentation}{newDotnetSdkVersion.Net10SdkVersion!}",
            ]);

        var originalValue = content[checked((int)valueScalar.Start.Index)..checked((int)valueScalar.End.Index)];
        if (originalValue.EndsWith(newline, StringComparison.Ordinal))
        {
            replacement += newline;
        }

        return replacement;
    }

    private static string GetLineIndentation(string content, int index)
    {
        var lineStart = content.LastIndexOf('\n', Math.Max(index - 1, 0));
        lineStart = lineStart == -1 ? 0 : lineStart + 1;
        return content[lineStart..index];
    }

    private static string[] GetYamlFiles(string root)
    {
        var workflows = Directory.GetFiles(GetWorkflowsDirectory(root), "*.yml");
        var actions = Directory.GetFiles(GetActionsDirectory(root), "action.yml", SearchOption.AllDirectories);
        return workflows.Concat(actions).ToArray();
    }

    private static string GetWorkflowsDirectory(string root)
    {
        return Path.Combine(root, ".github", "workflows");
    }

    private static string GetActionsDirectory(string root)
    {
        return Path.Combine(root, ".github", "actions");
    }

    private static bool VerifySdkVersions(string content, DotnetSdkVersion expectedDotnetSdkVersion)
    {
        foreach (var dotnetVersionNode in ExtractDotnetVersionNodes(content))
        {
            if (ContainsGitHubExpression(dotnetVersionNode))
            {
                Console.WriteLine(".NET SDK version must be a literal value. GitHub expression substitutions are not allowed in dotnet-version.");
                return false;
            }

            var extractedSdkVersion = ExtractVersion(dotnetVersionNode);
            if (extractedSdkVersion is null)
            {
                continue;
            }

            if (!VersionComparer.CompareVersions(expectedDotnetSdkVersion, extractedSdkVersion))
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<DotnetSdkVersion> ExtractDotnetSdkVersions(string content)
    {
        foreach (var dotnetVersionNode in ExtractDotnetVersionNodes(content))
        {
            var extractedVersion = ExtractVersion(dotnetVersionNode);
            if (extractedVersion is not null)
            {
                yield return extractedVersion;
            }
        }
    }

    private static IEnumerable<YamlScalarNode> ExtractDotnetVersionNodes(string content)
    {
        var workflow = new YamlStream();
        using var stringReader = new StringReader(content);
        workflow.Load(stringReader);

        foreach (var stepGroup in ExtractStepGroups(workflow))
        {
            foreach (var step in stepGroup)
            {
                var jobStepNode = (YamlMappingNode)step;
                if (jobStepNode.Children.TryGetValue(new YamlScalarNode("uses"), out var usesNode) && usesNode.ToString().StartsWith("actions/setup-dotnet", StringComparison.Ordinal))
                {
                    var withNode = (YamlMappingNode)jobStepNode.Children[new YamlScalarNode("with")];
                    var dotnetVersionNode = (YamlScalarNode)withNode.Children[new YamlScalarNode("dotnet-version")];
                    yield return dotnetVersionNode;
                }
            }
        }
    }

    private static bool ContainsGitHubExpression(YamlScalarNode dotnetVersionNode)
    {
        return dotnetVersionNode.ToString().Contains("${{", StringComparison.Ordinal);
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

    private static IEnumerable<YamlSequenceNode> ExtractStepGroups(YamlStream yaml)
    {
        var rootNode = yaml.Documents[0].RootNode as YamlMappingNode;
        if (rootNode is null)
        {
            yield break;
        }

        if (rootNode.Children.TryGetValue(new YamlScalarNode("jobs"), out var jobsNode))
        {
            foreach (var job in ((YamlMappingNode)jobsNode).Children.Select(j => (YamlMappingNode)j.Value))
            {
                if (job.Children.TryGetValue(new YamlScalarNode("steps"), out var stepsNode))
                {
                    yield return (YamlSequenceNode)stepsNode;
                }
            }
        }

        if (rootNode.Children.TryGetValue(new YamlScalarNode("runs"), out var runsNode)
            && runsNode is YamlMappingNode runsMapping
            && runsMapping.Children.TryGetValue(new YamlScalarNode("steps"), out var actionStepsNode))
        {
            yield return (YamlSequenceNode)actionStepsNode;
        }
    }
}
