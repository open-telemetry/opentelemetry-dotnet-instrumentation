// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// UnsafeAccessorTypeAttribute overrides should work starting from net10.0
#if NET10_0_OR_GREATER

using IntegrationTests.Helpers;

namespace IntegrationTests;

/// <summary>
/// Verifies that the native profiler rewrites assembly versions in UnsafeAccessorTypeAttribute metadata without
/// resolving or loading the assembly named by the attribute. It also verifies that incomplete unsafe-accessor
/// declarations are ignored.
/// </summary>
public class UnsafeAccessorTypeRedirectionTests(ITestOutputHelper output)
    : TestHelper("UnsafeAccessorTypeRedirection", output)
{
    /// <summary>
    /// Verifies that the native profiler adds a missing version, promotes a lower version, and preserves equal or
    /// higher versions in complete unsafe-accessor declarations.
    /// </summary>
    /// <param name="assemblyName">The assembly name embedded in the attribute.</param>
    /// <param name="compiledVersion">The assembly version embedded at build time, or <see langword="null"/> to omit it.</param>
    /// <param name="expectedVersion">The assembly version expected after the profiler processes the module.</param>
    [Theory]
    [Trait("Category", "EndToEnd")]
#if NET10_0
    [InlineData("Microsoft.Extensions.DependencyInjection.Abstractions", null, "10.0.0.0")]
    [InlineData("Microsoft.Extensions.DependencyInjection.Abstractions", "9.0.0.0", "10.0.0.0")]
    [InlineData("Microsoft.Extensions.DependencyInjection.Abstractions", "10.0.0.0", "10.0.0.0")]
    [InlineData("Microsoft.Extensions.DependencyInjection.Abstractions", "11.0.0.0", "11.0.0.0")]
#endif
    public void NativeRewriteAttributeMetadata(string assemblyName, string? compiledVersion, string expectedVersion)
    {
        EnableBytecodeInstrumentation();

        var packageVersion = compiledVersion ?? "null";
        var arguments = $"--assembly-name {assemblyName} --expected-version {expectedVersion}";

        RunTestApplication(new TestSettings { PackageVersion = packageVersion, Arguments = arguments });
    }

    /// <summary>
    /// Verifies that UnsafeAccessorTypeAttribute metadata is not rewritten when the declaring method does not also
    /// have UnsafeAccessorAttribute.
    /// </summary>
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void NoNativeRewriteOnIncompleteAttribute()
    {
        EnableBytecodeInstrumentation();

        var packageVersion = "1.0.0.0-incomplete";
        var arguments = $"--assembly-name Microsoft.Extensions.DependencyInjection.Abstractions --expected-version 1.0.0.0";

        RunTestApplication(new TestSettings { PackageVersion = packageVersion, Arguments = arguments });
    }
}
#endif
