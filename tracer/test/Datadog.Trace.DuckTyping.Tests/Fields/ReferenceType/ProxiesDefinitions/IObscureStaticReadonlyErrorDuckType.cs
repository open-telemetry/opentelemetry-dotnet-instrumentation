namespace Datadog.Trace.DuckTyping.Tests.Fields.ReferenceType.ProxiesDefinitions
{
    public interface IObscureStaticReadonlyErrorDuckType
    {
        [DuckField(Name = "_publicStaticReadonlyReferenceTypeField")]
        string PublicStaticReadonlyReferenceTypeField { get; set; }
    }
}
