namespace Datadog.Trace.DuckTyping.Tests.Fields.TypeChaining.ProxiesDefinitions
{
    public interface IObscureReadonlyErrorDuckType
    {
        [DuckField(Name = "_publicReadonlySelfTypeField")]
        IDummyFieldObject PublicReadonlySelfTypeField { get; set; }
    }
}
