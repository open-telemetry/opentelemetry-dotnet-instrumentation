// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Service;

internal static class WcfServiceConstants
{
    public const string IntegrationName = nameof(TracerInstrumentation.WcfService);
    public const string ServiceHostBaseTypeName = "System.ServiceModel.ServiceHostBase";
    public const string InitializeDescriptionMethodName = "InitializeDescription";
    public const string UriSchemeKeyedCollectionTypeName = "System.ServiceModel.UriSchemeKeyedCollection";
}
#endif
