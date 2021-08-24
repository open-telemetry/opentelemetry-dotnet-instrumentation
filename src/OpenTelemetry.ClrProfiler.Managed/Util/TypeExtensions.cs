namespace OpenTelemetry.ClrProfiler.Managed.Util
{
    internal static class TypeExtensions
    {
        public static System.Type GetInstrumentedInterface(
            this object runtimeObject,
            string instrumentedInterfaceName)
        {
            if (runtimeObject == null)
            {
                return null;
            }

            var currentType = runtimeObject.GetType();
            var interfaces = currentType.GetInterfaces();

            foreach (var interfaceType in interfaces)
            {
                if ($"{interfaceType.Namespace}.{interfaceType.Name}" == instrumentedInterfaceName)
                {
                    return interfaceType;
                }
            }

            return null;
        }
    }
}
