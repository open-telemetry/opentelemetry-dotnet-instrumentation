namespace Models;

public class PackageBuildInfo
{
    public PackageBuildInfo(string libraryVersion, string[] supportedFrameworks = null, string[] supportedPlatforms = null, Dictionary<string, string> additionalMetaData = null)
    {
        LibraryVersion = libraryVersion;
        SupportedFrameworks = supportedFrameworks ?? Array.Empty<string>();
        SupportedPlatforms = supportedPlatforms ?? Array.Empty<string>();
        AdditionalMetaData = additionalMetaData ?? new Dictionary<string, string>();
    }

    public string LibraryVersion { get; }

    public string[] SupportedFrameworks { get; }

    public string[] SupportedPlatforms { get; }

    public Dictionary<string, string> AdditionalMetaData { get; }
}
