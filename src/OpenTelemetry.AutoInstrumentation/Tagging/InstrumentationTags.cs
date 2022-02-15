using System.Diagnostics;
using OpenTelemetry.ClrProfiler.Managed.Util;

namespace OpenTelemetry.ClrProfiler.Managed.Tagging
{
    internal abstract class InstrumentationTags : CommonTags
    {
        protected static readonly IProperty<string>[] InstrumentationTagsProperties =
            CommonTagsProperties.Concat(
                new ReadOnlyProperty<InstrumentationTags, string>(Tags.SpanKind, t => t.Kind.ToString()));

        public abstract ActivityKind Kind { get; }

        protected override IProperty<string>[] GetAdditionalTags() => InstrumentationTagsProperties;
    }
}
