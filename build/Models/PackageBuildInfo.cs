namespace Models;

public class PackageBuildInfo
{
    public PackageBuildInfo(string libraryVersion, string[] supportedFrameworks)
    {
        LibraryVersion = libraryVersion;
        SupportedFrameworks = supportedFrameworks;
        AdditionalMetaData = new Dictionary<string, string>();
    }

    public PackageBuildInfo(string libraryVersion, string[] supportedFrameworks, Dictionary<string, string> additionalMetaData)
        : this(libraryVersion, supportedFrameworks)
    {
        AdditionalMetaData = additionalMetaData;
    }

    public string LibraryVersion { get; }

    public string[] SupportedFrameworks { get; }

    public Dictionary<string, string> AdditionalMetaData { get; }
}
