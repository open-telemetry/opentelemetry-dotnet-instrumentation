// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.LegacySecurityPolicy.NetFramework;

internal static class Program
{
    public static void Main()
    {
        var setup = new AppDomainSetup
        {
            AppDomainManagerAssembly = typeof(CustomAppDomainManager).Assembly.FullName,
            AppDomainManagerType = typeof(CustomAppDomainManager).FullName
        };

        var domain = AppDomain.CreateDomain("LegacySecurityPolicy", null, setup);
        try
        {
            var remote = (RemoteType)domain.CreateInstanceAndUnwrap(typeof(RemoteType).Assembly.FullName, typeof(RemoteType).FullName);
            remote.Verify();
        }
        finally
        {
            AppDomain.Unload(domain);
        }
    }
}
