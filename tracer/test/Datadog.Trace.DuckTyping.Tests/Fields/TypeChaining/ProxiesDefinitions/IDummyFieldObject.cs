namespace Datadog.Trace.DuckTyping.Tests.Fields.TypeChaining.ProxiesDefinitions
{
    public interface IDummyFieldObject
    {
        [DuckField]
        int MagicNumber { get; set; }
    }
}
