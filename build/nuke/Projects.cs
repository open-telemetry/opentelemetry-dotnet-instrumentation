public static class Projects
{
    public const string ClrProfilerManaged = "OpenTelemetry.AutoInstrumentation";
    public const string ClrProfilerManagedCore = "OpenTelemetry.AutoInstrumentation.Core";
    public const string ClrProfilerManagedLoader = "OpenTelemetry.AutoInstrumentation.Loader";
    public const string ClrProfilerNative = "OpenTelemetry.AutoInstrumentation.Native";
    public const string StartupHook = "OpenTelemetry.AutoInstrumentation.StartupHook";
    public const string AdditionalDeps = "OpenTelemetry.AutoInstrumentation.AdditionalDeps";

    public static class Tests
    {
        public const string ClrProfilerNativeTests = "OpenTelemetry.AutoInstrumentation.Tests";
        public const string ClrProfilerManagedLoaderTests = "OpenTelemetry.AutoInstrumentation.Loader.Tests";
        public const string ClrProfilerManagedBootstrappingTests = "OpenTelemetry.AutoInstrumentation.Bootstrapping.Tests";
        public const string ClrProfilerManagedTests = "OpenTelemetry.AutoInstrumentation.Tests";

        public const string IntegrationTestsHelpers = "IntegrationTests.Helpers";
    }

    public static class Mocks
    {
        public const string ClrProfilerManagedMock = "OpenTelemetry.AutoInstrumentation.Mock";
    }
}
