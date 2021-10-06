using System.Reflection.Emit;

namespace Datadog.Trace.ClrProfiler.CallTarget.Handlers
{
    internal readonly struct CreateAsyncEndMethodResult
    {
        public readonly DynamicMethod Method;
        public readonly bool PreserveContext;

        public CreateAsyncEndMethodResult(DynamicMethod method, bool preserveContext)
        {
            Method = method;
            PreserveContext = preserveContext;
        }
    }
}
