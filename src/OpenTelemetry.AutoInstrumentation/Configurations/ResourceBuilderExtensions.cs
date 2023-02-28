// <copyright file="ResourceBuilderExtensions.cs" company="OpenTelemetry Authors">
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

using Microsoft.Extensions.Configuration;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class ResourceBuilderExtensions
{
    /// <summary>
    /// Adds resource attributes for service.name from ServiceNameDetector <see ref="ServiceNameDetector.cs"/>
    /// to a <see cref="ResourceBuilder"/> following the <a
    /// href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md#specifying-resource-information-via-an-environment-variable">Resource
    /// SDK</a>.
    /// </summary>
    /// <param name="resourceBuilder"><see cref="ResourceBuilder"/>.</param>
    /// <returns>Returns <see cref="ResourceBuilder"/> for chaining.</returns>
    public static ResourceBuilder AddServiceNameDetector(this ResourceBuilder resourceBuilder)
    {
        Lazy<IConfiguration> configuration = new Lazy<IConfiguration>(() => new ConfigurationBuilder().AddEnvironmentVariables().Build());

        return resourceBuilder
            .AddDetector(new ServiceNameDetector(configuration.Value));
    }
}
