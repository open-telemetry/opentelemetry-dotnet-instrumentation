namespace Datadog.Trace.DuckTyping.Tests.Fields.ReferenceType.ProxiesDefinitions
{
    public interface IObscureReadonlyErrorDuckType
    {
        [DuckField(Name = "_publicReadonlyReferenceTypeField")]
        string PublicReadonlyReferenceTypeField { get; set; }
    }
}
