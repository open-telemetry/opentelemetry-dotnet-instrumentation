// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.Bridge;

internal delegate void EmitLog(object loggerInstance, string? body, DateTime timestamp, string? severityText, int severityLevel, Exception? exception, IEnumerable<KeyValuePair<string, object?>>? properties, Activity? current, object?[]? args, string? renderedMessage);

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
#pragma warning disable CA1031 // Do not catch general exception types. Logged and ignored to avoid breaking logging.
        catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types. Logged and ignored to avoid breaking logging.
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

        var constructorInfo = logRecordDataType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, [typeof(Activity)], null)!;
        var assignInstanceVar = Expression.Assign(instanceVar, Expression.New(constructorInfo, activity));
        var setBody = Expression.IfThen(Expression.NotEqual(body, Expression.Constant(null)), Expression.Call(instanceVar, bodySetterMethodInfo, body));
        var setTimestamp = Expression.Call(instanceVar, timestampSetterMethodInfo, timestamp);
        var setSeverityText = Expression.IfThen(Expression.NotEqual(severityText, Expression.Constant(null)), Expression.Call(instanceVar, severityTextSetterMethodInfo, severityText));
        var setSeverityLevel = Expression.Call(instanceVar, severityLevelSetterMethodInfo, Expression.Convert(severityLevel, typeof(Nullable<>).MakeGenericType(severityType)));

        return Expression.Block(
            [instanceVar],
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
        // .Block(
        //     OpenTelemetry.Logs.LogRecordAttributeList $instance,
        //     System.Int32 $index,
        //     System.Object $value) {
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
        //         .Block(System.Collections.Generic.IEnumerator`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Object]] $enumerator)
        //          {
        //             $enumerator = .Call $properties.GetEnumerator();
        //             .Try {
        //                 .Loop  {
        //                     .If (.Call $enumerator.MoveNext() == True) {
        //                         .Block(
        //                             System.Collections.Generic.KeyValuePair`2[System.String,System.Object] $loopVar,
        //                             System.String $key) {
        //                             $loopVar = $enumerator.Current;
        //                             $key = (System.String)$loopVar.Key;
        //                             .Call $instance.Add(
        //                                 $key,
        //                                 $loopVar.Value)
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
        //     $index = 0;
        //     $value = null;
        //     .If ($args != null) {
        //         .Loop  {
        //             .If ($index < $args.Length) {
        //                 .Block() {
        //                     $value = $args[$index];
        //                     .Call $instance.Add(
        //                         .Call $index.ToString(),
        //                         $value);
        //                     $index++
        //                 }
        //             } .Else {
        //                 .Break #Label2 { }
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

        var dictionaryEnumerator = typeof(IEnumerator<KeyValuePair<string, object?>>);

        var exceptionRecordMethod = logRecordAttributesListType.GetMethod("RecordException", BindingFlags.Instance | BindingFlags.Public)!;
        var addAttributeMethod = logRecordAttributesListType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, [stringType, typeof(object)], null)!;
        var disposeMethod = disposableInterface.GetMethod(nameof(IDisposable.Dispose))!;
        var moveNextMethod = enumeratorInterface.GetMethod(nameof(IEnumerator.MoveNext))!;
        var getEnumeratorMethod = typeof(IEnumerable<KeyValuePair<string, object?>>).GetMethod(nameof(IEnumerable.GetEnumerator))!;
        var toStringMethod = typeof(object).GetMethod(nameof(ToString))!;

        var exitLabel = Expression.Label();

        var instanceVar = Expression.Variable(logRecordAttributesListType, "instance");
        var enumeratorVar = Expression.Variable(dictionaryEnumerator, "enumerator");
        var loopVar = Expression.Variable(typeof(KeyValuePair<string, object?>), "loopVar");
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

        var loopAndAddProperties = Expression.Loop(
            Expression.IfThenElse(
                Expression.Equal(moveNext, Expression.Constant(true)),
                Expression.Block(
                    [loopVar, keyVar],
                    Expression.Assign(loopVar, Expression.Property(enumeratorVar, nameof(IEnumerator.Current))),
                    Expression.Assign(keyVar, getKeyProperty),
                    Expression.Call(instanceVar, addAttributeMethod, keyVar, Expression.Property(loopVar, "Value"))),
                Expression.Break(exitLabel)),
            exitLabel);

        var addPropertiesWithForeach =
            Expression.Block(
                [enumeratorVar],
                enumeratorAssign,
                Expression.TryFinally(
                    loopAndAddProperties,
                    Expression.Block(
                        [disposable],
                        Expression.Assign(disposable, Expression.TypeAs(enumeratorVar, disposableInterface)),
                        Expression.IfThen(Expression.NotEqual(disposable, Expression.Constant(null)), enumeratorDispose))));

        var exitLabel2 = Expression.Label();

        var argsIndexVar = Expression.Variable(typeof(int), "index");
        var argsValueVar = Expression.Variable(typeof(object), "value");

        var loopAndAddArgs = Expression.Loop(
                Expression.IfThenElse(
                        Expression.LessThan(argsIndexVar, Expression.Property(argsParam, nameof(Array.Length))),
                        Expression.Block(
                            Expression.Assign(argsValueVar, Expression.ArrayIndex(argsParam, argsIndexVar)),
                            Expression.Call(instanceVar, addAttributeMethod, Expression.Call(argsIndexVar, toStringMethod), argsValueVar),
                            Expression.PostIncrementAssign(argsIndexVar)),
                        Expression.Break(exitLabel2)),
                exitLabel2);

        var addPropertiesIfNotNull = Expression.IfThen(Expression.NotEqual(properties, Expression.Constant(null)), addPropertiesWithForeach);
        var addArgsIfNotNull = Expression.IfThen(Expression.NotEqual(argsParam, Expression.Constant(null)), loopAndAddArgs);
        return Expression.Block(
            [instanceVar, argsIndexVar, argsValueVar],
            assignInstanceVar,
            recordExceptionIfNotNull,
            setRenderedMessageIfNotNull,
            addPropertiesIfNotNull,
            Expression.Assign(argsIndexVar, Expression.Constant(0)),
            Expression.Assign(argsValueVar, Expression.Constant(null)),
            addArgsIfNotNull,
            instanceVar);
    }

    private static EmitLog? BuildEmitLog(Type logRecordDataType, Type logRecordAttributesListType, Type loggerType)
    {
        // Creates expression:
        // .Lambda #Lambda1<OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.Bridge.EmitLog>(
        //     System.Object $instance,
        //     System.String $body,
        //     System.DateTime $timestamp,
        //     System.String $severityText,
        //     System.Int32 $severityLevel,
        //     System.Exception $exception,
        //     System.Collections.Generic.IEnumerable`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Object]] $properties,
        //     System.Diagnostics.Activity $activity,
        //     System.Object[] $args,
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
        var propertiesParam = Expression.Parameter(typeof(IEnumerable<KeyValuePair<string, object?>>), "properties");

        var logRecordDataVar = Expression.Variable(logRecordDataType, "logRecordData");
        var logRecordAttributesVar = Expression.Variable(logRecordAttributesListType, "logRecordAttributes");

        var instanceCasted = Expression.Convert(instance, loggerType);

        var methodInfo = loggerType.GetMethod("EmitLog", BindingFlags.Instance | BindingFlags.Public, null, [logRecordDataType.MakeByRefType(), logRecordAttributesListType.MakeByRefType()], null);

        var logRecord = BuildLogRecord(logRecordDataType, typeof(LoggerProvider).Assembly.GetType("OpenTelemetry.Logs.LogRecordSeverity")!, bodyParam, timestampParam, severityTextParam, severityLevelParam, activityParam);
        var logRecordAttributes = BuildLogRecordAttributes(logRecordAttributesListType, exceptionParam, propertiesParam, argsParam, renderedMessageParam);

        var block = Expression.Block(
            [logRecordDataVar, logRecordAttributesVar],
            Expression.Assign(logRecordDataVar, logRecord),
            Expression.Assign(logRecordAttributesVar, logRecordAttributes),
            Expression.Call(instanceCasted, methodInfo!, logRecordDataVar, logRecordAttributesVar));

        var expr = Expression.Lambda<EmitLog?>(block, instance, bodyParam, timestampParam, severityTextParam, severityLevelParam, exceptionParam, propertiesParam, activityParam, argsParam, renderedMessageParam);

        return expr.Compile();
    }
}
