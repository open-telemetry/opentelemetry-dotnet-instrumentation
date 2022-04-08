// <copyright file="PluginsConfigurationHelper.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

internal static class PluginsConfigurationHelper
{
    public static TracerProviderBuilder InvokePlugins(this TracerProviderBuilder builder, IEnumerable<string> pluginsAssemblyQualifiedNames)
    {
        foreach (var assemblyQualifiedName in pluginsAssemblyQualifiedNames)
        {
            builder = builder.InvokePlugin(assemblyQualifiedName);
        }

        return builder;
    }

    private static TracerProviderBuilder InvokePlugin(this TracerProviderBuilder builder, string pluginAssemblyQualifiedName)
    {
        const string configureTracerProviderMethodName = "ConfigureTracerProvider";

        // get the type and method
        var t = Type.GetType(pluginAssemblyQualifiedName, throwOnError: true);
        var mi = t.GetMethod(configureTracerProviderMethodName, new Type[] { typeof(TracerProviderBuilder) });
        if (mi is null)
        {
            throw new MissingMethodException(t.Name, configureTracerProviderMethodName);
        }

        // execute
        var obj = Activator.CreateInstance(t);
        var result = mi.Invoke(obj, new object[] { builder });
        return (TracerProviderBuilder)result;
    }
}
