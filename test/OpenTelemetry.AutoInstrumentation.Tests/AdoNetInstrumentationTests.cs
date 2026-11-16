// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data.Common;
using System.Diagnostics;
using NSubstitute;
using OpenTelemetry.AutoInstrumentation.Instrumentations.AdoNet;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class AdoNetInstrumentationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StartActivityReadsConnectionOnce(bool connectionUnsupported)
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == "OpenTelemetry.AutoInstrumentation.AdoNet",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        using var parent = new Activity("parent");
        parent.SetIdFormat(ActivityIdFormat.W3C);
        parent.Start();
        using var connection = Substitute.For<DbConnection>();
        connection.Database.Returns("FakeDatabase");
        connection.DataSource.Returns("tcp:FakeServer,5433");
        using var command = Substitute.For<DbCommand>();
        command.CommandText.Returns("SELECT 1");
        var connectionReads = 0;
        command.Connection.Returns(_ =>
        {
            connectionReads++;
            if (connectionUnsupported)
            {
                throw new NotSupportedException();
            }

            return connection;
        });

        using var activity = AdoNetInstrumentation.StartActivity(command, "ExecuteScalarAsync");

        Assert.NotNull(activity);
        Assert.Equal(1, connectionReads);
        if (connectionUnsupported)
        {
            Assert.Null(activity.GetTagItem("db.namespace"));
            Assert.Null(activity.GetTagItem("server.address"));
            Assert.Null(activity.GetTagItem("server.port"));
        }
        else
        {
            Assert.Equal("FakeDatabase", activity.GetTagItem("db.namespace"));
            Assert.Equal("FakeServer", activity.GetTagItem("server.address"));
            Assert.Equal(5433, activity.GetTagItem("server.port"));
        }
    }
}
