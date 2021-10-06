using System;

namespace Datadog.Trace.DuckTyping.Tests.Methods.ProxiesDefinitions
{
    public class DefaultGenericMethodDuckTypeVirtualClass
    {
        public virtual T GetDefault<T>() => default;

        public virtual Tuple<T1, T2> Wrap<T1, T2>(T1 a, T2 b) => null;

        public virtual void ForEachScope<TState2>(Action<object, TState2> callback, TState2 state)
        {
        }
    }
}
