// <copyright file="GraphQLCommon.cs" company="OpenTelemetry Authors">
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

using System;
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.GraphQL;

internal class GraphQLCommon
{
    internal const string GraphQLAssembly = "GraphQL";
    internal const string Major2 = "2";
    internal const string Major2Minor3 = "2.3";

    internal const string IntegrationName = nameof(TracerInstrumentation.GraphQL);

    internal static readonly ActivitySource ActivitySource = new ActivitySource(
        "OpenTelemetry.AutoInstrumentation.GraphQL", Constants.Tracer.Version);

    private static readonly ILogger Log = OtelLogging.GetLogger();

    internal static Activity CreateActivityFromExecuteAsync(IExecutionContext executionContext)
    {
        Activity activity = null;
        InstrumentationOptions options = Instrumentation.TracerSettings.Value.InstrumentationOptions;

        try
        {
            string query = executionContext.Document.OriginalQuery;
            string operationName = executionContext.Operation.Name;
            string operationType = executionContext.Operation.OperationType.ToString().ToLowerInvariant();
            string operation = GetOperation(operationName, operationType);

            var tags = new GraphQLTags
            {
                OperationName = operationName,
                OperationType = operationType
            };

            if (options.GraphQLSetDocument)
            {
                tags.Document = query;
            }

            activity = ActivitySource.StartActivityWithTags(operation, ActivityKind.Server, tags);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating or populating scope.");
        }

        return activity;
    }

    internal static void RecordExecutionErrorsIfPresent(Activity activity, IExecutionErrors executionErrors)
    {
        var errorCount = executionErrors?.Count ?? 0;

        if (errorCount > 0)
        {
            for (int i = 0; i < errorCount; i++)
            {
                Exception ex = executionErrors[i].InnerException;

                if (ex != null)
                {
                    activity.SetException(ex);
                }
            }
        }
    }

    private static string GetOperation(string operationName, string operationType)
    {
        bool hasOperationType = !string.IsNullOrWhiteSpace(operationType);
        bool hasOperationName = !string.IsNullOrWhiteSpace(operationName);

        if (hasOperationType && hasOperationName)
        {
            return $"{operationType} {operationName}";
        }
        else if (hasOperationType)
        {
            return operationType;
        }

        return "GraphQL Operation";
    }
}
