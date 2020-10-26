using System;
using System.Threading.Tasks;
using Datadog.Util;
using OpenTelemetry.DynamicActivityBinding;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1201 // Elements must appear in the correct order
#pragma warning disable SA1214 // Readonly fields must appear before non-readonly fields
namespace OpenTelemetry.AutoInstrumentation.ActivityCollector
{
    internal sealed class ActivityCollector : IDisposable
    {
        #region Static APIs

        private static readonly object s_singeltonAccessLock = new object();
        private static ActivityCollector s_currentCollector;

        public static ActivityCollector Current { get { return s_currentCollector; } }

        public static bool TryCreateAndStart(IActivityCollectorConfiguration config, out ActivityCollector newActivityCollector)
        {
            Validate.NotNull(config, nameof(config));

            newActivityCollector = null;

            if (s_currentCollector != null)
            {
                return false;
            }

            lock (s_singeltonAccessLock)
            {
                if (s_currentCollector != null)
                {
                    return false;
                }

                s_currentCollector = new ActivityCollector(config);
                return true;
            }
        }

        #endregion Static APIs

        private readonly CollectAndExportBackgroundLoop _collectAndExportLoop;
        private readonly ActivityListenerStub _activityListenerHandle;
        private readonly Action<ActivityStub> _onActivityStartedProcessor;
        private readonly Action<ActivityStub> _onActivityStoppedProcessor;

        private ActivityCollector(IActivityCollectorConfiguration config)
        {
            Validate.NotNull(config, nameof(config));

            _onActivityStartedProcessor = config.OnActivityStartedProcessor;
            _onActivityStoppedProcessor = config.OnActivityStoppedProcessor;

            _collectAndExportLoop = new CollectAndExportBackgroundLoop(
                                                config.ExportInterval,
                                                config.ExportBatchSizeCap,
                                                config.AggregateActivitiesIntoTraces,
                                                config.ActivityExporter);
            _collectAndExportLoop.Start();

            // When we decide to support sampling before an Activity is creted, the ActivityListenerStub.CreateAndStart(..)
            // method will need to accept delegates for the underlying SampleXxx(..) methods of the ActivityListener
            // (such delegates will need to accep respective stubs).
            //
            // Sampling is not a concern of the ActivityCollector, it will likely be vendor-specific.
            // So the respespective delegates will be optional members on IActivityCollectorConfiguration.
            _activityListenerHandle = ActivityListenerStub.CreateAndStart(this.OnActivityStarted, this.OnActivityStopped);
        }

        private void OnActivityStarted(ActivityStub activity)
        {
            if (activity.IsNoOpStub)
            {
                return;
            }

            if (_onActivityStartedProcessor != null)
            {
                try
                {
                    _onActivityStartedProcessor(activity);
                }
                catch(Exception ex)
                {
                    Log.Error(nameof(ActivityCollector) + "." + nameof(OnActivityStarted), ex);
                }
            }

            _collectAndExportLoop.OnActivityStarted(activity);
        }

        private void OnActivityStopped(ActivityStub activity)
        {
            if (activity.IsNoOpStub)
            {
                return;
            }

            // Move this to exporter!
            // Add values controlled by the Tracer configuration
            //Tracer tracer = Tracer.Instance;
            //activity.AddServiceName(tracer.DefaultServiceName)
            //        .AddTags(tracer.Settings.GlobalTags) // Add global tags first so service unification tags added later can override them
            //        .AddEnvironment(tracer.Settings.Environment)
            //        .AddVersion(tracer.Settings.ServiceVersion)
            //        .AddAnalyticsSampleRate(tracer.Settings);

            if (_onActivityStoppedProcessor != null)
            {
                try
                {
                    _onActivityStoppedProcessor(activity);
                }
                catch (Exception ex)
                {
                    Log.Error(nameof(ActivityCollector) + "." + nameof(OnActivityStopped), ex);
                }
            }

            _collectAndExportLoop.OnActivityStopped(activity);
        }

        public void Dispose()
        {
            lock (s_singeltonAccessLock)
            {
                _collectAndExportLoop.Dispose();
                _activityListenerHandle.Dispose();

                s_currentCollector = null;
            }
        }

        /// <summary>
        /// The normal Dispose() method can block for a long time,
        /// becasue it waits for the backgroud collect-export loop to shut down.
        /// Call DisposeAsync(), in order to perform the wait on the threadpool instead of the current thread.
        /// This allows for an async wait for the shutdown to complete.
        /// </summary>
        /// <returns>A task representing the completion of the Dispose.</returns>
        public Task DisposeAsync()
        {
            return Task.Run(() =>
                            {
                                try
                                {
                                    Dispose();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(nameof(ActivityCollector) + "." + nameof(DisposeAsync), ex);
                                }
                            });
        }
    }
}

#pragma warning restore SA1214 // Readonly fields must appear before non-readonly fields
#pragma warning restore SA1201 // Elements must appear in the correct order
#pragma warning restore SA1124 // Do not use regions
