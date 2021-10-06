using System;

namespace Datadog.Trace.DuckTyping.Tests.Properties.TypeChaining.ProxiesDefinitions
{
    public interface ITypesTuple
    {
        [DuckField]
        Type ProxyDefinitionType { get; }

        [DuckField]
        Type TargetType { get; }
    }
}
