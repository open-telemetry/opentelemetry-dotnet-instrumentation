namespace Datadog.Trace.DuckTyping.Tests.Fields.ValueType.ProxiesDefinitions
{
    public interface IObscureStaticReadonlyErrorDuckType
    {
        [DuckField(Name = "_publicStaticReadonlyValueTypeField")]
        int PublicStaticReadonlyValueTypeField { get; set; }
    }
}
