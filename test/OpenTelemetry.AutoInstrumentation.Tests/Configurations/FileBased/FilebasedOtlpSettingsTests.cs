// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Configurations.Otlp;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedOtlpSettingsTests
{
    [Fact]
    public void HeadersList_ValueContainingEqualsSign_IsPreserved()
    {
        // '=' is a valid W3C Baggage value character, e.g. in base64 padded credentials.
        var config = new OtlpHttpExporterConfig { HeadersList = "Authorization=Basic dXNlcjpwYXNzd29yZA==" };

        var settings = new OtlpSettings(OtlpSignalType.Traces, config);

        Assert.Equal("Authorization=Basic dXNlcjpwYXNzd29yZA==", settings.Headers);
    }

    [Fact]
    public void HeadersList_WhitespaceAroundSeparators_IsTrimmed()
    {
        var config = new OtlpHttpExporterConfig { HeadersList = "api-key = 1234, x-tenant=acme" };

        var settings = new OtlpSettings(OtlpSignalType.Traces, config);

        Assert.Equal("api-key=1234,x-tenant=acme", settings.Headers);
    }

    [Fact]
    public void HeadersList_EntryWithoutSeparator_IsSkipped()
    {
        var config = new OtlpHttpExporterConfig { HeadersList = "api-key,x-tenant=acme" };

        var settings = new OtlpSettings(OtlpSignalType.Traces, config);

        Assert.Equal("x-tenant=acme", settings.Headers);
    }

    [Fact]
    public void Headers_TakePrecedenceOverHeadersList_ForTheSameKey()
    {
        var config = new OtlpHttpExporterConfig
        {
            HeadersList = " api-key=fromHeadersList",
            Headers = [new Header { Name = "api-key", Value = "fromHeaders" }]
        };

        var settings = new OtlpSettings(OtlpSignalType.Traces, config);

        Assert.Equal("api-key=fromHeaders", settings.Headers);
    }
}
