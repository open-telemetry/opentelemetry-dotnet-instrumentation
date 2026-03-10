// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// Handles update of config files for non-default AppDomain
/// </summary>
internal static class AppConfigUpdater
{
    /// <summary>
    /// Modify assembly bindings in appDomainSetup.
    /// Will be called through reflection when new <see cref="System.AppDomain"/> created.
    /// Call done using bytecode modifications for <see cref="AppDomain.CreateDomain(string,System.Security.Policy.Evidence,System.AppDomainSetup)"/>
    /// and <see cref="AppDomainManager.CreateDomainHelper(string,System.Security.Policy.Evidence,System.AppDomainSetup)"/>.
    /// </summary>
    /// <param name="appDomainSetup">appDomainSetup to be updated</param>
    public static void ModifyConfig(AppDomainSetup appDomainSetup)
    {
        appDomainSetup.LoaderOptimization = LoaderOptimization.SingleDomain;
    }
}
#endif
