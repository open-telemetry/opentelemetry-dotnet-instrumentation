namespace Datadog.Trace.DuckTyping.Tests.Fields.TypeChaining.ProxiesDefinitions
{
    public interface IObscureStaticReadonlyErrorDuckType
    {
        [DuckField(Name = "_publicStaticReadonlySelfTypeField")]
        IDummyFieldObject PublicStaticReadonlySelfTypeField { get; set; }
    }
}
