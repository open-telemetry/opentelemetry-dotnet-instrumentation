// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal interface ICustomCapabilitiesSink
{
    void SubmitCapabilities(IReadOnlyCollection<string> capabilities);

    void SubmitFullState(FullStateReport report);

    void SendMessage(string capability, string type, ReadOnlyMemory<byte> data);
}
