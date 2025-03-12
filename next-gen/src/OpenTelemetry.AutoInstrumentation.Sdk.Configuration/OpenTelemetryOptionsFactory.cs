// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace OpenTelemetry.Configuration;

internal sealed class OpenTelemetryOptionsFactory : OptionsFactory<OpenTelemetryOptions>
{
    private readonly IConfigurationSection _Configuration;

    public OpenTelemetryOptionsFactory(
        IConfigurationSection configuration,
        IEnumerable<IConfigureOptions<OpenTelemetryOptions>> setups,
        IEnumerable<IPostConfigureOptions<OpenTelemetryOptions>> postConfigures,
        IEnumerable<IValidateOptions<OpenTelemetryOptions>> validations)
        : base(setups, postConfigures, validations)
    {
        _Configuration = configuration;
    }

    protected override OpenTelemetryOptions CreateInstance(string name)
    {
        IConfigurationSection config = _Configuration;

        return new(
            OpenTelemetryResourceOptions.ParseFromConfig(config.GetSection("Resource")),
            OpenTelemetryLoggingOptions.ParseFromConfig(config.GetSection("Logs")),
            OpenTelemetryMetricsOptions.ParseFromConfig(config.GetSection("Metrics")),
            OpenTelemetryTracingOptions.ParseFromConfig(config.GetSection("Traces")),
            OpenTelemetryExporterOptions.ParseFromConfig(config.GetSection("Exporter")));
    }
}
