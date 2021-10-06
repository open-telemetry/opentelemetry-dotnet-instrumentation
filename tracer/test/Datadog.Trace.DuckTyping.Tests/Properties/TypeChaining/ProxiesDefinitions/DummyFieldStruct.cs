namespace Datadog.Trace.DuckTyping.Tests.Properties.TypeChaining.ProxiesDefinitions
{
#pragma warning disable 649

    [DuckCopy]
    public struct DummyFieldStruct
    {
        [DuckField]
        public int MagicNumber;
    }
}
