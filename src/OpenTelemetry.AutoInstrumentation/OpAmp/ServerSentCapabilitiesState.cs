// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal sealed class ServerSentCapabilitiesState
{
    private readonly object _lock = new();
    private ServerSentCapabilities _capabilities;

    public ServerSentCapabilities UpdateAndGetNewlyEnabled(ServerSentCapabilities capabilities)
    {
        // Zero means that the field was omitted, so retain the last complete nonzero bitmask.
        if (capabilities == ServerSentCapabilities.None)
        {
            return ServerSentCapabilities.None;
        }

        lock (_lock)
        {
            var newlyEnabled = capabilities & ~_capabilities;
            // A nonzero capabilities value replaces the complete previously reported set.
            _capabilities = capabilities;
            return newlyEnabled;
        }
    }

    public ServerSentCapabilities GetSnapshot()
    {
        lock (_lock)
        {
            return _capabilities;
        }
    }
}
