// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation;
using OpenTelemetry.AutoInstrumentation.PluginApi;

namespace TestLibrary.NativeAsync;

public sealed class Plugin : IPlugin
{
    private const byte DirectCallTargetKind = 0;
    private const uint TracingCategory = 1;

    public void Initializing()
    {
    }

    public void Initialized()
    {
    }

#pragma warning disable CA1822 // PluginManager discovers this instance method by reflection.
    internal InstrumentationDefinitions.SharedPayload GetAllDefinitionsPayload()
#pragma warning restore CA1822 // PluginManager discovers this instance method by reflection.
    {
        return new InstrumentationDefinitions.SharedPayload
        {
            DefinitionsId = "EEC3706304E349A5AF1B8D33E9325EB4",
            Definitions =
            [
                new(
                    "TestApplication.NativeAsync",
                    "TestApplication.NativeAsync.RuntimeAsyncTarget",
                    "TaskWithTaskReturningCallbackAsync",
                    ["System.Threading.Tasks.Task"],
                    1,
                    0,
                    0,
                    1,
                    ushort.MaxValue,
                    ushort.MaxValue,
                    typeof(TaskReturningNonGenericAsyncCallbackIntegration).Assembly.FullName!,
                    typeof(TaskReturningNonGenericAsyncCallbackIntegration).FullName!,
                    DirectCallTargetKind,
                    TracingCategory),
                new(
                    "TestApplication.NativeAsync",
                    "TestApplication.NativeAsync.RuntimeAsyncTarget",
                    "TaskOfTWithTaskReturningCallbackAsync",
                    ["System.Threading.Tasks.Task`1<System.Int32>"],
                    1,
                    0,
                    0,
                    1,
                    ushort.MaxValue,
                    ushort.MaxValue,
                    typeof(TaskReturningAsyncCallbackIntegration).Assembly.FullName!,
                    typeof(TaskReturningAsyncCallbackIntegration).FullName!,
                    DirectCallTargetKind,
                    TracingCategory),
            ]
        };
    }
}
