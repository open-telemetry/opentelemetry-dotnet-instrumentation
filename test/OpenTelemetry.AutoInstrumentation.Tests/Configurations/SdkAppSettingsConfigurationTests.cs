// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Collections.Specialized;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

[Collection("Non-Parallel Collection")]
public sealed class SdkAppSettingsConfigurationTests
{
    private const string SamplerKey = "OTEL_TRACES_SAMPLER";

    [Fact]
    public void AppSettingsValueIsVisibleWhenEnvironmentVariableIsUnset()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?> { { SamplerKey, null } });

        var configuration = SdkAppSettingsConfiguration.Create(new NameValueCollection { { SamplerKey, "always_off" } });

        Assert.Equal("always_off", configuration[SamplerKey]);
        Assert.Null(Environment.GetEnvironmentVariable(SamplerKey));
    }

    [Fact]
    public void EnvironmentVariableTakesPrecedenceOverAppSettings()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?> { { SamplerKey, "always_on" } });

        var configuration = SdkAppSettingsConfiguration.Create(new NameValueCollection { { SamplerKey, "always_off" } });

        Assert.Equal("always_on", configuration[SamplerKey]);
    }

    [Fact]
    public void NonOtelAndProcessWideKeysAreExcluded()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>
        {
            { "SOME_OTHER_SETTING", null },
            { ConfigurationKeys.FailFast, null },
        });

        var configuration = SdkAppSettingsConfiguration.Create(new NameValueCollection
        {
            { "SOME_OTHER_SETTING", "value" },
            { ConfigurationKeys.FailFast, "true" },
        });

        Assert.Null(configuration["SOME_OTHER_SETTING"]);
        Assert.Null(configuration[ConfigurationKeys.FailFast]);
    }

    [Fact]
    public void SamplerFromAppSettingsSuppressesSampling()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?> { { SamplerKey, null } });

        var sourceName = $"{nameof(SdkAppSettingsConfigurationTests)}.{nameof(SamplerFromAppSettingsSuppressesSampling)}";
        var configuration = SdkAppSettingsConfiguration.Create(new NameValueCollection { { SamplerKey, "always_off" } });

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .ConfigureServices(services => services.AddSingleton(configuration))
            .Build();
        using var activitySource = new ActivitySource(sourceName);
        using var activity = activitySource.StartActivity("test");

        Assert.True(activity == null || !activity.Recorded);
    }
}
#endif
