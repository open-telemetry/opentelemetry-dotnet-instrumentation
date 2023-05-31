namespace DependencyListGenerator.DotNetOutdated;

internal class TempDirectory : IDisposable
{
    private string tempPath;
    private string tempDirName;

    public TempDirectory()
    {
        tempPath = Path.GetTempPath();
        tempDirName = Path.GetRandomFileName();
        Directory.CreateDirectory(DirectoryPath);
    }

    public string DirectoryPath
    {
        get => Path.Combine(tempPath, tempDirName);
    }

    public void Dispose()
    {
        Directory.Delete(DirectoryPath, true);
    }
}
