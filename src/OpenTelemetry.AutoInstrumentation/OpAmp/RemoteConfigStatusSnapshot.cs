// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal sealed class RemoteConfigStatusSnapshot
{
    private RemoteConfigStatusSnapshot(RemoteConfigStatusReport report)
    {
        Report = report;
    }

    public RemoteConfigStatusReport Report { get; }

    public static RemoteConfigStatusSnapshot Create(RemoteConfigStatusReport report)
    {
        return new RemoteConfigStatusSnapshot(
            new RemoteConfigStatusReport(report.LastRemoteConfigHash.Span, report.Status, report.ErrorMessage));
    }

    public bool HasSameContent(RemoteConfigStatusSnapshot other)
    {
        return Report.Status == other.Report.Status &&
               string.Equals(Report.ErrorMessage, other.Report.ErrorMessage, StringComparison.Ordinal) &&
               Report.LastRemoteConfigHash.Span.SequenceEqual(other.Report.LastRemoteConfigHash.Span);
    }
}
