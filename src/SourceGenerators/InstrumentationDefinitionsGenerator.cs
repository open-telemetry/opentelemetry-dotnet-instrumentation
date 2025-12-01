// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Globalization;
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
    private const string InstrumentMethodAttributeName = "OpenTelemetry.AutoInstrumentation.Instrumentations.InstrumentMethodAttribute";
    private const int IntegrationKindDirect = 0;
    private const int IntegrationKindDerived = 1;

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var instrumentationClasses = context.SyntaxProvider.ForAttributeWithMetadataName(
                InstrumentMethodAttributeName,
                (node, _) => node is ClassDeclarationSyntax,
                GetClassesMarkedByInstrumentMethodAttribute)
            .Where(static m => m != null);

        var instrumentationClassesAsOneCollection = instrumentationClasses.Collect();

        context.RegisterSourceOutput(
            instrumentationClassesAsOneCollection,
            static (context, instrumentationClasses) => Generate(in instrumentationClasses, context));
    }

    private static void Generate(in ImmutableArray<IntegrationToGenerate?> instrumentationClasses, SourceProductionContext context)
    {
        if (instrumentationClasses.IsDefaultOrEmpty)
        {
            return;
        }

        var directIntegrations = GenerateInstrumentationDefinitionsPartialClass(instrumentationClasses, IntegrationKindDirect);
        context.AddSource("InstrumentationDefinitions.g.cs", SourceText.From(directIntegrations, Encoding.UTF8));

        var derivedIntegrations = GenerateInstrumentationDefinitionsPartialClass(instrumentationClasses, IntegrationKindDerived);
        context.AddSource("InstrumentationDefinitions.Derived.g.cs", SourceText.From(derivedIntegrations, Encoding.UTF8));
    }

    private static TargetToGenerate CreateTargetToGenerate(AttributeData attribute)
    {
        var returnTypeName = attribute.ConstructorArguments[3].Value?.ToString()!;
        var parameterTypeNames = attribute.ConstructorArguments[4].Values;

        var targetSignatureTypesBuilder = new StringBuilder();

        targetSignatureTypesBuilder.AppendFormat(CultureInfo.InvariantCulture, "\"{0}\"", returnTypeName);

        foreach (var parameterTypeName in parameterTypeNames)
        {
            targetSignatureTypesBuilder.AppendFormat(CultureInfo.InvariantCulture, ", \"{0}\"", parameterTypeName.Value);
        }

        var signalType = int.Parse(attribute.ConstructorArguments[8].Value!.ToString(), CultureInfo.InvariantCulture);
        var integrationKind = int.Parse(attribute.ConstructorArguments[9].Value!.ToString(), CultureInfo.InvariantCulture);
        var integrationName = attribute.ConstructorArguments[7].Value!.ToString();
        var targetAssembly = attribute.ConstructorArguments[0].Value!.ToString();
        var targetType = attribute.ConstructorArguments[1].Value!.ToString();
        var targetMethod = attribute.ConstructorArguments[2].Value!.ToString();

        var minVersion = attribute.ConstructorArguments[5].Value?.ToString().Split('.')!;

        var targetMinimumMajor = int.Parse(minVersion[0], CultureInfo.InvariantCulture);
        var targetMinimumMinor = minVersion.Length > 1 && minVersion[1] != "*" ? int.Parse(minVersion[1], CultureInfo.InvariantCulture) : ushort.MinValue;
        var targetMinimumPatch = minVersion.Length > 2 && minVersion[2] != "*" ? int.Parse(minVersion[2], CultureInfo.InvariantCulture) : ushort.MinValue;

        var maxVersion = attribute.ConstructorArguments[6].Value?.ToString().Split('.')!;
        var targetMaximumMajor = int.Parse(maxVersion[0], CultureInfo.InvariantCulture);
        var targetMaximumMinor = maxVersion.Length > 1 && maxVersion[1] != "*" ? int.Parse(maxVersion[1], CultureInfo.InvariantCulture) : ushort.MaxValue;
        var targetMaximumPatch = maxVersion.Length > 2 && maxVersion[2] != "*" ? int.Parse(maxVersion[2], CultureInfo.InvariantCulture) : ushort.MaxValue;

        return new TargetToGenerate(signalType, integrationName, targetAssembly, targetType, targetMethod, targetMinimumMajor, targetMinimumMinor, targetMinimumPatch, targetMaximumMajor, targetMaximumMinor, targetMaximumPatch, targetSignatureTypesBuilder.ToString(), integrationKind);
    }

    private static string GenerateInstrumentationDefinitionsPartialClass(
        ImmutableArray<IntegrationToGenerate?> integrationClasses, int integrationKind)
    {
        var tracesByIntegrationName = new Dictionary<string, List<(string IntegrationType, TargetToGenerate Target)>>();
        var logsByIntegrationName = new Dictionary<string, List<(string IntegrationType, TargetToGenerate Target)>>();
        var metricsByIntegrationName = new Dictionary<string, List<(string IntegrationType, TargetToGenerate Target)>>();

        var instrumentationCount = 0;

        foreach (var integrationToGenerate in integrationClasses)
        {
            foreach (var targetToGenerate in integrationToGenerate!.Value.Targets.Where(t => t.IntegrationKind == integrationKind))
            {
                Dictionary<string, List<(string IntegrationType, TargetToGenerate Target)>> byName;
                switch (targetToGenerate.SignalType)
                {
                    case 0:
                        byName = tracesByIntegrationName;
                        break;
                    case 1:
                        byName = metricsByIntegrationName;
                        break;
                    case 2:
                        byName = logsByIntegrationName;
                        break;
                    default:
                        continue;
                }

                if (byName.TryGetValue(targetToGenerate.IntegrationName, out var value))
                {
                    value.Add((integrationToGenerate.Value.IntegrationType, targetToGenerate));
                }
                else
                {
                    byName.Add(
                        targetToGenerate.IntegrationName,
                        [(integrationToGenerate.Value.IntegrationType, targetToGenerate)]);
                }

                instrumentationCount++;
            }
        }

        var generatedMethodName = integrationKind == IntegrationKindDirect ? "GetDefinitionsArray" : "GetDerivedDefinitionsArray";

#pragma warning disable SA1118 // Parameter should not span multiple lines
        var sb = new StringBuilder()
            .AppendFormat(
                CultureInfo.InvariantCulture,
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
    private static NativeCallTargetDefinition[] {0}()
    {{
        var nativeCallTargetDefinitions = new List<NativeCallTargetDefinition>({1});",
                generatedMethodName,
                instrumentationCount)
            .AppendLine();
#pragma warning restore SA1118 // Parameter should not span multiple lines

        const string tracesHeader = @"        // Traces
        var tracerSettings = Instrumentation.TracerSettings.Value;
        if (tracerSettings.TracesEnabled)";
        GenerateInstrumentationForSignal(tracesByIntegrationName, sb, tracesHeader, "tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation");

        const string logsHeader = @"        // Logs
        var logSettings = Instrumentation.LogSettings.Value;
        if (logSettings.LogsEnabled)";
        GenerateInstrumentationForSignal(logsByIntegrationName, sb, logsHeader, "logSettings.EnabledInstrumentations.Contains(LogInstrumentation");

        const string metricsHeader = @"        // Metrics
        var metricSettings = Instrumentation.MetricSettings.Value;
        if (metricSettings.MetricsEnabled)";
        GenerateInstrumentationForSignal(metricsByIntegrationName, sb, metricsHeader, "metricSettings.EnabledInstrumentations.Contains(MetricInstrumentation");

        sb.AppendLine(@"        return nativeCallTargetDefinitions.ToArray();
    }
}");

        return sb.ToString();
    }

    private static void GenerateInstrumentationForSignal(Dictionary<string, List<(string IntegrationType, TargetToGenerate Target)>> integrations, StringBuilder sb, string signalHeader, string conditionPrefix)
    {
        if (integrations.Count > 0)
        {
            sb.AppendLine(signalHeader)
                .AppendLine("        {");
            GenerateIntegrations(integrations, sb, conditionPrefix);

            sb.AppendLine("        }")
                .AppendLine();
        }
    }

    private static void GenerateIntegrations(Dictionary<string, List<(string IntegrationType, TargetToGenerate Target)>> integrationsByName, StringBuilder sb, string conditionPrefix)
    {
        bool firstLine = true;
        foreach (var group in integrationsByName)
        {
            if (!firstLine)
            {
                sb.AppendLine();
            }
            else
            {
                firstLine = false;
            }

            sb.Append("            // ");
            sb.AppendLine(group.Key);
#pragma warning disable SA1118 // Parameter should not span multiple lines
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                @"            if ({0}.{1}))
            {{",
                conditionPrefix,
                group.Key);
#pragma warning restore SA1118 // Parameter should not span multiple lines
            sb.AppendLine();

            foreach (var integration in group.Value)
            {
                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "                nativeCallTargetDefinitions.Add(new(\"{0}\", \"{1}\", \"{2}\", [{3}], {4}, {5}, {6}, {7}, {8}, {9}, AssemblyFullName, \"{10}\"));",
                    integration.Target.Assembly,
                    integration.Target.Type,
                    integration.Target.Method,
                    integration.Target.SignatureTypes,
                    integration.Target.MinimumMajor,
                    integration.Target.MinimumMinor,
                    integration.Target.MinimumPatch,
                    integration.Target.MaximumMajor,
                    integration.Target.MaximumMinor,
                    integration.Target.MaximumPatch,
                    integration.IntegrationType);
                sb.AppendLine();
            }

            sb.AppendLine("            }");
        }
    }

    private static IntegrationToGenerate? GetClassesMarkedByInstrumentMethodAttribute(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var integrationType = classSymbol.ToDisplayString();

        var targets = new List<TargetToGenerate>();

        foreach (var contextAttribute in context.Attributes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targetToGenerate = CreateTargetToGenerate(contextAttribute);
            targets.Add(targetToGenerate);
        }

        return new IntegrationToGenerate(integrationType, new EquatableArray<TargetToGenerate>(targets.ToArray()));
    }
}
