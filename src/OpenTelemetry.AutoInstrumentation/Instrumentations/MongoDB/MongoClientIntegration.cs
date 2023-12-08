// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB;

/// <summary>
/// MongoDB.Driver.MongoClient calltarget instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: "MongoDB.Driver",
    typeName: "MongoDB.Driver.MongoClient",
    methodName: ".ctor",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { "MongoDB.Driver.MongoClientSettings" },
    minimumVersion: "2.13.3",
    maximumVersion: "2.65535.65535",
    integrationName: "MongoDB",
    type: InstrumentationType.Trace)]
public static class MongoClientIntegration
{
    private static Delegate? _setActivityListener;

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TMongoClientSettings">Type of the settings</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="settings">The source of the original GraphQL query</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TMongoClientSettings>(TTarget instance, TMongoClientSettings settings)
        where TMongoClientSettings : notnull
    {
        var setListenerDelegate = _setActivityListener ??= GetClusterConfiguratorExpression().Compile();

        var clusterConfiguratorProperty = settings
            .GetType()
            .GetProperty("ClusterConfigurator", BindingFlags.Public | BindingFlags.Instance);
        var existingDelegate = clusterConfiguratorProperty?.GetValue(settings) as Delegate;

        clusterConfiguratorProperty?.SetValue(settings, Delegate.Combine(existingDelegate, setListenerDelegate));

        return CallTargetState.GetDefault();
    }

    private static object GetInstrumentationOptions()
    {
        Type optionsType = Type.GetType("MongoDB.Driver.Core.Extensions.DiagnosticSources.InstrumentationOptions, MongoDB.Driver.Core.Extensions.DiagnosticSources")!;

        var options = Activator.CreateInstance(optionsType)!;
        var publicProperty = BindingFlags.Public | BindingFlags.Instance;
        var shouldStartActivityLambda = GetShouldStartActivityExpression();

        optionsType.GetProperty("CaptureCommandText", publicProperty)?.SetValue(options, true);
        optionsType.GetProperty("ShouldStartActivity", publicProperty)?.SetValue(options, shouldStartActivityLambda.Compile());

        return options;
    }

    private static LambdaExpression GetShouldStartActivityExpression()
    {
        Expression<Func<string, bool>> shouldStartActivity = (string cmdName) => !Regex.IsMatch(cmdName, "isMaster|buildInfo|explain|killCursors", RegexOptions.Compiled);

        Type eventType = Type.GetType("MongoDB.Driver.Core.Events.CommandStartedEvent, MongoDB.Driver.Core")!;
        Type lambdaType = typeof(Func<,>).MakeGenericType(eventType, typeof(bool));

        var commandStartedEventParam = Expression.Parameter(eventType);
        var commandNameProperty = eventType.GetProperty("CommandName")!;
        var invokeExpression = Expression.Invoke(shouldStartActivity, Expression.MakeMemberAccess(commandStartedEventParam, commandNameProperty));
        var shouldStartActivityLambda = Expression.Lambda(lambdaType, invokeExpression, commandStartedEventParam);

        return shouldStartActivityLambda;
    }

    private static LambdaExpression GetClusterConfiguratorExpression()
    {
        Type eventSubscriberInterface = Type.GetType("MongoDB.Driver.Core.Events.IEventSubscriber, MongoDB.Driver.Core")!;
        Type clusterBuilderType = Type.GetType("MongoDB.Driver.Core.Configuration.ClusterBuilder, MongoDB.Driver.Core")!;
        Type listenerType = Type.GetType("MongoDB.Driver.Core.Extensions.DiagnosticSources.DiagnosticsActivityEventSubscriber, MongoDB.Driver.Core.Extensions.DiagnosticSources")!;

        var options = GetInstrumentationOptions();
        var listener = Activator.CreateInstance(listenerType, options);

        var mi = clusterBuilderType.GetMethods()
            .First(x =>
                x.Name == "Subscribe" &&
                x.GetParameters().All(p =>
                    p.ParameterType == eventSubscriberInterface));

        var cbParamExpression = Expression.Parameter(clusterBuilderType);
        var callExpression = Expression.Call(cbParamExpression, mi, Expression.Constant(listener));
        var lambdaType = typeof(Action<>).MakeGenericType(clusterBuilderType);

        var setListenerLambda = Expression.Lambda(lambdaType, callExpression, cbParamExpression);

        return setListenerLambda;
    }
}
