namespace Datadog.Trace.DuckTyping.Tests.Fields.ValueType.ProxiesDefinitions
{
    public interface IObscureReadonlyErrorDuckType
    {
        [DuckField(Name = "_publicReadonlyValueTypeField")]
        int PublicReadonlyValueTypeField { get; set; }
    }
}
