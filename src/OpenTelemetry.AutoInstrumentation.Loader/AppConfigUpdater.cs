// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace OpenTelemetry.AutoInstrumentation.Loader;

#pragma warning disable RS0016
#pragma warning restore RS0016

/// <summary>
/// Handles update of config files for non-default AppDomain
/// </summary>
public static class AppConfigUpdater
{
    /// <summary>
    /// Modify assembly bindings in appDomainSetup.
    /// Will be called through reflection when new <see cref="System.AppDomain"/> created.
    /// Call done using bytecode modifications for <see cref="AppDomain.CreateDomain(string,System.Security.Policy.Evidence,AppDomainSetup)"/>
    /// and <see cref="AppDomainManager.CreateDomainHelper(string,System.Security.Policy.Evidence,System.AppDomainSetup)"/>.
    /// </summary>
    /// <param name="appDomainSetup">appDomainSetup to be updated</param>
    public static void ModifyConfig(AppDomainSetup appDomainSetup)
    {
        appDomainSetup.LoaderOptimization = LoaderOptimization.SingleDomain;
    }
}
#endif
