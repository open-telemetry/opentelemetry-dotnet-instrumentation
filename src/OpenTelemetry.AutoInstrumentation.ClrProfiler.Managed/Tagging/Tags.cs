using System.Diagnostics;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Tagging
{
    /// <summary>
    /// Standard span tags used by integrations.
    /// </summary>
    public static class Tags
    {
        /// <summary>
        /// The environment of the profiled service.
        /// </summary>
        public const string Env = "env";

        /// <summary>
        /// The version of the profiled service.
        /// </summary>
        public const string Version = "version";

        /// <summary>
        /// The name of the integration that generated the span.
        /// Use OpenTracing tag "component"
        /// </summary>
        public const string InstrumentationName = "component";

        /// <summary>
        /// The kind of span (e.g. client, server).
        /// </summary>
        /// <seealso cref="Activity.Kind"/>
        public const string SpanKind = "span.kind";

        /// <summary>
        /// The error message of an exception
        /// </summary>
        public const string ErrorMsg = "error.msg";

        /// <summary>
        /// The type of an exception
        /// </summary>
        public const string ErrorType = "error.type";

        /// <summary>
        /// The stack trace of an exception
        /// </summary>
        public const string ErrorStack = "error.stack";

        /// <summary>
        /// The status of a span
        /// </summary>
        public const string Status = "status";

        /// <summary>
        /// The hostname of a outgoing server connection.
        /// </summary>
        public const string OutHost = "out.host";

        /// <summary>
        /// The port of a outgoing server connection.
        /// </summary>
        public const string OutPort = "out.port";

        /// <summary>
        /// A MongoDB query.
        /// </summary>
        public const string MongoDbQuery = "mongodb.query";

        /// <summary>
        /// A MongoDB database name.
        /// </summary>
        public const string MongoDbName = "mongodb.name";

        /// <summary>
        /// A MongoDB collection name.
        /// </summary>
        public const string MongoDbCollection = "mongodb.collection";
    }
}
