namespace Datadog.Core.Tools
{
    /// <summary>
    /// The canonical version for the dd-trace-dotnet libraries and tools.
    /// </summary>
    public class TracerVersion
    {
        /// <summary>
        /// The major portion of the current version.
        /// </summary>
        public const int Major = 0;

        /// <summary>
        /// The minor portion of the current version.
        /// </summary>
        public const int Minor = 0;

        /// <summary>
        /// The patch portion of the current version.
        /// </summary>
        public const int Patch = 1;

        /// <summary>
        /// Whether the current release is a pre-release
        /// </summary>
        public const bool IsPreRelease = false;
    }
}
