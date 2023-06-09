// <copyright file="InstrumentationDefinitionsGenerator.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerators;

/// <summary>
/// Generates InstrumentationDefinitions for byte code instrumentation.
/// It is based on InstrumentMethodAttribute.
/// </summary>
[Generator]
public class InstrumentationDefinitionsGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var instrumentationClasses = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsAttributedClass(node),
                static (node, _) => GetClassesMarkedByInstrumentMethodAttribute(node))
            .Where(static m => m != null);

        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax?>)> compilationAndClasses
            = context.CompilationProvider.Combine(instrumentationClasses.Collect());

        context.RegisterSourceOutput(
            compilationAndClasses,
            static (context, source) => Generate(source.Item1, source.Item2, context));
    }

    private static void Generate(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classesWithAttributes, SourceProductionContext context)
    {
        if (classesWithAttributes.IsDefaultOrEmpty)
        {
            return;
        }

        var distinctClasses = classesWithAttributes.Distinct();

        var (traceIntegrations, metricsIntegrations, logsIntegrations) = GetIntegrationsToGenerate(compilation, distinctClasses, context.CancellationToken);

        var result = GenerateInstrumentationDefinitionsPartialClass(traceIntegrations, metricsIntegrations, logsIntegrations);
        context.AddSource("InstrumentationDefinitions.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static (IReadOnlyCollection<IntegrationToGenerate> TraceIntegrations, IReadOnlyCollection<IntegrationToGenerate> MetricsIntegrations, IReadOnlyCollection<IntegrationToGenerate> LogsIntegrations) GetIntegrationsToGenerate(Compilation compilation, IEnumerable<ClassDeclarationSyntax?> classes, CancellationToken contextCancellationToken)
    {
        var instrumentAttribute = compilation.GetTypeByMetadataName("OpenTelemetry.AutoInstrumentation.Instrumentations.InstrumentMethodAttribute");

        if (instrumentAttribute == null)
        {
            return (Array.Empty<IntegrationToGenerate>(), Array.Empty<IntegrationToGenerate>(), Array.Empty<IntegrationToGenerate>());
        }

        var traceIntegrations = new List<IntegrationToGenerate>();
        var metricsIntegrations = new List<IntegrationToGenerate>();
        var logsIntegrations = new List<IntegrationToGenerate>();

        foreach (var classDeclaration in classes)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclaration!.SyntaxTree);
            var classNamedTypeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, contextCancellationToken) as INamedTypeSymbol;

            var attributes = classNamedTypeSymbol!.GetAttributes();

            foreach (var attribute in attributes)
            {
                if (!instrumentAttribute.Equals(attribute.AttributeClass, SymbolEqualityComparer.Default))
                {
                    continue;
                }

                var (integrationToGenerate, type) = CreateIntegrationToGenerate(attribute, classNamedTypeSymbol);

                switch (type)
                {
                    case 0:
                        traceIntegrations.Add(integrationToGenerate);
                        break;
                    case 1:
                        metricsIntegrations.Add(integrationToGenerate);
                        break;
                    case 2:
                        logsIntegrations.Add(integrationToGenerate);
                        break;
                }
            }
        }

        return (traceIntegrations, metricsIntegrations, logsIntegrations);
    }

    private static (IntegrationToGenerate IntegrationToGenerate, int Type) CreateIntegrationToGenerate(AttributeData attribute, INamedTypeSymbol classNamedTypeSymbol)
    {
        var returnTypeName = attribute.ConstructorArguments[3].Value?.ToString()!;
        var parameterTypeNames = attribute.ConstructorArguments[4].Values;

        var targetSignatureTypes = new string[parameterTypeNames.Length + 1];

        targetSignatureTypes[0] = returnTypeName;

        for (var i = 0; i < parameterTypeNames.Length; i++)
        {
            targetSignatureTypes[i + 1] = parameterTypeNames[i].Value?.ToString()!;
        }

        var integrationToGenerate = new IntegrationToGenerate
        {
            TargetAssembly = attribute.ConstructorArguments[0].Value?.ToString(),
            TargetType = attribute.ConstructorArguments[1].Value?.ToString(),
            TargetMethod = attribute.ConstructorArguments[2].Value?.ToString(),
            TargetSignatureTypes = targetSignatureTypes,
            IntegrationName = attribute.ConstructorArguments[7].Value?.ToString(),
            IntegrationType = classNamedTypeSymbol.ToDisplayString()
        };

        var minVersion = attribute.ConstructorArguments[5].Value?.ToString().Split('.')!;
        integrationToGenerate.TargetMinimumMajor = int.Parse(minVersion[0]);
        if (minVersion.Length > 1 && minVersion[1] != "*")
        {
            integrationToGenerate.TargetMinimumMinor = int.Parse(minVersion[1]);
        }

        if (minVersion.Length > 2 && minVersion[2] != "*")
        {
            integrationToGenerate.TargetMinimumPatch = int.Parse(minVersion[2]);
        }

        var maxVersion = attribute.ConstructorArguments[6].Value?.ToString().Split('.')!;
        integrationToGenerate.TargetMaximumMajor = int.Parse(maxVersion[0]);
        if (maxVersion.Length > 1 && maxVersion[1] != "*")
        {
            integrationToGenerate.TargetMaximumMinor = int.Parse(maxVersion[1]);
        }

        if (maxVersion.Length > 2 && maxVersion[2] != "*")
        {
            integrationToGenerate.TargetMaximumPatch = int.Parse(maxVersion[2]);
        }

        return (integrationToGenerate, int.Parse(attribute.ConstructorArguments[8].Value!.ToString()));
    }

    private static string GenerateInstrumentationDefinitionsPartialClass(IReadOnlyCollection<IntegrationToGenerate> traceIntegrations, IReadOnlyCollection<IntegrationToGenerate> metricsIntegrations, IReadOnlyCollection<IntegrationToGenerate> logsIntegrations)
    {
        var sb = new StringBuilder()
            .AppendFormat(
                @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the InstrumentationDefinitionsGenerator tool. To safely
//     modify this file, edit InstrumentMethodAttribute on the classes and
//     compile project.

//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated. 
// </auto-generated>
//------------------------------------------------------------------------------

using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation;

internal static partial class InstrumentationDefinitions
{{
    private static readonly string AssemblyFullName = typeof(InstrumentationDefinitions).Assembly.FullName!;

    private static NativeCallTargetDefinition[] GetDefinitionsArray()
    {{
        var nativeCallTargetDefinitions = new List<NativeCallTargetDefinition>({0});",
                traceIntegrations.Count + metricsIntegrations.Count + logsIntegrations.Count)
            .AppendLine();

        const string tracesHeader = @"        // Traces
        var tracerSettings = Instrumentation.TracerSettings.Value;
        if (tracerSettings.TracesEnabled)";
        GenerateInstrumentationForSignal(traceIntegrations, sb, tracesHeader, "tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation");

        const string logsHeader = @"        // Logs
        var logSettings = Instrumentation.LogSettings.Value;
        if (logSettings.LogsEnabled)";
        GenerateInstrumentationForSignal(logsIntegrations, sb, logsHeader, "logSettings.EnabledInstrumentations.Contains(LogInstrumentation");

        const string metricsHeader = @"        // Metrics
        var metricSettings = Instrumentation.MetricSettings.Value;
        if (metricSettings.MetricsEnabled)";
        GenerateInstrumentationForSignal(metricsIntegrations, sb, metricsHeader, "metricSettings.EnabledInstrumentations.Contains(MetricInstrumentation");

        sb.AppendLine(@"        return nativeCallTargetDefinitions.ToArray();
    }
}");

        return sb.ToString();
    }

    private static void GenerateInstrumentationForSignal(IReadOnlyCollection<IntegrationToGenerate> integrations, StringBuilder sb, string signalHeader, string conditionPrefix)
    {
        if (integrations.Count > 0)
        {
            sb.AppendLine(signalHeader)
                .AppendLine("{");
            GenerateIntegrations(integrations, sb, conditionPrefix);

            sb.AppendLine("        }")
                .AppendLine();
        }
    }

    private static void GenerateIntegrations(IReadOnlyCollection<IntegrationToGenerate> integrations, StringBuilder sb, string conditionPrefix)
    {
        var gropedByIntegrationName = integrations.GroupBy(x => x.IntegrationName);

        foreach (var group in gropedByIntegrationName)
        {
            sb.Append("            // ");
            sb.AppendLine(group.Key);
            sb.AppendFormat(
                @"            if ({0}.{1}))
            {{",
                conditionPrefix,
                group.Key);
            sb.AppendLine();

            foreach (var integration in group)
            {
                sb.AppendFormat(
                    "                nativeCallTargetDefinitions.Add(new(\"{0}\", \"{1}\", \"{2}\", new[] {{{3}}}, {4}, {5}, {6}, {7}, {8}, {9}, AssemblyFullName, \"{10}\"));",
                    integration.TargetAssembly,
                    integration.TargetType,
                    integration.TargetMethod,
                    string.Join(", ", integration.TargetSignatureTypes!.Select(x => $"\"{x}\"")),
                    integration.TargetMinimumMajor,
                    integration.TargetMinimumMinor,
                    integration.TargetMinimumPatch,
                    integration.TargetMaximumMajor,
                    integration.TargetMaximumMinor,
                    integration.TargetMaximumPatch,
                    integration.IntegrationType);
                sb.AppendLine();
            }

            sb.AppendLine("            }");
            sb.AppendLine();
        }
    }

    private static bool IsAttributedClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    private static ClassDeclarationSyntax? GetClassesMarkedByInstrumentMethodAttribute(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (attributeSyntax.Name.ToString() is "InstrumentMethod" or "InstrumentMethodAttribute")
                {
                    return classDeclarationSyntax;
                }
            }
        }

        return null;
    }
}
