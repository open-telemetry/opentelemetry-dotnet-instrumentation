// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.TraceContextInjection;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.Bridge;

internal delegate void EmitLog(object loggerInstance, string? body, DateTime timestamp, string? severityText, int severityLevel, Exception? exception, IDictionary? properties, Activity? current, object?[]? args, string? renderedMessage);

// TODO: Remove whole class when Logs Api is made public in non-rc builds.
internal static class OpenTelemetryLogHelpers
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    static OpenTelemetryLogHelpers()
    {
        try
        {
            var loggerProviderType = typeof(LoggerProvider);
            var apiAssembly = loggerProviderType.Assembly;
            var loggerType = typeof(Sdk).Assembly.GetType("OpenTelemetry.Logs.LoggerSdk");
            var logRecordDataType = apiAssembly.GetType("OpenTelemetry.Logs.LogRecordData")!;
            var logRecordAttributesListType = apiAssembly.GetType("OpenTelemetry.Logs.LogRecordAttributeList")!;

            LogEmitter = BuildEmitLog(logRecordDataType, logRecordAttributesListType, loggerType!);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to initialize LogEmitter delegate.");
        }
    }

    public static EmitLog? LogEmitter { get; }

    private static BlockExpression BuildLogRecord(
        Type logRecordDataType,
        Type severityType,
        ParameterExpression body,
        ParameterExpression timestamp,
        ParameterExpression severityText,
        ParameterExpression severityLevel,
        ParameterExpression activity)
    {
        // Creates expression:
        // .Block(OpenTelemetry.Logs.LogRecordData $instance) {
        //     $instance = .New OpenTelemetry.Logs.LogRecordData($activity);
        //     .If ($body != null) {
        //         .Call $instance.set_Body($body)
        //     } .Else {
        //         .Default(System.Void)
        //     };
        //     .Call $instance.set_Timestamp($timestamp);
        //     .If ($severityText != null) {
        //         .Call $instance.set_SeverityText($severityText)
        //     } .Else {
        //         .Default(System.Void)
        //     };
        //     .Call $instance.set_Severity((System.Nullable`1[OpenTelemetry.Logs.LogRecordSeverity])$severityLevel);
        //     $instance
        // }

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

    private static BlockExpression BuildLogRecordAttributes(
        Type logRecordAttributesListType,
        ParameterExpression exception,
        ParameterExpression properties,
        ParameterExpression argsParam,
        ParameterExpression renderedMessageParam)
    {
        // Creates expression:
        // .Block(OpenTelemetry.Logs.LogRecordAttributeList $instance) {
        //     $instance = .New OpenTelemetry.Logs.LogRecordAttributeList();
        //     .If ($exception != null) {
        //         .Call $instance.RecordException($exception)
        //     } .Else {
        //         .Default(System.Void)
        //     };
        //     .If ($renderedMessage != null) {
        //         .Call $instance.Add(
        //             "log4net.rendered_message",
        //             $renderedMessage)
        //     } .Else {
        //         .Default(System.Void)
        //     };
        //     .If ($properties != null) {
        //         .Block(System.Collections.IDictionaryEnumerator $enumerator) {
        //             $enumerator = .Call $properties.GetEnumerator();
        //             .Try {
        //                 .Loop  {
        //                     .If (.Call $enumerator.MoveNext() == True) {
        //                         .Block(
        //                             System.Collections.IDictionaryEnumerator $loopVar,
        //                             System.String $key) {
        //                             $loopVar = (System.Collections.IDictionaryEnumerator)$enumerator;
        //                             $key = (System.String)$loopVar.Key;
        //                             .If (
        //                                 .Call $key.StartsWith("log4net:") != True & $key != "span_id" & $key != "trace_id" & $key != "trace_flags"
        //                             ) {
        //                                 .Call $instance.Add(
        //                                     $key,
        //                                     $loopVar.Value)
        //                             } .Else {
        //                                 .Default(System.Void)
        //                             }
        //                         }
        //                     } .Else {
        //                         .Break #Label1 { }
        //                     }
        //                 }
        //                 .LabelTarget #Label1:
        //             } .Finally {
        //                 .Block(System.IDisposable $disposable) {
        //                     $disposable = $enumerator .As System.IDisposable;
        //                     .If ($disposable != null) {
        //                         .Call ((System.IDisposable)$enumerator).Dispose()
        //                     } .Else {
        //                         .Default(System.Void)
        //                     }
        //                 }
        //             }
        //         }
        //     } .Else {
        //         .Default(System.Void)
        //     };
        //     .If ($args != null) {
        //         .Loop  {
        //             .Block(
        //                 System.Int32 $index,
        //                 System.Object $value) {
        //                 .If ($index < $args.Length) {
        //                     .Block() {
        //                         $value = $args[$index];
        //                         .Call $instance.Add(
        //                             .Call $index.ToString(),
        //                             $value);
        //                         $index++
        //                     }
        //                 } .Else {
        //                     .Break #Label2 { }
        //                 }
        //             }
        //         }
        //         .LabelTarget #Label2:
        //     } .Else {
        //         .Default(System.Void)
        //     };
        //     $instance
        // }

        var stringType = typeof(string);

        var enumeratorInterface = typeof(IEnumerator);
        var disposableInterface = typeof(IDisposable);

        var dictionaryEnumerator = typeof(IDictionaryEnumerator);

        var exceptionRecordMethod = logRecordAttributesListType.GetMethod("RecordException", BindingFlags.Instance | BindingFlags.Public)!;
        var addAttributeMethod = logRecordAttributesListType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new[] { stringType, typeof(object) }, null)!;
        var disposeMethod = disposableInterface.GetMethod("Dispose")!;
        var moveNextMethod = enumeratorInterface.GetMethod("MoveNext")!;
        var getEnumeratorMethod = typeof(IDictionary).GetMethod("GetEnumerator")!;
        var toStringMethod = typeof(object).GetMethod("ToString")!;

        var exitLabel = Expression.Label();

        var instanceVar = Expression.Variable(logRecordAttributesListType, "instance");
        var enumeratorVar = Expression.Variable(typeof(IDictionaryEnumerator), "enumerator");
        var loopVar = Expression.Variable(dictionaryEnumerator, "loopVar");
        var disposable = Expression.Variable(typeof(IDisposable), "disposable");
        var keyVar = Expression.Variable(stringType, "key");

        var assignInstanceVar = Expression.Assign(instanceVar, Expression.New(logRecordAttributesListType));
        var recordExceptionIfNotNull = Expression.IfThen(Expression.NotEqual(exception, Expression.Constant(null)), Expression.Call(instanceVar, exceptionRecordMethod, exception));
        var setRenderedMessageIfNotNull = Expression.IfThen(Expression.NotEqual(renderedMessageParam, Expression.Constant(null)), Expression.Call(instanceVar, addAttributeMethod, Expression.Constant("log4net.rendered_message"), renderedMessageParam));

        var getPropertiesEnumerator = Expression.Call(properties, getEnumeratorMethod);
        var enumeratorAssign = Expression.Assign(enumeratorVar, getPropertiesEnumerator);
        var enumeratorDispose = Expression.Call(Expression.Convert(enumeratorVar, disposableInterface), disposeMethod);

        var moveNext = Expression.Call(enumeratorVar, moveNextMethod);

        var getKeyProperty = Expression.Convert(Expression.Property(loopVar, "Key"), stringType);
        var startsWithMethodInfo = stringType.GetMethod("StartsWith", BindingFlags.Instance | BindingFlags.Public, null, new[] { stringType }, null)!;

        // Filter out built-in log4net properties, starting with 'log4net:' prefix, and properties added by trace context injection
        var propertyNameFilter = Expression.And(
            Expression.NotEqual(Expression.Call(keyVar, startsWithMethodInfo, Expression.Constant("log4net:")), Expression.Constant(true)),
            Expression.And(
                Expression.NotEqual(
                    keyVar,
                    Expression.Constant(LogsTraceContextInjectionConstants.SpanIdPropertyName)),
                Expression.And(
                    Expression.NotEqual(
                        keyVar,
                        Expression.Constant(LogsTraceContextInjectionConstants.TraceIdPropertyName)),
                    Expression.NotEqual(
                        keyVar,
                        Expression.Constant(LogsTraceContextInjectionConstants.TraceFlagsPropertyName)))));
        var loopAndAddProperties = Expression.Loop(
            Expression.IfThenElse(
                Expression.Equal(moveNext, Expression.Constant(true)),
                Expression.Block(
                    new[] { loopVar, keyVar },
                    Expression.Assign(loopVar, Expression.Convert(enumeratorVar, dictionaryEnumerator)),
                    Expression.Assign(keyVar, getKeyProperty),
                    Expression.IfThen(
                            propertyNameFilter,
                            Expression.Call(instanceVar, addAttributeMethod, keyVar, Expression.Property(loopVar, "Value")))),
                Expression.Break(exitLabel)),
            exitLabel);

        var addPropertiesWithForeach =
            Expression.Block(
                new[] { enumeratorVar },
                enumeratorAssign,
                Expression.TryFinally(
                    loopAndAddProperties,
                    Expression.Block(
                        new[] { disposable },
                        Expression.Assign(disposable, Expression.TypeAs(enumeratorVar, disposableInterface)),
                        Expression.IfThen(Expression.NotEqual(disposable, Expression.Constant(null)), enumeratorDispose))));

        var exitLabel2 = Expression.Label();

        var argsIndexVar = Expression.Variable(typeof(int), "index");
        var argsValueVar = Expression.Variable(typeof(object), "value");

        var loopAndAddArgs = Expression.Loop(
                Expression.Block(
                    new[] { argsIndexVar, argsValueVar },
                    Expression.IfThenElse(
                        Expression.LessThan(argsIndexVar, Expression.Property(argsParam, "Length")),
                        Expression.Block(
                            Expression.Assign(argsValueVar, Expression.ArrayIndex(argsParam, argsIndexVar)),
                            Expression.Call(instanceVar, addAttributeMethod, Expression.Call(argsIndexVar, toStringMethod), argsValueVar),
                            Expression.PostIncrementAssign(argsIndexVar)),
                        Expression.Break(exitLabel2))),
                exitLabel2);

        var addPropertiesIfNotNull = Expression.IfThen(Expression.NotEqual(properties, Expression.Constant(null)), addPropertiesWithForeach);
        var addArgsIfNotNull = Expression.IfThen(Expression.NotEqual(argsParam, Expression.Constant(null)), loopAndAddArgs);
        return Expression.Block(
            new[] { instanceVar },
            assignInstanceVar,
            recordExceptionIfNotNull,
            setRenderedMessageIfNotNull,
            addPropertiesIfNotNull,
            addArgsIfNotNull,
            instanceVar);
    }

    private static EmitLog? BuildEmitLog(Type logRecordDataType, Type logRecordAttributesListType, Type loggerType)
    {
        // Creates expression:
        // .Lambda #Lambda1<OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.EmitLog>(
        //     System.Object $instance,
        //     System.String $body,
        //     System.DateTime $timestamp,
        //     System.String $severityText,
        //     System.Int32 $severityLevel,
        //     System.Exception $exception,
        //     System.Collections.IDictionary $properties,
        //     System.Diagnostics.Activity $activity,
        //     System.Object[] $args
        //     System.String $renderedMessage) {
        //     .Block(
        //         OpenTelemetry.Logs.LogRecordData $logRecordData,
        //         OpenTelemetry.Logs.LogRecordAttributeList $logRecordAttributes) {
        //         $logRecordData = BuildLogRecord expression's value;
        //         $logRecordAttributes = BuildLogRecordAttributes expression's value;
        //         .Call ((OpenTelemetry.Logs.LoggerSdk)$instance).EmitLog(
        //             $logRecordData,
        //             $logRecordAttributes)
        //     }
        // }

        var stringType = typeof(string);

        var instance = Expression.Parameter(typeof(object), "instance");
        var bodyParam = Expression.Parameter(stringType, "body");
        var timestampParam = Expression.Parameter(typeof(DateTime), "timestamp");
        var severityTextParam = Expression.Parameter(stringType, "severityText");
        var severityLevelParam = Expression.Parameter(typeof(int), "severityLevel");
        var activityParam = Expression.Parameter(typeof(Activity), "activity");
        var argsParam = Expression.Parameter(typeof(object[]), "args");
        var renderedMessageParam = Expression.Parameter(typeof(string), "renderedMessage");

        var exceptionParam = Expression.Parameter(typeof(Exception), "exception");
        var propertiesParam = Expression.Parameter(typeof(IDictionary), "properties");

        var logRecordDataVar = Expression.Variable(logRecordDataType, "logRecordData");
        var logRecordAttributesVar = Expression.Variable(logRecordAttributesListType, "logRecordAttributes");

        var instanceCasted = Expression.Convert(instance, loggerType);

        var methodInfo = loggerType.GetMethod("EmitLog", BindingFlags.Instance | BindingFlags.Public, null, new[] { logRecordDataType.MakeByRefType(), logRecordAttributesListType.MakeByRefType() }, null);

        var logRecord = BuildLogRecord(logRecordDataType, typeof(LoggerProvider).Assembly.GetType("OpenTelemetry.Logs.LogRecordSeverity")!, bodyParam, timestampParam, severityTextParam, severityLevelParam, activityParam);
        var logRecordAttributes = BuildLogRecordAttributes(logRecordAttributesListType, exceptionParam, propertiesParam, argsParam, renderedMessageParam);

        var block = Expression.Block(
            new[] { logRecordDataVar, logRecordAttributesVar },
            Expression.Assign(logRecordDataVar, logRecord),
            Expression.Assign(logRecordAttributesVar, logRecordAttributes),
            Expression.Call(instanceCasted, methodInfo!, logRecordDataVar, logRecordAttributesVar));

        var expr = Expression.Lambda<EmitLog?>(block, instance, bodyParam, timestampParam, severityTextParam, severityLevelParam, exceptionParam, propertiesParam, activityParam, argsParam, renderedMessageParam);

        return expr.Compile();
    }
}
