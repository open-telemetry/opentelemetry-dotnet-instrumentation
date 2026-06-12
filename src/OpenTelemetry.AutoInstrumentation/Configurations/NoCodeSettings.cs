// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Logging;
using static OpenTelemetry.AutoInstrumentation.InstrumentationDefinitions;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class NoCodeSettings : Settings
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    public bool Enabled { get; set; }

    public List<NoCodeInstrumentedMethod> InstrumentedMethods { get; set; } = [];

    public Payload GetDirectPayload()
    {
        return new Payload
        {
            // Fixed Id for definitions payload (to avoid loading same integrations from multiple AppDomains)
            DefinitionsId = "D3B88A224E034D60AC3A923BABEE6B7F",
            Definitions = [.. InstrumentedMethods.Select(x => x.Definition)],
        };
    }

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        // Intentionally empty. NoCodeSettings are only loaded from file-based configuration.
    }

    protected override void OnLoadFile(YamlConfiguration configuration)
    {
        var noCodeConfiguration = configuration.InstrumentationDevelopment?.DotNet?.NoCode;

        if (noCodeConfiguration is null)
        {
            Log.Debug("NoCode configuration section not found, no-code instrumentation will be disabled.");
            return;
        }

        if (noCodeConfiguration.Targets == null || noCodeConfiguration.Targets.Length == 0)
        {
            Log.Debug("NoCode targets section no found, or it is empty, no-code instrumentation will be disabled.");
            return;
        }

        var noCodeEntries = noCodeConfiguration.Targets;

        var instrumentedMethods = new List<NoCodeInstrumentedMethod>(noCodeEntries.Length);

        foreach (var noCodeEntry in noCodeEntries)
        {
            var noCodeTarget = noCodeEntry.Target;

            if (noCodeTarget == null)
            {
                Log.Debug("No code target option is null. Skipping this entry.");
                continue;
            }

            if (noCodeTarget.Assembly == null)
            {
                Log.Debug("No code target assembly is null. Skipping this entry.");
                continue;
            }

            if (string.IsNullOrEmpty(noCodeTarget.Assembly.Name))
            {
                Log.Debug("No code target assembly name is null or empty. Skipping this entry.");
                continue;
            }

            if (string.IsNullOrEmpty(noCodeTarget.Method))
            {
                Log.Debug("No code target method is null or empty. Skipping this entry.");
                continue;
            }

            if (string.IsNullOrEmpty(noCodeTarget.Type))
            {
                Log.Debug("No code target type is null or empty. Skipping this entry.");
                continue;
            }

            if (noCodeTarget.Signature == null)
            {
                Log.Debug("No code target signature is null. Skipping this entry.");
                continue;
            }

            if (string.IsNullOrEmpty(noCodeTarget.Signature.ReturnType))
            {
                Log.Debug("No code target signature return type is null or empty. Skipping this entry.");
                continue;
            }

            if (noCodeTarget.Signature.ParameterTypes is { Length: > 9 })
            {
                Log.Debug("No code target signature parameters contains more than 9 elements. It is not supported. Skipping this entry.");
                continue;
            }

            if (noCodeEntry.Span == null)
            {
                Log.Debug("No code span is null. Skipping this entry.");
                continue;
            }

            if (string.IsNullOrEmpty(noCodeEntry.Span.Name))
            {
                Log.Debug("No code span name is null or empty. Skipping this entry.");
                continue;
            }

            string[] targetSignatureTypes;
            int parametersCount;
            if (noCodeTarget.Signature.ParameterTypes == null || noCodeTarget.Signature.ParameterTypes.Length == 0)
            {
                targetSignatureTypes = [noCodeTarget.Signature.ReturnType!];
                parametersCount = 0;
            }
            else
            {
                targetSignatureTypes = new string[noCodeTarget.Signature.ParameterTypes.Length + 1];
                targetSignatureTypes[0] = noCodeTarget.Signature.ReturnType!;
                parametersCount = noCodeTarget.Signature.ParameterTypes.Length;
                Array.Copy(noCodeTarget.Signature.ParameterTypes, 0, targetSignatureTypes, 1, noCodeTarget.Signature.ParameterTypes.Length);
            }

            var definition = new NativeCallTargetDefinition(
                noCodeTarget.Assembly.Name!,
                noCodeTarget.Type!,
                noCodeTarget.Method!,
                targetSignatureTypes,
                0,
                0,
                0,
                ushort.MaxValue,
                ushort.MaxValue,
                ushort.MaxValue,
                AssemblyFullName,
                $"OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.NoCodeIntegration{parametersCount}");

            var activityKind = ParseActivityKind(noCodeEntry.Span.Kind);
            var attributes = noCodeEntry.Span.ParseAttributes();
            var dynamicAttributes = noCodeEntry.Span.ParseDynamicAttributes();
            var statusRules = noCodeEntry.Span.ParseStatusRules();
            var dynamicSpanName = noCodeEntry.Span.ParseDynamicSpanName();

            Log.Debug($"NoCode adding instrumentation for assembly: '{noCodeTarget.Assembly.Name}', type: '{noCodeTarget.Type}', method: '{noCodeTarget.Method}' with signature: '{string.Join(",", targetSignatureTypes)}'");

            instrumentedMethods.Add(new NoCodeInstrumentedMethod(definition, targetSignatureTypes, noCodeEntry.Span.Name!, activityKind, attributes, dynamicAttributes, statusRules, dynamicSpanName));
        }

        if (instrumentedMethods.Count > 0)
        {
            Enabled = true;
            InstrumentedMethods = instrumentedMethods;
        }
    }

    private static ActivityKind ParseActivityKind(string? kindString)
    {
        return kindString switch
        {
            "internal" => ActivityKind.Internal,
            "server" => ActivityKind.Server,
            "client" => ActivityKind.Client,
            "producer" => ActivityKind.Producer,
            "consumer" => ActivityKind.Consumer,
            _ => ActivityKind.Internal // Default for unknown values
        };
    }
}
