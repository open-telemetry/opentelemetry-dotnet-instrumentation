// <copyright file="MongoClientIntegration.cs" company="OpenTelemetry Authors">
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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB
{
    /// <summary>
    /// MongoDB.Driver.MongoClient calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "MongoDB.Driver",
        TypeName = "MongoDB.Driver.MongoClient",
        MethodName = ".ctor",
        ReturnTypeName = ClrNames.Void,
        ParameterTypeNames = new[] { "MongoDB.Driver.MongoClientSettings" },
        MinimumVersion = "2.3.0",
        MaximumVersion = "2.65535.65535",
        IntegrationName = "MongoDB")]
    public class MongoClientIntegration
    {
#if NETCOREAPP3_1_OR_GREATER
        private static Delegate _setActivityListener;
#endif

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TMongoClientSettings">Type of the settings</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="settings">The source of the original GraphQL query</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TMongoClientSettings>(TTarget instance, TMongoClientSettings settings)
        {
            // Additional deps doesn't support .NET FX
            // TODO: Find another way how to ship & load "MongoDB.Driver.Core.Extensions.DiagnosticSources"

#if NETCOREAPP3_1_OR_GREATER
            var setListenerDelegate = _setActivityListener ??= GetClusterConfiguratorExpression().Compile();

            var clusterConfiguratorProperty = settings
               .GetType()
               .GetProperty("ClusterConfigurator", BindingFlags.Public | BindingFlags.Instance);
            var existingDelegate = clusterConfiguratorProperty?.GetValue(settings) as Delegate;

            clusterConfiguratorProperty?.SetValue(settings, Delegate.Combine(existingDelegate, setListenerDelegate));
#endif

            return CallTargetState.GetDefault();
        }

        private static object GetInstrumentationOptions()
        {
            Type optionsType = Type.GetType("MongoDB.Driver.Core.Extensions.DiagnosticSources.InstrumentationOptions, MongoDB.Driver.Core.Extensions.DiagnosticSources");

            var options = Activator.CreateInstance(optionsType);
            var publicProperty = BindingFlags.Public | BindingFlags.Instance;
            var shouldStartActivityLambda = GetShouldStartActivityExpression();

            optionsType.GetProperty("CaptureCommandText", publicProperty)?.SetValue(options, true);
            optionsType.GetProperty("ShouldStartActivity", publicProperty)?.SetValue(options, shouldStartActivityLambda.Compile());

            return options;
        }

        private static LambdaExpression GetShouldStartActivityExpression()
        {
            Expression<Func<string, bool>> shouldStartActivity = (string cmdName) => !Regex.IsMatch(cmdName, "isMaster|buildInfo|explain|killCursors", RegexOptions.Compiled);

            Type eventType = Type.GetType("MongoDB.Driver.Core.Events.CommandStartedEvent, MongoDB.Driver.Core");
            Type lambdaType = typeof(Func<,>).MakeGenericType(eventType, typeof(bool));

            var commandStartedEventParam = Expression.Parameter(eventType);
            var commandNameProperty = eventType.GetProperty("CommandName");
            var invokeExpression = Expression.Invoke(shouldStartActivity, Expression.MakeMemberAccess(commandStartedEventParam, commandNameProperty));
            var shouldStartActivityLambda = Expression.Lambda(lambdaType, invokeExpression, commandStartedEventParam);

            return shouldStartActivityLambda;
        }

        private static LambdaExpression GetClusterConfiguratorExpression()
        {
            Type eventSubscriberInterface = Type.GetType("MongoDB.Driver.Core.Events.IEventSubscriber, MongoDB.Driver.Core");
            Type clusterBuilderType = Type.GetType("MongoDB.Driver.Core.Configuration.ClusterBuilder, MongoDB.Driver.Core");
            Type listenerType = Type.GetType("MongoDB.Driver.Core.Extensions.DiagnosticSources.DiagnosticsActivityEventSubscriber, MongoDB.Driver.Core.Extensions.DiagnosticSources");

            var options = GetInstrumentationOptions();
            var listener = Activator.CreateInstance(listenerType, options);

            var mi = clusterBuilderType.GetMethods()
                .FirstOrDefault(x =>
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
}
