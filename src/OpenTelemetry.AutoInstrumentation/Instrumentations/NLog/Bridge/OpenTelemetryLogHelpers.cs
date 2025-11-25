// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge;

/// <summary>
/// Delegate for emitting log records to OpenTelemetry.
/// This delegate signature matches the requirements for creating OpenTelemetry log records
/// with all necessary metadata and context information.
/// </summary>
/// <param name="loggerInstance">The OpenTelemetry logger instance.</param>
/// <param name="body">The log message body or template.</param>
/// <param name="timestamp">The timestamp when the log event occurred.</param>
/// <param name="severityText">The textual representation of the log level.</param>
/// <param name="severityLevel">The numeric severity level mapped to OpenTelemetry standards.</param>
/// <param name="exception">The exception associated with the log event, if any.</param>
/// <param name="properties">Additional properties to include in the log record.</param>
/// <param name="current">The current activity for trace context.</param>
/// <param name="args">Message template arguments for structured logging.</param>
/// <param name="renderedMessage">The fully formatted message for inclusion as an attribute.</param>
internal delegate void EmitLog(object loggerInstance, string? body, DateTime timestamp, string? severityText, int severityLevel, Exception? exception, IEnumerable<KeyValuePair<string, object?>>? properties, Activity? current, object?[]? args, string? renderedMessage);

/// <summary>
/// Helper class for creating OpenTelemetry log records from NLog events.
/// This class provides the core functionality for bridging NLog logging to OpenTelemetry
/// by dynamically creating log emission functions that work with OpenTelemetry's internal APIs.
///
/// TODO: Remove whole class when Logs Api is made public in non-rc builds.
/// </summary>
internal static class OpenTelemetryLogHelpers
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    static OpenTelemetryLogHelpers()
    {
        try
        {
            // Use reflection to access OpenTelemetry's internal logging types
            // This is necessary because the logging API is not yet public
            var loggerProviderType = typeof(LoggerProvider);
            var apiAssembly = loggerProviderType.Assembly;
            var loggerType = typeof(Sdk).Assembly.GetType("OpenTelemetry.Logs.LoggerSdk");
            var logRecordDataType = apiAssembly.GetType("OpenTelemetry.Logs.LogRecordData")!;
            var logRecordAttributesListType = apiAssembly.GetType("OpenTelemetry.Logs.LogRecordAttributeList")!;

            // Build the log emission delegate using expression trees
            LogEmitter = BuildEmitLog(logRecordDataType, logRecordAttributesListType, loggerType!);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to initialize LogEmitter delegate for NLog bridge.");
        }
    }

    /// <summary>
    /// Gets the log emitter delegate that can create OpenTelemetry log records.
    /// This delegate is constructed dynamically using reflection and expression trees
    /// to work with OpenTelemetry's internal logging APIs.
    /// </summary>
    public static EmitLog? LogEmitter { get; }

    /// <summary>
    /// Builds an expression tree for creating OpenTelemetry log records.
    /// This method constructs the necessary expressions to properly initialize
    /// LogRecordData objects with all required properties and attributes.
    /// </summary>
    /// <param name="logRecordDataType">The type of LogRecordData from OpenTelemetry.</param>
    /// <param name="severityType">The type representing log severity levels.</param>
    /// <param name="body">Parameter expression for the log message body.</param>
    /// <param name="timestamp">Parameter expression for the log timestamp.</param>
    /// <param name="severityText">Parameter expression for the severity text.</param>
    /// <param name="severityLevel">Parameter expression for the numeric severity level.</param>
    /// <param name="activity">Parameter expression for the current activity.</param>
    /// <returns>A block expression that creates and initializes a LogRecordData object.</returns>
    private static BlockExpression BuildLogRecord(
        Type logRecordDataType,
        Type severityType,
        ParameterExpression body,
        ParameterExpression timestamp,
        ParameterExpression severityText,
        ParameterExpression severityLevel,
        ParameterExpression activity)
    {
        // Creates expression tree that generates code equivalent to:
        // var instance = new LogRecordData(activity);
        // if (body != null) instance.Body = body;
        // instance.Timestamp = timestamp;
        // if (severityText != null) instance.SeverityText = severityText;
        // instance.Severity = (LogRecordSeverity?)severityLevel;
        // return instance;

        var timestampSetterMethodInfo = logRecordDataType.GetProperty("Timestamp")!.GetSetMethod()!;
        var bodySetterMethodInfo = logRecordDataType.GetProperty("Body")!.GetSetMethod()!;
        var severityTextSetterMethodInfo = logRecordDataType.GetProperty("SeverityText")!.GetSetMethod()!;
        var severityLevelSetterMethodInfo = logRecordDataType.GetProperty("Severity")!.GetSetMethod()!;

        var instanceVar = Expression.Variable(bodySetterMethodInfo.DeclaringType!, "instance");

        var constructorInfo = logRecordDataType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, new[] { typeof(Activity) }, null)!;
        var assignInstanceVar = Expression.Assign(instanceVar, Expression.New(constructorInfo, activity));
        var setBody = Expression.IfThen(Expression.NotEqual(body, Expression.Constant(null)), Expression.Call(instanceVar, bodySetterMethodInfo, body));
        var setTimestamp = Expression.Call(instanceVar, timestampSetterMethodInfo, timestamp);
        var setSeverityText = Expression.IfThen(Expression.NotEqual(severityText, Expression.Constant(null)), Expression.Call(instanceVar, severityTextSetterMethodInfo, severityText));
        var setSeverityLevel = Expression.Call(instanceVar, severityLevelSetterMethodInfo, Expression.Convert(severityLevel, typeof(Nullable<>).MakeGenericType(severityType)));

        return Expression.Block(
            new[] { instanceVar },
            assignInstanceVar,
            setBody,
            setTimestamp,
            setSeverityText,
            setSeverityLevel,
            instanceVar);
    }

    /// <summary>
    /// Builds an expression tree for creating and populating log record attributes.
    /// This handles exceptions, custom properties, message template arguments, and rendered messages.
    /// </summary>
    /// <param name="logRecordAttributesListType">The type of LogRecordAttributeList from OpenTelemetry.</param>
    /// <param name="exception">Parameter expression for the exception.</param>
    /// <param name="properties">Parameter expression for custom properties.</param>
    /// <param name="argsParam">Parameter expression for message template arguments.</param>
    /// <param name="renderedMessageParam">Parameter expression for the rendered message.</param>
    /// <returns>A block expression that creates and populates a LogRecordAttributeList.</returns>
    private static BlockExpression BuildLogRecordAttributes(
        Type logRecordAttributesListType,
        ParameterExpression exception,
        ParameterExpression properties,
        ParameterExpression argsParam,
        ParameterExpression renderedMessageParam)
    {
        // Creates expression tree that generates code to populate log attributes
        // including exception details, custom properties, and structured logging parameters

        var instanceVar = Expression.Variable(logRecordAttributesListType, "instance");
        var constructorInfo = logRecordAttributesListType.GetConstructor(Type.EmptyTypes);

        Expression assignInstanceVar;

        // If no parameterless constructor, try to find other constructors or use default for structs
        if (constructorInfo == null)
        {
            // Try to find a constructor that takes an int (capacity)
            constructorInfo = logRecordAttributesListType.GetConstructor(new[] { typeof(int) });
            if (constructorInfo != null)
            {
                assignInstanceVar = Expression.Assign(instanceVar, Expression.New(constructorInfo, Expression.Constant(4)));
            }
            else if (logRecordAttributesListType.IsValueType)
            {
                // For structs, use default value
                assignInstanceVar = Expression.Assign(instanceVar, Expression.Default(logRecordAttributesListType));
            }
            else
            {
                throw new InvalidOperationException($"No suitable constructor found for {logRecordAttributesListType.Name}");
            }
        }
        else
        {
            assignInstanceVar = Expression.Assign(instanceVar, Expression.New(constructorInfo));
        }

        var addAttributeMethodInfo = logRecordAttributesListType.GetMethod("Add", new[] { typeof(string), typeof(object) })!;
        var recordExceptionMethodInfo = logRecordAttributesListType.GetMethod("RecordException", BindingFlags.Instance | BindingFlags.Public)!;

        var expressions = new List<Expression> { assignInstanceVar };

        // Record exception using RecordException which adds exception.type, exception.message, exception.stacktrace
        var recordExceptionExpression = Expression.IfThen(
            Expression.NotEqual(exception, Expression.Constant(null)),
            Expression.Call(instanceVar, recordExceptionMethodInfo, exception));
        expressions.Add(recordExceptionExpression);

        // Add custom properties if present
        var addPropertiesExpression = BuildAddPropertiesExpression(instanceVar, properties, addAttributeMethodInfo);
        expressions.Add(addPropertiesExpression);

        // Add structured logging arguments if present
        var addArgsExpression = BuildAddArgsExpression(instanceVar, argsParam, addAttributeMethodInfo);
        expressions.Add(addArgsExpression);

        // Add rendered message if present
        var addRenderedMessageExpression = Expression.IfThen(
            Expression.NotEqual(renderedMessageParam, Expression.Constant(null)),
            Expression.Call(instanceVar, addAttributeMethodInfo, Expression.Constant("RenderedMessage"), renderedMessageParam));
        expressions.Add(addRenderedMessageExpression);

        expressions.Add(instanceVar);

        return Expression.Block(
            new[] { instanceVar },
            expressions);
    }

    /// <summary>
    /// Builds an expression for adding custom properties to the log record attributes.
    /// </summary>
    /// <param name="instanceVar">The LogRecordAttributeList instance variable.</param>
    /// <param name="properties">The properties parameter expression.</param>
    /// <param name="addAttributeMethodInfo">The Add method for adding attributes.</param>
    /// <returns>An expression that adds all custom properties to the attributes list.</returns>
    private static Expression BuildAddPropertiesExpression(ParameterExpression instanceVar, ParameterExpression properties, MethodInfo addAttributeMethodInfo)
    {
        // Create a foreach loop to iterate over properties and add them as attributes
        var enumerableType = typeof(IEnumerable<KeyValuePair<string, object?>>);
        var kvpType = typeof(KeyValuePair<string, object?>);

        var getEnumeratorMethod = enumerableType.GetMethod("GetEnumerator")!;
        var enumeratorType = getEnumeratorMethod.ReturnType;
        var moveNextMethod = typeof(System.Collections.IEnumerator).GetMethod("MoveNext")!;
        var currentProperty = enumeratorType.GetProperty("Current")!;
        var keyProperty = kvpType.GetProperty("Key")!;
        var valueProperty = kvpType.GetProperty("Value")!;

        var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
        var currentVar = Expression.Variable(kvpType, "current");
        var breakLabel = Expression.Label();

        var loop = Expression.Loop(
            Expression.Block(
                Expression.IfThen(
                    Expression.IsFalse(Expression.Call(enumeratorVar, moveNextMethod)),
                    Expression.Break(breakLabel)),
                Expression.Assign(currentVar, Expression.Property(enumeratorVar, currentProperty)),
                Expression.Call(
                    instanceVar,
                    addAttributeMethodInfo,
                    Expression.Property(currentVar, keyProperty),
                    Expression.Property(currentVar, valueProperty))),
            breakLabel);

        return Expression.IfThen(
            Expression.NotEqual(properties, Expression.Constant(null)),
            Expression.Block(
                new[] { enumeratorVar, currentVar },
                Expression.Assign(enumeratorVar, Expression.Call(properties, getEnumeratorMethod)),
                loop));
    }

    /// <summary>
    /// Builds an expression for adding structured logging arguments to the log record attributes.
    /// </summary>
    /// <param name="instanceVar">The LogRecordAttributeList instance variable.</param>
    /// <param name="argsParam">The arguments parameter expression.</param>
    /// <param name="addAttributeMethodInfo">The Add method for adding attributes.</param>
    /// <returns>An expression that adds structured logging arguments as attributes.</returns>
    private static Expression BuildAddArgsExpression(ParameterExpression instanceVar, ParameterExpression argsParam, MethodInfo addAttributeMethodInfo)
    {
        // Create a for loop to iterate over args array and add them as indexed attributes
        var lengthProperty = typeof(Array).GetProperty("Length")!;
        var indexVar = Expression.Variable(typeof(int), "i");
        var breakLabel = Expression.Label();

        var loop = Expression.Loop(
            Expression.Block(
                Expression.IfThen(
                    Expression.GreaterThanOrEqual(indexVar, Expression.Property(argsParam, lengthProperty)),
                    Expression.Break(breakLabel)),
                Expression.Call(
                    instanceVar,
                    addAttributeMethodInfo,
                    Expression.Call(indexVar, typeof(int).GetMethod("ToString", Type.EmptyTypes)!),
                    Expression.ArrayIndex(argsParam, indexVar)),
                Expression.Assign(indexVar, Expression.Add(indexVar, Expression.Constant(1)))),
            breakLabel);

        return Expression.IfThen(
            Expression.NotEqual(argsParam, Expression.Constant(null)),
            Expression.Block(
                new[] { indexVar },
                Expression.Assign(indexVar, Expression.Constant(0)),
                loop));
    }

    /// <summary>
    /// Builds the complete EmitLog delegate using expression trees.
    /// This method constructs a function that can create OpenTelemetry log records
    /// from NLog event data.
    /// </summary>
    /// <param name="logRecordDataType">The LogRecordData type from OpenTelemetry.</param>
    /// <param name="logRecordAttributesListType">The LogRecordAttributeList type from OpenTelemetry.</param>
    /// <param name="loggerType">The Logger type from OpenTelemetry.</param>
    /// <returns>An EmitLog delegate that can create OpenTelemetry log records.</returns>
    private static EmitLog BuildEmitLog(Type logRecordDataType, Type logRecordAttributesListType, Type loggerType)
    {
        // Get the LogRecordSeverity enum type
        var severityType = logRecordDataType.Assembly.GetType("OpenTelemetry.Logs.LogRecordSeverity")!;

        // Define parameters for the delegate
        var loggerInstance = Expression.Parameter(typeof(object), "loggerInstance");
        var body = Expression.Parameter(typeof(string), "body");
        var timestamp = Expression.Parameter(typeof(DateTime), "timestamp");
        var severityText = Expression.Parameter(typeof(string), "severityText");
        var severityLevel = Expression.Parameter(typeof(int), "severityLevel");
        var exception = Expression.Parameter(typeof(Exception), "exception");
        var properties = Expression.Parameter(typeof(IEnumerable<KeyValuePair<string, object?>>), "properties");
        var activity = Expression.Parameter(typeof(Activity), "activity");
        var args = Expression.Parameter(typeof(object[]), "args");
        var renderedMessage = Expression.Parameter(typeof(string), "renderedMessage");

        // Build the log record creation expression
        var logRecordExpression = BuildLogRecord(logRecordDataType, severityType, body, timestamp, severityText, severityLevel, activity);

        // Build the attributes creation expression
        var attributesExpression = BuildLogRecordAttributes(logRecordAttributesListType, exception, properties, args, renderedMessage);

        // Get the EmitLog method from the logger
        var emitLogRecordMethod = loggerType.GetMethod("EmitLog", BindingFlags.Instance | BindingFlags.Public, null, new[] { logRecordDataType.MakeByRefType(), logRecordAttributesListType.MakeByRefType() }, null)!;

        // Build the complete expression that creates the log record, creates attributes, and emits the log
        var completeExpression = Expression.Block(
            Expression.Call(
                Expression.Convert(loggerInstance, loggerType),
                emitLogRecordMethod,
                logRecordExpression,
                attributesExpression));

        // Compile the expression into a delegate
        var lambda = Expression.Lambda<EmitLog>(
            completeExpression,
            loggerInstance,
            body,
            timestamp,
            severityText,
            severityLevel,
            exception,
            properties,
            activity,
            args,
            renderedMessage);

        return lambda.Compile();
    }
}
