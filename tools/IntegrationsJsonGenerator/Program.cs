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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;

const string instrumentMethodAttributeName = "OpenTelemetry.AutoInstrumentation.Instrumentations.InstrumentMethodAttribute";

var thisFilePath = GetSourceFilePathName();
var solutionFolder = Path.Combine(thisFilePath, "..", "..", "..");

var autoInstrumentationLibPath = Path.Combine(solutionFolder, "bin", "tracer-home", "netcoreapp3.1", "OpenTelemetry.AutoInstrumentation.dll");

var autoInstrumentationLib = Assembly.LoadFrom(autoInstrumentationLibPath);

var assemblyInstrumentMethodAttributes = autoInstrumentationLib.DefinedTypes
    .Where(type => InheritsFrom(type, instrumentMethodAttributeName)).Select(x => x.FullName);

var integrations = new Dictionary<string, Integration>();
foreach (var typeInfo in autoInstrumentationLib.GetTypes())
{
    foreach (var attribute in typeInfo.GetCustomAttributes()
                 .Where(a => assemblyInstrumentMethodAttributes.Contains(a.GetType().FullName)))
    {
        var integration = ConvertToIntegration(typeInfo.FullName, attribute);

        if (!integrations.ContainsKey(integration.IntegrationName))
        {
            integrations.Add(
                integration.IntegrationName,
                new Integration
                {
                    Name = integration.IntegrationName,
                    Type = integration.IntegartionType,
                    MethodReplacements = new List<MethodReplacement> { integration.MethodReplacement }
                });
        }
        else
        {
            var integration2 = integrations[integration.IntegrationName];
            integration2.MethodReplacements.Add(integration.MethodReplacement);
        }
    }
}

var productionIntegrations = integrations.Where(x => x.Key != "StrongNamedValidation").Select(x => x.Value)
    .OrderBy(x => x.Name).ToArray();

var testIntegrations = integrations.Where(x => x.Key == "StrongNamedValidation").Select(x => x.Value)
    .OrderBy(x => x.Name).ToArray();

UpdateIntegrationFile(Path.Combine(solutionFolder, "integrations.json"), productionIntegrations);
UpdateIntegrationFile(Path.Combine(solutionFolder, "test", "IntegrationTests", "StrongNamedTestsIntegrations.json"), testIntegrations);

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

(string IntegartionType, string IntegrationName, MethodReplacement MethodReplacement) ConvertToIntegration(string wrapperTypeName, Attribute attribute)
{
    var integrationName = GetPropertyValue<string>("IntegrationName", attribute);
    var integrationType = GetPropertyValue<object>("Type", attribute).ToString();

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
    var getMethod = type.GetProperty(propertyName).GetGetMethod();

    if (!getMethod.ReturnType.IsAssignableTo(typeof(T)))
    {
        throw new ArgumentException($"Property {propertyName} is not assignable to {typeof(T)}");
    }

    var value = getMethod.Invoke(attribute, Array.Empty<object>());

    return (T)value;
}

void UpdateIntegrationFile(string filePath, Integration[] productionIntegrations1)
{
    using var fileStream = new FileStream(filePath, FileMode.Truncate);

    var jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    JsonSerializer.Serialize(fileStream, productionIntegrations1, jsonSerializerOptions);
}

static string GetSourceFilePathName([CallerFilePath] string callerFilePath = null)
    => callerFilePath ?? string.Empty;
