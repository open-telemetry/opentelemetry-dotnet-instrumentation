// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.LegacySecurityPolicy.NetFramework;

#pragma warning disable CA1515 // Public type is required for cross-AppDomain activation.
public sealed class RemoteType : MarshalByRefObject
{
    public void Verify()
    {
        Console.WriteLine($"LegacyDomainType={GetType().FullName}");
        Console.WriteLine($"LegacyDomainIsHomogenous={AppDomain.CurrentDomain.IsHomogenous}");
        Console.WriteLine($"LegacyDomainIsFullyTrusted={AppDomain.CurrentDomain.IsFullyTrusted}");
        var loaderLoaded = AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
            assembly.FullName.StartsWith("OpenTelemetry.AutoInstrumentation.Loader,", StringComparison.Ordinal));
        Console.WriteLine($"LegacyDomainLoaderLoaded={loaderLoaded}");
    }
}
#pragma warning restore CA1515
