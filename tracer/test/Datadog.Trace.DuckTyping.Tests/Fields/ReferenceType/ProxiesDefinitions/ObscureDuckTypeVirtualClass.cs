namespace Datadog.Trace.DuckTyping.Tests.Fields.ReferenceType.ProxiesDefinitions
{
    public class ObscureDuckTypeVirtualClass
    {
        [DuckField(Name = "_publicStaticReadonlyReferenceTypeField")]
        public virtual string PublicStaticReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_internalStaticReadonlyReferenceTypeField")]
        public virtual string InternalStaticReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_protectedStaticReadonlyReferenceTypeField")]
        public virtual string ProtectedStaticReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_privateStaticReadonlyReferenceTypeField")]
        public virtual string PrivateStaticReadonlyReferenceTypeField { get; }

        // *

        [DuckField(Name = "_publicStaticReferenceTypeField")]
        public virtual string PublicStaticReferenceTypeField { get; set; }

        [DuckField(Name = "_internalStaticReferenceTypeField")]
        public virtual string InternalStaticReferenceTypeField { get; set; }

        [DuckField(Name = "_protectedStaticReferenceTypeField")]
        public virtual string ProtectedStaticReferenceTypeField { get; set; }

        [DuckField(Name = "_privateStaticReferenceTypeField")]
        public virtual string PrivateStaticReferenceTypeField { get; set; }

        // *

        [DuckField(Name = "_publicReadonlyReferenceTypeField")]
        public virtual string PublicReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_internalReadonlyReferenceTypeField")]
        public virtual string InternalReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_protectedReadonlyReferenceTypeField")]
        public virtual string ProtectedReadonlyReferenceTypeField { get; }

        [DuckField(Name = "_privateReadonlyReferenceTypeField")]
        public virtual string PrivateReadonlyReferenceTypeField { get; }

        // *

        [DuckField(Name = "_publicReferenceTypeField")]
        public virtual string PublicReferenceTypeField { get; set; }

        [DuckField(Name = "_internalReferenceTypeField")]
        public virtual string InternalReferenceTypeField { get; set; }

        [DuckField(Name = "_protectedReferenceTypeField")]
        public virtual string ProtectedReferenceTypeField { get; set; }

        [DuckField(Name = "_privateReferenceTypeField")]
        public virtual string PrivateReferenceTypeField { get; set; }
    }
}
