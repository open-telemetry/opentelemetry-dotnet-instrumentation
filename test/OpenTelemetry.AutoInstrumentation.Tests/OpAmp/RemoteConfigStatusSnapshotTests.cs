// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

public class RemoteConfigStatusSnapshotTests
{
    [Fact]
    public void CopiesStatusAndComparesAllProperties()
    {
        var mutableHash = new byte[] { 1, 2, 3 };
        var report = new RemoteConfigStatusReport(mutableHash, RemoteConfigStatusCode.Applied);
        var snapshot = RemoteConfigStatusSnapshot.Create(report);
        mutableHash[0] = 9;

        var equivalent = RemoteConfigStatusSnapshot.Create(
            new RemoteConfigStatusReport(new byte[] { 1, 2, 3 }, RemoteConfigStatusCode.Applied));
        var otherHash = RemoteConfigStatusSnapshot.Create(
            new RemoteConfigStatusReport(new byte[] { 1, 2, 4 }, RemoteConfigStatusCode.Applied));
        var otherStatus = RemoteConfigStatusSnapshot.Create(
            new RemoteConfigStatusReport(new byte[] { 1, 2, 3 }, RemoteConfigStatusCode.Applying));
        var failed = RemoteConfigStatusSnapshot.Create(
            new RemoteConfigStatusReport(new byte[] { 1, 2, 3 }, RemoteConfigStatusCode.Failed, "failure"));
        var equivalentFailure = RemoteConfigStatusSnapshot.Create(
            new RemoteConfigStatusReport(new byte[] { 1, 2, 3 }, RemoteConfigStatusCode.Failed, "failure"));
        var otherError = RemoteConfigStatusSnapshot.Create(
            new RemoteConfigStatusReport(new byte[] { 1, 2, 3 }, RemoteConfigStatusCode.Failed, "other"));

        Assert.True(snapshot.HasSameContent(equivalent));
        Assert.False(snapshot.HasSameContent(otherHash));
        Assert.False(snapshot.HasSameContent(otherStatus));
        Assert.False(snapshot.HasSameContent(failed));
        Assert.True(failed.HasSameContent(equivalentFailure));
        Assert.False(failed.HasSameContent(otherError));
        Assert.NotSame(report, snapshot.Report);
        Assert.Equal(new byte[] { 1, 2, 3 }, snapshot.Report.LastRemoteConfigHash.ToArray());
    }
}
