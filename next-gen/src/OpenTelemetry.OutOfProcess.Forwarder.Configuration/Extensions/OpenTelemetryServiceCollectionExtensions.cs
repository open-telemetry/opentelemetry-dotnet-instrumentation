// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Configuration;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Contains OpenTelemetry configuration extensions.
/// </summary>
public static class OpenTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Configure <see cref="OpenTelemetryOptions"/> using the <c>OpenTelemetry</c> <see cref="IConfigurationSection"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>.</param>
    /// <returns>Supplied <see cref="IServiceCollection"/> for call chaining.</returns>
    public static IServiceCollection ConfigureOpenTelemetry(
        this IServiceCollection services)
        => ConfigureOpenTelemetry(services, configurationSectionName: "OpenTelemetry");

    /// <summary>
    /// Configure <see cref="OpenTelemetryOptions"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>.</param>
    /// <param name="configurationSectionName">Configuration section to bind to <see cref="OpenTelemetryOptions"/>.</param>
    /// <returns>Supplied <see cref="IServiceCollection"/> for call chaining.</returns>
    public static IServiceCollection ConfigureOpenTelemetry(
        this IServiceCollection services,
        string configurationSectionName)
    {
        ArgumentException.ThrowIfNullOrEmpty(configurationSectionName);

        services.TryAddSingleton<IOptionsFactory<OpenTelemetryOptions>>(sp =>
        {
            IConfiguration config = sp.GetRequiredService<IConfiguration>();

            return new OpenTelemetryOptionsFactory(
                config.GetSection(configurationSectionName),
                sp.GetServices<IConfigureOptions<OpenTelemetryOptions>>(),
                sp.GetServices<IPostConfigureOptions<OpenTelemetryOptions>>(),
                sp.GetServices<IValidateOptions<OpenTelemetryOptions>>());
        });

        services.AddOptions<OpenTelemetryOptions>().ValidateDataAnnotations();

        return services;
    }
}
