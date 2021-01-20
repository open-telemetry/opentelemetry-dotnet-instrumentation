namespace Datadog.Trace.RuntimeMetrics
{
    internal static class MetricsNames
    {
        public const string ExceptionsCount = "runtime.dotnet.exceptions.count";

        public const string Gen0CollectionsCount = "runtime.dotnet.gc.count.gen0";
        public const string Gen1CollectionsCount = "runtime.dotnet.gc.count.gen1";
        public const string Gen2CollectionsCount = "runtime.dotnet.gc.count.gen2";

        public const string GcPauseTime = "runtime.dotnet.gc.pause_time";
        public const string GcMemoryLoad = "runtime.dotnet.gc.memory_load";

        public const string Gen0HeapSize = "runtime.dotnet.gc.size.gen0";
        public const string Gen1HeapSize = "runtime.dotnet.gc.size.gen1";
        public const string Gen2HeapSize = "runtime.dotnet.gc.size.gen2";
        public const string LohSize = "runtime.dotnet.gc.size.loh";

        public const string ContentionTime = "runtime.dotnet.threads.contention_time";
        public const string ContentionCount = "runtime.dotnet.threads.contention_count";

        public const string ThreadPoolWorkersCount = "runtime.dotnet.threads.workers_count";

        public const string ThreadsCount = "runtime.dotnet.threads.count";

        public const string CommittedMemory = "runtime.dotnet.mem.committed";

        public const string CpuUserTime = "runtime.dotnet.cpu.user";
        public const string CpuSystemTime = "runtime.dotnet.cpu.system";
        public const string CpuPercentage = "runtime.dotnet.cpu.percent";
    }
}
