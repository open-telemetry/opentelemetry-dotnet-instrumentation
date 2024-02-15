namespace Models;

public class PackageBuildInfo
{
    public PackageBuildInfo(string libraryVersion) : this(libraryVersion, Array.Empty<string>())
    {
    }

    public PackageBuildInfo(string libraryVersion, string[] supportedFrameworks) : this(libraryVersion, supportedFrameworks, new Dictionary<string, string>())
    {
    }

    public PackageBuildInfo(string libraryVersion, string[] supportedFrameworks, Dictionary<string, string> additionalMetaData)
    {
        LibraryVersion = libraryVersion;
        SupportedFrameworks = supportedFrameworks;
        AdditionalMetaData = additionalMetaData;
    }

    public string LibraryVersion { get; }

    public string[] SupportedFrameworks { get; }

    public Dictionary<string, string> AdditionalMetaData { get; }
}
