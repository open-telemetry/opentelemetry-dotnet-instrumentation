namespace Datadog.Trace.AppSec.Agent
{
    internal interface IAppSecAgentWriter
    {
        void AddEvent(IEvent @event);

        void Shutdown();
    }
}
