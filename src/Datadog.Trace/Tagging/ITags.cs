using System.Collections.Generic;

namespace Datadog.Trace.Tagging
{
    internal interface ITags
    {
        List<KeyValuePair<string, double>> Metrics { get; }

        List<KeyValuePair<string, string>> GetAllTags();

        string GetTag(string key);

        void SetTag(string key, string value);

        double? GetMetric(string key);

        void SetMetric(string key, double? value);

        int SerializeTo(ref byte[] buffer, int offset);
    }
}
