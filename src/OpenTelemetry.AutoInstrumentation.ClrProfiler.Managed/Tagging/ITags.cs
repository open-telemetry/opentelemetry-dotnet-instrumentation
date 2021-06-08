using System.Collections.Generic;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Tagging
{
    internal interface ITags
    {
        List<KeyValuePair<string, string>> GetAllTags();

        string GetTag(string key);

        void SetTag(string key, string value);
    }
}
