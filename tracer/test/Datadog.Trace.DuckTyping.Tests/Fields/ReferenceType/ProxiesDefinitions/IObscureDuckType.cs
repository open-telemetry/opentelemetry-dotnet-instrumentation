namespace Datadog.Trace.DuckTyping.Tests.Fields.ReferenceType.ProxiesDefinitions
{
    public interface IObscureDuckType
    {
        [DuckField(Name = "_publicStaticReadonlyReferenceTypeField")]
        string PublicStaticReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_internalStaticReadonlyReferenceTypeField")]
        string InternalStaticReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_protectedStaticReadonlyReferenceTypeField")]
        string ProtectedStaticReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_privateStaticReadonlyReferenceTypeField")]
        string PrivateStaticReadonlyReferenceTypeField { get; }

        // *

        [DuckField(Name = "_publicStaticReferenceTypeField")]
        string PublicStaticReferenceTypeField { get; set; }

        [DuckField(Name = "_internalStaticReferenceTypeField")]
        string InternalStaticReferenceTypeField { get; set; }

        [DuckField(Name = "_protectedStaticReferenceTypeField")]
        string ProtectedStaticReferenceTypeField { get; set; }

        [DuckField(Name = "_privateStaticReferenceTypeField")]
        string PrivateStaticReferenceTypeField { get; set; }

        // *

        [DuckField(Name = "_publicReadonlyReferenceTypeField")]
        string PublicReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_internalReadonlyReferenceTypeField")]
        string InternalReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_protectedReadonlyReferenceTypeField")]
        string ProtectedReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_privateReadonlyReferenceTypeField")]
        string PrivateReadonlyReferenceTypeField { get; }

        // *

        [DuckField(Name = "_publicReferenceTypeField")]
        string PublicReferenceTypeField { get; set; }

        [DuckField(Name = "_internalReferenceTypeField")]
        string InternalReferenceTypeField { get; set; }

        [DuckField(Name = "_protectedReferenceTypeField")]
        string ProtectedReferenceTypeField { get; set; }

        [DuckField(Name = "_privateReferenceTypeField")]
        string PrivateReferenceTypeField { get; set; }
    }
}
