using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    /// <summary>
    /// Attribute that allows us to declare JsonProperty attributes with
    /// NullValueHandling.Ignore, without running into the issue where the
    /// named argument is a custom type.
    /// </summary>
    internal class JsonPropertyIgnoreNullValueAttribute : JsonPropertyAttribute
    {
        internal JsonPropertyIgnoreNullValueAttribute(string propertyName)
            : base(propertyName)
        {
            NullValueHandling = NullValueHandling.Ignore;
        }
    }
}
