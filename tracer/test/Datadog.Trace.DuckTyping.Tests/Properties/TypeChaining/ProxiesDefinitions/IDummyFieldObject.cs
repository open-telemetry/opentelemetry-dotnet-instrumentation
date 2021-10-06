namespace Datadog.Trace.DuckTyping.Tests.Properties.TypeChaining.ProxiesDefinitions
{
    public interface IDummyFieldObject
    {
        [DuckField]
        int MagicNumber { get; set; }

        ITypesTuple this[ITypesTuple index] { get; set; }
    }
}
