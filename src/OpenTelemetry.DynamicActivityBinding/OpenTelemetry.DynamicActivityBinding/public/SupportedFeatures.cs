using System;

namespace OpenTelemetry.DynamicActivityBinding
{
    public class SupportedFeatures
    {
        internal static readonly SupportedFeatures SingeltonInstance = new SupportedFeatures();

        public bool ActivityIdFormatOptions { get; internal set; }

        public bool FeatureSet_4020 { get; internal set; }

        public bool FeatureSet_5000 { get; internal set; }

        public string FormatFeatureSetSupportList()
        {
            return $"[{nameof(FeatureSet_5000)}={FeatureSet_5000}, {nameof(FeatureSet_4020)}={FeatureSet_4020}]";
        }
    }
}
