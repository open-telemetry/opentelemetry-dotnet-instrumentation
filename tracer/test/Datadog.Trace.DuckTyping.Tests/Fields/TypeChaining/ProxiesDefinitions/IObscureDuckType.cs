namespace Datadog.Trace.DuckTyping.Tests.Fields.TypeChaining.ProxiesDefinitions
{
    public interface IObscureDuckType
    {
        [DuckField(Name = "_publicStaticReadonlySelfTypeField")]
        IDummyFieldObject PublicStaticReadonlySelfTypeField { get; }

        [DuckField(Name = "_internalStaticReadonlySelfTypeField")]
        IDummyFieldObject InternalStaticReadonlySelfTypeField { get; }

        [DuckField(Name = "_protectedStaticReadonlySelfTypeField")]
        IDummyFieldObject ProtectedStaticReadonlySelfTypeField { get; }

        [DuckField(Name = "_privateStaticReadonlySelfTypeField")]
        IDummyFieldObject PrivateStaticReadonlySelfTypeField { get; }

        // *

        [DuckField(Name = "_publicStaticSelfTypeField")]
        IDummyFieldObject PublicStaticSelfTypeField { get; set; }

        [DuckField(Name = "_internalStaticSelfTypeField")]
        IDummyFieldObject InternalStaticSelfTypeField { get; set; }

        [DuckField(Name = "_protectedStaticSelfTypeField")]
        IDummyFieldObject ProtectedStaticSelfTypeField { get; set; }

        [DuckField(Name = "_privateStaticSelfTypeField")]
        IDummyFieldObject PrivateStaticSelfTypeField { get; set; }

        // *

        [DuckField(Name = "_publicReadonlySelfTypeField")]
        IDummyFieldObject PublicReadonlySelfTypeField { get; }

        [DuckField(Name = "_internalReadonlySelfTypeField")]
        IDummyFieldObject InternalReadonlySelfTypeField { get; }

        [DuckField(Name = "_protectedReadonlySelfTypeField")]
        IDummyFieldObject ProtectedReadonlySelfTypeField { get; }

        [DuckField(Name = "_privateReadonlySelfTypeField")]
        IDummyFieldObject PrivateReadonlySelfTypeField { get; }

        // *

        [DuckField(Name = "_publicSelfTypeField")]
        IDummyFieldObject PublicSelfTypeField { get; set; }

        [DuckField(Name = "_internalSelfTypeField")]
        IDummyFieldObject InternalSelfTypeField { get; set; }

        [DuckField(Name = "_protectedSelfTypeField")]
        IDummyFieldObject ProtectedSelfTypeField { get; set; }

        [DuckField(Name = "_privateSelfTypeField")]
        IDummyFieldObject PrivateSelfTypeField { get; set; }
    }
}
