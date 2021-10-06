namespace Datadog.Trace.DuckTyping.Tests.Methods.ProxiesDefinitions
{
    public interface IDummyFieldObject
    {
        [DuckField]
        int MagicNumber { get; set; }
    }
}
