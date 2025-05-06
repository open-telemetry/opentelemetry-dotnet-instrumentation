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
    /// Modify assembly bindings in appDomainSetup
    /// </summary>
    /// <param name="appDomainSetup">appDomainSetup to be updated</param>
    public static void ModifyConfig(AppDomainSetup appDomainSetup)
    {
        appDomainSetup.LoaderOptimization = LoaderOptimization.SingleDomain;
    }
}
#endif
