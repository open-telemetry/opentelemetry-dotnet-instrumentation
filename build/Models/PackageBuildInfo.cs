namespace Models;

public class PackageBuildInfo
{
    public PackageBuildInfo(string libraryVersion)
    {
        LibraryVersion = libraryVersion;
        AdditionalMetaData = new Dictionary<string, string>();
    }

    public PackageBuildInfo(string libraryVersion, Dictionary<string, string> additionalMetaData)
        : this(libraryVersion)
    {
        AdditionalMetaData = additionalMetaData;
    }

    public string LibraryVersion { get; private set; }

    public Dictionary<string, string> AdditionalMetaData { get; private set; }
}
