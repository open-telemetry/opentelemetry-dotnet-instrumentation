using System;
using System.IO;

namespace Datadog.Trace.TestHelpers
{
    public class GacFixture
    {
        public void AddAssembliesToGac()
        {
#if NETFRAMEWORK
            var publish = new System.EnterpriseServices.Internal.Publish();

            var targetFolder = CustomTestFramework.GetProfilerTargetFolder();

            foreach (var file in Directory.GetFiles(targetFolder, "*.dll"))
            {
                publish.GacInstall(file);
            }
#endif
        }

        public void RemoveAssembliesFromGac()
        {
#if NETFRAMEWORK
            var publish = new System.EnterpriseServices.Internal.Publish();

            var targetFolder = CustomTestFramework.GetProfilerTargetFolder();

            foreach (var file in Directory.GetFiles(targetFolder, "*.dll"))
            {
                publish.GacRemove(file);
            }
#endif
        }
    }
}
