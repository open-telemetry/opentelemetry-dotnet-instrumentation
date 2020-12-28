using System;
using Datadog.Trace.ClrProfiler.CallTarget;

namespace CallTargetNativeTest.NoOp
{
    /// <summary>
    /// NoOp Integration for 3 Arguments and Void Return
    /// </summary>
    public static class Noop3ArgumentsVoidIntegration
    {
        public static CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, TArg3>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            CallTargetState returnValue = CallTargetState.GetDefault();
            string msg = $"{returnValue} {nameof(Noop3ArgumentsVoidIntegration)}.OnMethodBegin<{typeof(TTarget).FullName}, {typeof(TArg1).FullName}, {typeof(TArg2).FullName}, {typeof(TArg3).FullName}>({instance}, {arg1}, {arg2}, {arg3})";
            Console.WriteLine(msg);
            return returnValue;
        }

        public static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, CallTargetState state)
        {
            CallTargetReturn returnValue = CallTargetReturn.GetDefault();
            string msg = $"{returnValue} {nameof(Noop3ArgumentsVoidIntegration)}.OnMethodEnd<{typeof(TTarget).FullName}>({instance}, {exception}, {state})";
            Console.WriteLine(msg);
            return returnValue;
        }
    }
}
