using System;




namespace OpenTelemetry.DynamicActivityBinding
{
    public static class ActivityFactory
    {
        private static readonly ActivityStub DynamicLoaderErrorPlaceholder = new ActivityStub(null);

        internal static class SupportedFeatures
        {
            public static bool ActivityIsDisposable { get { return true; } }
        }

        public static bool IsEnabled { get { return DynamicLoader.EnsureInitialized(); } }
        
        public static ActivityStub Create()
        {
            if (! DynamicLoader.EnsureInitialized())
            {
                return DynamicLoaderErrorPlaceholder;
            }

            var activity = new ActivityStub();
            return activity;
        }
    }
}
