// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal interface IOpAmpReportSender
{
    void SendEffectiveConfig(IReadOnlyCollection<EffectiveConfigFile> files);

    void SendRemoteConfigStatus(RemoteConfigStatusReport statusReport);

    void SendFullStateReport(FullStateReport report);
}
