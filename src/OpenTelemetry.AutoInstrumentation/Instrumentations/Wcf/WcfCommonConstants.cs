// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf;

internal static class WcfCommonConstants
{
#if NETFRAMEWORK
    public const string ServiceModelAssemblyName = "System.ServiceModel";
#else
    public const string ServiceModelAssemblyName = "System.Private.ServiceModel";

    public const string ServiceModelPrimitivesAssemblyName = "System.ServiceModel.Primitives";
    public const string Min6Version = "6.0.0";
    public const string Max10Version = "10.*.*";
#endif
    public const string Min4Version = "4.0.0";
    public const string Max4Version = "4.*.*";
}
