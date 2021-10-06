namespace Datadog.Trace.Logging
{
    internal interface ILogProviderWithEnricher
    {
        ILogEnricher CreateEnricher();
    }
}
