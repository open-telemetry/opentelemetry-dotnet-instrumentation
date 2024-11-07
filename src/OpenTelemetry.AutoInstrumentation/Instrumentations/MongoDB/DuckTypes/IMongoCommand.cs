// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

// wraps MongoDB command, for example a "find" or "insert" command
internal interface IMongoCommand : IDuckType
{
    string CommandName { get; }

    string CollectionName { get; }
}

// wraps MongoDB client or database connection
internal interface IMongoClientConnection : IDuckType
{
    string DatabaseName { get; }

    IServer? Server { get; }
}

// wraps MongoDB server, represents server information such as host and port
internal interface IServer : IDuckType
{
    string Host { get; }

    int Port { get; }
}

// wraps MongoDB collection, represents the collection within a MongoDB database
internal interface IMongoCollection : IDuckType
{
    string CollectionName { get; }

    IMongoClientConnection? ClientConnection { get; }
}

// wraps MongoDB result, which could contain result information or metadata from the database
internal interface IMongoResult : IDuckType
{
    bool Success { get; }

    int? ModifiedCount { get; }

    string? ErrorCode { get; }

    string? ErrorMessage { get; }
}
