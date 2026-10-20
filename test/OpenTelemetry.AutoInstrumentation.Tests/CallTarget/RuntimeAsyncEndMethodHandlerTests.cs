// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

namespace OpenTelemetry.AutoInstrumentation.Tests.CallTarget;

public sealed class RuntimeAsyncEndMethodHandlerTests
{
    [Fact]
    public void NonGenericTaskRunsBothCallbacksWithCompletedTask()
    {
        var target = new Target();
        var state = CallTargetState.GetDefault();

        CallTargetInvoker.EndMethodRuntimeAsync<NonGenericTaskIntegration, Target, Task>(target, null, in state);

        Assert.Equal(1, NonGenericTaskIntegration.AsyncCalls);
        Assert.Equal(1, NonGenericTaskIntegration.EndCalls);
        Assert.True(NonGenericTaskIntegration.ReceivedCompletedTask);
    }

    [Fact]
    public void GenericTaskFeedsAsyncReplacementIntoOnMethodEndAndUnwrapsReplacement()
    {
        var target = new Target();
        var state = CallTargetState.GetDefault();

        var result = CallTargetInvoker.EndMethodRuntimeAsync<GenericTaskIntegration, Target, int, Task<int>>(
            target, 40, null, in state);

        Assert.Equal(42, GenericTaskIntegration.EndMethodValue);
        Assert.Equal(43, result.GetReturnValue());
    }

    [Fact]
    public void GenericValueTaskSynthesizesAndUnwrapsDeclaredReturn()
    {
        var target = new Target();
        var state = CallTargetState.GetDefault();

        var result = CallTargetInvoker.EndMethodRuntimeAsync<GenericValueTaskIntegration, Target, int, ValueTask<int>>(
            target, 10, null, in state);

        Assert.Equal(10, GenericValueTaskIntegration.EndMethodValue);
        Assert.Equal(11, result.GetReturnValue());
    }

    [Fact]
    public void SynchronousCallbackReturningTaskIsNotMistakenForAsyncCallback()
    {
        var target = new Target();
        var state = CallTargetState.GetDefault();
        var innerTask = Task.CompletedTask;

        var result = CallTargetInvoker.EndMethodRuntimeAsync<NestedTaskIntegration, Target, Task, Task<Task>>(
            target, innerTask, null, in state);

        Assert.Same(innerTask, result.GetReturnValue());
        Assert.Equal(1, NestedTaskIntegration.Calls);
        Assert.True(IntegrationOptions<NestedTaskIntegration, Target>.IsIntegrationEnabled);
    }

    [Fact]
    public void AsyncCallbackDisablesIntegrationForTarget()
    {
        var target = new Target();
        var state = CallTargetState.GetDefault();

        var result = CallTargetInvoker.EndMethodRuntimeAsync<UnsupportedAsyncIntegration, Target, int, Task<int>>(
            target, 7, null, in state);

        Assert.Equal(7, result.GetReturnValue());
        Assert.False(IntegrationOptions<UnsupportedAsyncIntegration, Target>.IsIntegrationEnabled);
    }

    [Fact]
    public void RuntimeAsyncEndDoesNotRestorePreviousActivity()
    {
        using var previous = new Activity("previous");
        previous.Start();
        using var current = new Activity("current");
        current.Start();
        var state = new CallTargetState(previous, CallTargetState.GetDefault());
        var target = new Target();

        CallTargetInvoker.EndMethodRuntimeAsync<NoCallbacksIntegration, Target, Task>(target, null, in state);

        Assert.Same(current, Activity.Current);
    }

    [Fact]
    public void HelperRejectsIncompleteReplacementTask()
    {
        var completion = new TaskCompletionSource<int>();

        Assert.False(RuntimeAsyncHelper.TryGetCompletedResult<int, Task<int>>(completion.Task, out _));

        var completedTask = RuntimeAsyncHelper.CreateCompletedFromResult<int, Task<int>>(12);
        Assert.True(RuntimeAsyncHelper.TryGetCompletedResult<int, Task<int>>(completedTask, out var taskResult));
        Assert.Equal(12, taskResult);

        var completedValueTask = RuntimeAsyncHelper.CreateCompletedFromResult<int, ValueTask<int>>(13);
        Assert.True(RuntimeAsyncHelper.TryGetCompletedResult<int, ValueTask<int>>(completedValueTask, out var valueTaskResult));
        Assert.Equal(13, valueTaskResult);
    }

#pragma warning disable CA1812 // Types are instantiated indirectly as CallTarget generic arguments.
    private sealed class Target;

    private sealed class NonGenericTaskIntegration
    {
        internal static int AsyncCalls { get; private set; }

        internal static int EndCalls { get; private set; }

        internal static bool ReceivedCompletedTask { get; private set; }

        internal static object? OnAsyncMethodEnd<TTarget>(TTarget instance, object? returnValue, Exception? exception, in CallTargetState state)
        {
            AsyncCalls++;
            return returnValue;
        }

        internal static CallTargetReturn<Task> OnMethodEnd<TTarget>(TTarget instance, Task returnValue, Exception? exception, in CallTargetState state)
        {
            EndCalls++;
            ReceivedCompletedTask = returnValue.IsCompletedSuccessfully;
            return new CallTargetReturn<Task>(returnValue);
        }
    }

    private sealed class GenericTaskIntegration
    {
        internal static int EndMethodValue { get; private set; }

        internal static int OnAsyncMethodEnd<TTarget>(TTarget instance, int returnValue, Exception? exception, in CallTargetState state)
            => returnValue + 2;

        internal static CallTargetReturn<Task<int>> OnMethodEnd<TTarget>(TTarget instance, Task<int> returnValue, Exception? exception, in CallTargetState state)
        {
            EndMethodValue = returnValue.Result;
            return new CallTargetReturn<Task<int>>(Task.FromResult(returnValue.Result + 1));
        }
    }

    private sealed class GenericValueTaskIntegration
    {
        internal static int EndMethodValue { get; private set; }

        internal static CallTargetReturn<ValueTask<int>> OnMethodEnd<TTarget>(TTarget instance, ValueTask<int> returnValue, Exception? exception, in CallTargetState state)
        {
            EndMethodValue = returnValue.Result;
            return new CallTargetReturn<ValueTask<int>>(new ValueTask<int>(returnValue.Result + 1));
        }
    }

    private sealed class NestedTaskIntegration
    {
        internal static int Calls { get; private set; }

        internal static Task OnAsyncMethodEnd<TTarget>(TTarget instance, Task returnValue, Exception? exception, in CallTargetState state)
        {
            Calls++;
            return returnValue;
        }
    }

    private sealed class UnsupportedAsyncIntegration
    {
        internal static Task<int> OnAsyncMethodEnd<TTarget>(TTarget instance, int returnValue, Exception? exception, in CallTargetState state)
            => Task.FromResult(returnValue + 1);
    }

    private sealed class NoCallbacksIntegration;
#pragma warning restore CA1812 // Types are instantiated indirectly as CallTarget generic arguments.
}

#endif
