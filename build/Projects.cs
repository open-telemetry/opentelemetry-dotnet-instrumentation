public static class Projects
{
    public const string AutoInstrumentation = "OpenTelemetry.AutoInstrumentation";
    public const string AutoInstrumentationAssemblies = "OpenTelemetry.AutoInstrumentation.Assemblies";
    public const string AutoInstrumentationLoader = "OpenTelemetry.AutoInstrumentation.Loader";
    public const string AutoInstrumentationNative = "OpenTelemetry.AutoInstrumentation.Native";
    public const string AutoInstrumentationStartupHook = "OpenTelemetry.AutoInstrumentation.StartupHook";
    public const string AutoInstrumentationAdditionalDeps = "OpenTelemetry.AutoInstrumentation.AdditionalDeps";
    public const string AutoInstrumentationAspNetCoreBootstrapper = "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper";

    public static class Mocks
    {
        public const string AutoInstrumentationMock = "OpenTelemetry.AutoInstrumentation.Mock";
    }

    public static class Tests
    {
        public const string AutoInstrumentationNativeTests = "OpenTelemetry.AutoInstrumentation.Native.Tests";
        public const string AutoInstrumentationBuildTasksTests = "OpenTelemetry.AutoInstrumentation.BuildTasks.Tests";
        public const string AutoInstrumentationLoaderTests = "OpenTelemetry.AutoInstrumentation.Loader.Tests";
        public const string AutoInstrumentationBootstrappingTests = "OpenTelemetry.AutoInstrumentation.Bootstrapping.Tests";
        public const string AutoInstrumentationTests = "OpenTelemetry.AutoInstrumentation.Tests";
        public const string AutoInstrumentationStartupHookTests = "OpenTelemetry.AutoInstrumentation.StartupHook.Tests";
        public const string IntegrationTests = "IntegrationTests";

        public static class Applications
        {
            public const string AspNet = "TestApplication.AspNet.NetFramework";
            public const string ContinuousProfilerNativeDep = "TestApplication.ContinuousProfiler.NativeDep";
            public const string OwinIis = "TestApplication.Owin.IIS.NetFramework";
            public const string WcfIis = "TestApplication.Wcf.Server.IIS.NetFramework";
            public const string WcfServer = "TestApplication.Wcf.Server.NetFramework";
        }
    }

    public static class Tools
    {
        public const string LibraryVersionsGenerator = "LibraryVersionsGenerator";
        public const string GacInstallTool = "GacInstallTool";
        public const string SdkVersionAnalyzerTool = "SdkVersionAnalyzer";
    }
}
