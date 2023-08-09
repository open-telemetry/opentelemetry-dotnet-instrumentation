// <copyright file="MySqlDataCommon.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#if NET6_0_OR_GREATER

using System.Data;
using System.Diagnostics;
using OpenTelemetry.Instrumentation.MySqlData;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MySqlData;

internal class MySqlDataCommon
{
    internal const string DbName = "db.name";
    internal const string DbSystem = "db.system";
    internal const string DbStatement = "db.statement";
    internal const string DbUser = "db.user";

    internal const string NetPeerIp = "net.peer.ip";
    internal const string NetPeerPort = "net.peer.port";
    internal const string NetPeerName = "net.peer.name";

    internal const string PeerService = "peer.service";

    internal const string MysqlDatabaseSystemName = "mysql";
    internal static readonly ActivitySource ActivitySource = new("OpenTelemetry.AutoInstrumentation.MySqlData", AutoInstrumentationVersion.Version);

    internal static readonly IEnumerable<KeyValuePair<string, object?>> CreationTags = new[]
    {
        new KeyValuePair<string, object?>(DbSystem, MysqlDatabaseSystemName),
    };

    // Store the MySqlDataInstrumentationOptions that corresponds to the most recent MySqlDataInstrumentation instance,
    // since each new MySqlDataInstrumentation replaces previous instances
    internal static OpenTelemetry.Instrumentation.MySqlData.MySqlDataInstrumentationOptions? MySqlDataInstrumentationOptions { get; set; }

    internal static Activity? CreateActivity<TCommand>(TCommand command)
        where TCommand : IMySqlCommand
    {
        var activity = ActivitySource.StartActivity("OpenTelemetry.Instrumentation.MySqlData.Execute", ActivityKind.Client, Activity.Current?.Context ?? default, CreationTags);
        if (activity is null)
        {
            return null;
        }

        if (activity.IsAllDataRequested)
        {
            // Figure out how to get the options, if possible
            if (MySqlDataInstrumentationOptions is not null && MySqlDataInstrumentationOptions.SetDbStatement)
            {
                activity.SetTag(DbStatement, command.CommandText);
            }

            if (command.Connection?.Settings is not null)
            {
                activity.DisplayName = command.Connection.Settings.Database;
                activity.SetTag(DbName, command.Connection.Settings.Database);

                AddConnectionLevelDetailsToActivity(command.Connection.Settings, activity);
            }
        }

        return activity;
    }

    internal static void StopActivity(Activity activity)
    {
        if (activity.Source != ActivitySource)
        {
            return;
        }

        activity.Stop();
    }

    private static void AddConnectionLevelDetailsToActivity(IMySqlConnectionStringBuilder dataSource, Activity activity)
    {
        // Figure out how to get the options, if possible
        if (MySqlDataInstrumentationOptions is null || !MySqlDataInstrumentationOptions.EnableConnectionLevelAttributes)
        {
            activity.SetTag(PeerService, dataSource.Server);
        }
        else
        {
            var uriHostNameType = Uri.CheckHostName(dataSource.Server);

            if (uriHostNameType == UriHostNameType.IPv4 || uriHostNameType == UriHostNameType.IPv6)
            {
                activity.SetTag(NetPeerIp, dataSource.Server);
            }
            else
            {
                activity.SetTag(NetPeerName, dataSource.Server);
            }

            activity.SetTag(NetPeerPort, dataSource.Port);
            activity.SetTag(DbUser, dataSource.UserID);
        }
    }
}
#endif
