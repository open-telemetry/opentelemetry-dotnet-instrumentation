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
using System.Text;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Tagging;
using OpenTelemetry.AutoInstrumentation.Util;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.GraphQL;

internal class GraphQLCommon
{
    internal const string GraphQLAssembly = "GraphQL";
    internal const string Major2 = "2";
    internal const string Major2Minor3 = "2.3";

    internal const string IntegrationName = nameof(TracerInstrumentation.GraphQL);
    internal static readonly IntegrationInfo IntegrationId = IntegrationRegistry.GetIntegrationInfo(IntegrationName);

    internal static readonly ActivitySource ActivitySource = new ActivitySource(
        "OpenTelemetry.AutoInstrumentation.GraphQL", Constants.Tracer.Version);

    private static readonly ILogger Log = OtelLogging.GetLogger();

    internal static Activity CreateActivityFromExecuteAsync(IExecutionContext executionContext)
    {
        Activity activity = null;
        InstrumentationOptions options = Instrumentation.TracerSettings.InstrumentationOptions;

        try
        {
            string query = executionContext.Document.OriginalQuery;
            string operationName = executionContext.Operation.Name;
            string operationType = executionContext.Operation.OperationType.ToString().ToLowerInvariant();
            string operation = GetOperation(operationName, operationType);

            var tags = new GraphQLTags();
            tags.OperationName = operationName;
            tags.OperationType = operationType;

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

    internal static void RecordExecutionErrorsIfPresent(Activity activity, string errorType, IExecutionErrors executionErrors)
    {
        var errorCount = executionErrors?.Count ?? 0;

        if (errorCount > 0)
        {
            activity.SetTag(Tags.Status, Status.Error);
            activity.SetTag(Tags.ErrorMsg, $"{errorCount} error(s)");
            activity.SetTag(Tags.ErrorType, errorType);
            activity.SetTag(Tags.ErrorStack, ConstructErrorMessage(executionErrors));
        }
    }

    private static string ConstructErrorMessage(IExecutionErrors executionErrors)
    {
        if (executionErrors == null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        try
        {
            const string tab = "    ";
            builder.AppendLine("errors: [");

            for (int i = 0; i < executionErrors.Count; i++)
            {
                var executionError = executionErrors[i];

                builder.AppendLine($"{tab}{{");

                var message = executionError.Message;
                if (message != null)
                {
                    builder.AppendLine($"{tab + tab}\"message\": \"{message.Replace("\r", "\\r").Replace("\n", "\\n")}\",");
                }

                var path = executionError.Path;
                if (path != null)
                {
                    builder.AppendLine($"{tab + tab}\"path\": \"{string.Join(".", path)}\",");
                }

                var code = executionError.Code;
                if (code != null)
                {
                    builder.AppendLine($"{tab + tab}\"code\": \"{code}\",");
                }

                builder.AppendLine($"{tab + tab}\"locations\": [");
                var locations = executionError.Locations;
                if (locations != null)
                {
                    foreach (var location in locations)
                    {
                        if (location.TryDuckCast<ErrorLocationStruct>(out var locationProxy))
                        {
                            builder.AppendLine($"{tab + tab + tab}{{");
                            builder.AppendLine($"{tab + tab + tab + tab}\"line\": {locationProxy.Line},");
                            builder.AppendLine($"{tab + tab + tab + tab}\"column\": {locationProxy.Column}");
                            builder.AppendLine($"{tab + tab + tab}}},");
                        }
                    }
                }

                builder.AppendLine($"{tab + tab}]");
                builder.AppendLine($"{tab}}},");
            }

            builder.AppendLine("]");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating GraphQL error message.");
            return "errors: []";
        }

        return builder.ToString();
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
