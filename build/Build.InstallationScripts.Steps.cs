using Nuke.Common;
using Nuke.Common.IO;

partial class Build
{
    AbsolutePath InstallationScriptsDirectory => OutputDirectory / "installation-scripts";

    Target BuildInstallationScripts => _ => _
        .After(Clean)
        .After(CreateRequiredDirectories)
        .Executes(() =>
        {
            var scriptTemplates = RootDirectory / "script-templates";
            var templateFiles = scriptTemplates.GetFiles();
            foreach (var templateFile in templateFiles)
            {
                var scriptFile = InstallationScriptsDirectory / templateFile.Name.Replace(".template", "");
                templateFile.Copy(scriptFile, ExistsPolicy.FileOverwrite);
                scriptFile.UpdateText(x =>
                    x.Replace("{{VERSION}}", VersionHelper.GetVersion()));
            }
        });
}
