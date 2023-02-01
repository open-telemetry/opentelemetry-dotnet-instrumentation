// <copyright file="Program.cs" company="OpenTelemetry Authors">
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

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using IntegrationsJsonGenerator;

const string instrumentMethodAttributeName = "OpenTelemetry.AutoInstrumentation.Instrumentations.InstrumentMethodAttribute";

var thisFilePath = GetSourceFilePathName();
var solutionFolder = Path.Combine(thisFilePath, "..", "..", "..");

var autoInstrumentationLibPath = Path.Combine(solutionFolder, "bin", "tracer-home", "net", "OpenTelemetry.AutoInstrumentation.dll");

var autoInstrumentationLib = Assembly.LoadFrom(autoInstrumentationLibPath);

var assemblyInstrumentMethodAttributes = autoInstrumentationLib.DefinedTypes
    .Where(type => InheritsFrom(type, instrumentMethodAttributeName)).Select(x => x.FullName);

var integrations = new Dictionary<(string IntegrationType, string IntegrationName), Integration>();
foreach (var typeInfo in autoInstrumentationLib.GetTypes())
{
    foreach (var attribute in typeInfo.GetCustomAttributes()
                 .Where(a => assemblyInstrumentMethodAttributes.Contains(a.GetType().FullName)))
    {
        var integration = ConvertToIntegration(typeInfo.FullName!, attribute);

        if (!integrations.ContainsKey((integration.IntegrationType, integration.IntegrationName)))
        {
            integrations.Add(
                (integration.IntegrationType, integration.IntegrationName),
                new Integration
                {
                    Name = integration.IntegrationName,
                    Type = integration.IntegrationType,
                    MethodReplacements = new List<MethodReplacement> { integration.MethodReplacement }
                });
        }
        else
        {
            var integration2 = integrations[(integration.IntegrationType, integration.IntegrationName)];
            integration2.MethodReplacements.Add(integration.MethodReplacement);
        }
    }
}

var productionIntegrations = integrations.Where(x => x.Key.IntegrationName != "StrongNamedValidation").Select(x => x.Value)
    .OrderBy(x => x.Name).ToArray();

var testIntegrations = integrations.Where(x => x.Key.IntegrationName == "StrongNamedValidation").Select(x => AppendMockIntegrations(x.Value))
    .OrderBy(x => x.Name).ToArray();

Dictionary<string, List<string>> byteCodeIntegrationsByType = new();

foreach (var (integrationType, integrationName) in integrations.Keys)
{
    if (byteCodeIntegrationsByType.TryGetValue(integrationType, out var integrationNames))
    {
        integrationNames.Add(integrationName);
    }
    else
    {
        byteCodeIntegrationsByType.Add(integrationType, new List<string> { integrationName });
    }
}

UpdateIntegrationFile(Path.Combine(solutionFolder, "integrations.json"), productionIntegrations);
UpdateIntegrationFile(Path.Combine(solutionFolder, "test", "IntegrationTests", "StrongNamedTestsIntegrations.json"), testIntegrations);
UpdateNativeInstrumentationFile(Path.Combine(solutionFolder, "src", "OpenTelemetry.AutoInstrumentation.Native", "bytecode_instrumentations.h"), byteCodeIntegrationsByType);

bool InheritsFrom(Type type, string baseType)
{
    while (true)
    {
        if (type.FullName == baseType)
        {
            return true;
        }

        if (type.BaseType is null)
        {
            return false;
        }

        type = type.BaseType;
    }
}

(string IntegrationType, string IntegrationName, MethodReplacement MethodReplacement) ConvertToIntegration(string wrapperTypeName, Attribute attribute)
{
    var integrationName = GetPropertyValue<string>("IntegrationName", attribute);
    var integrationType = GetPropertyValue<object>("Type", attribute).ToString()!;

    var methodReplacement = new MethodReplacement
    {
        Wrapper =
                {
                    Type = wrapperTypeName
                },
        Target =
                {
                    Assembly = GetPropertyValue<string>("AssemblyName", attribute), Type = GetPropertyValue<string>("TypeName", attribute),
                    Method = GetPropertyValue<string>("MethodName", attribute)
                }
    };

    var returnTypeName = GetPropertyValue<string>("ReturnTypeName", attribute);
    var parameterTypeNames = GetPropertyValue<string[]>("ParameterTypeNames", attribute);
    methodReplacement.Target.SignatureTypes = new[] { returnTypeName }.Concat(parameterTypeNames).ToArray();

    var minVersion = GetPropertyValue<string>("MinimumVersion", attribute).Split('.');
    methodReplacement.Target.MinimumMajor = int.Parse(minVersion[0]);
    if (minVersion.Length > 1 && minVersion[1] != "*")
    {
        methodReplacement.Target.MinimumMinor = int.Parse(minVersion[1]);
    }

    if (minVersion.Length > 2 && minVersion[2] != "*")
    {
        methodReplacement.Target.MinimumPath = int.Parse(minVersion[2]);
    }

    var maxVersion = GetPropertyValue<string>("MaximumVersion", attribute).Split('.');
    methodReplacement.Target.MaximumMajor = int.Parse(maxVersion[0]);
    if (maxVersion.Length > 1 && maxVersion[1] != "*")
    {
        methodReplacement.Target.MaximumMinor = int.Parse(maxVersion[1]);
    }

    if (maxVersion.Length > 2 && maxVersion[2] != "*")
    {
        methodReplacement.Target.MaximumPath = int.Parse(maxVersion[2]);
    }

    return (integrationType, integrationName, methodReplacement);
}

T GetPropertyValue<T>(string propertyName, Attribute attribute)
{
    var type = attribute.GetType();
    var getMethod = type.GetProperty(propertyName)!.GetGetMethod()!;

    if (!getMethod.ReturnType.IsAssignableTo(typeof(T)))
    {
        throw new ArgumentException($"Property {propertyName} is not assignable to {typeof(T)}");
    }

    var value = getMethod.Invoke(attribute, Array.Empty<object>())!;

    return (T)value;
}

void UpdateIntegrationFile(string filePath, Integration[] productionIntegrations1)
{
    using var fileStream = new FileStream(filePath, FileMode.Truncate);

    var jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    JsonSerializer.Serialize(fileStream, productionIntegrations1, jsonSerializerOptions);
}

void UpdateNativeInstrumentationFile(string filePath, Dictionary<string, List<string>> bytecodeIntegrations)
{
    using var fileStream = new FileStream(filePath, FileMode.Truncate);

    using var writer = new StreamWriter(fileStream);
    writer.WriteLine($"// Auto-generated file, do not change it - generated by the {nameof(IntegrationsJsonGenerator)}");
    writer.WriteLine("#ifndef BYTECODE_INSTRUMENTATIONS_H");
    writer.WriteLine("#define BYTECODE_INSTRUMENTATIONS_H");
    writer.WriteLine();
    writer.WriteLine("#include \"string.h\"");
    writer.WriteLine();
    writer.WriteLine("namespace trace");
    writer.WriteLine("{");
    foreach (var bytecodeIntegration in bytecodeIntegrations)
    {
        writer.Write("inline std::unordered_map<WSTRING, WSTRING> ");
        writer.Write(bytecodeIntegration.Key.ToLowerInvariant());
        writer.Write("_integration_names = {");
        writer.Write(string.Join(", ", bytecodeIntegration.Value.Select(name => $"{{WStr(\"{name}\"), WStr(\"OTEL_DOTNET_AUTO_{bytecodeIntegration.Key.ToUpperInvariant()}S_{name}_INSTRUMENTATION_ENABLED\")}}")));
        writer.WriteLine("};");
    }

    writer.WriteLine("}");
    writer.WriteLine("#endif");
}

static string GetSourceFilePathName([CallerFilePath] string? callerFilePath = null)
    => callerFilePath ?? string.Empty;

static Integration AppendMockIntegrations(Integration testIntegration)
{
    // Add some special cases used by the integration tests. This way the integrations
    // file used in the integrations test doesn't change on each run of the tool.
    var targetAssembly = testIntegration.MethodReplacements[0].Target.Assembly;
    var targetType = testIntegration.MethodReplacements[0].Target.Type;
    var targetSignatureTypes = testIntegration.MethodReplacements[0].Target.SignatureTypes;

    testIntegration.MethodReplacements.Add(new MethodReplacement
    {
        Target = new Target
        {
            Assembly = targetAssembly,
            Type = targetType,
            Method = "InstrumentationTargetMissingBytecodeInstrumentationType",
            SignatureTypes = targetSignatureTypes,
            MaximumMajor = 1,
            MinimumMajor = 1,
        },
        Wrapper = new Wrapper
        {
            Type = "OpenTelemetry.AutoInstrumentation.Instrumentations.Validations.MissingInstrumentationType",
        },
    });

    testIntegration.MethodReplacements.Add(new MethodReplacement
    {
        Target = new Target
        {
            Assembly = testIntegration.MethodReplacements[0].Target.Assembly,
            Type = targetType,
            Method = "InstrumentationTargetMissingBytecodeInstrumentationMethod",
            SignatureTypes = targetSignatureTypes,
            MaximumMajor = 1,
            MinimumMajor = 1,
        },
        Wrapper = new Wrapper
        {
            Type = "OpenTelemetry.AutoInstrumentation.DuckTyping.DuckAttribute",
        },
    });

    return testIntegration;
}
