using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.DiagnosticListeners;
using Datadog.Trace.DogStatsd;
using Datadog.Trace.Logging;
using Datadog.Trace.Sampling;
using Datadog.Trace.Vendors.StatsdClient;
using Newtonsoft.Json;

namespace Datadog.Trace
{
    /// <summary>
    /// The tracer is responsible for creating spans and flushing them to the Datadog agent
    /// </summary>
    public class Tracer : IDatadogTracer
    {
        private const string UnknownServiceName = "UnknownService";
        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.For<Tracer>();

        /// <summary>
        /// The number of Tracer instances that have been created and not yet destroyed.
        /// This is used in the heartbeat metrics to estimate the number of
        /// "live" Tracers that could potentially be sending traces to the Agent.
        /// </summary>
        private static int _liveTracerCount;

        /// <summary>
        /// Indicates whether we're initializing a tracer for the first time
        /// </summary>
        private static int _firstInitialization = 1;

        private readonly IScopeManager _scopeManager;
        private readonly Timer _heartbeatTimer;

        private IAgentWriter _agentWriter;

        static Tracer()
        {
            TracingProcessManager.Initialize();
            // create the default global Tracer
            Instance = new Tracer();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer"/> class with default settings.
        /// </summary>
        public Tracer()
            : this(settings: null, agentWriter: null, sampler: null, scopeManager: null, statsd: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer"/>
        /// class using the specified <see cref="IConfigurationSource"/>.
        /// </summary>
        /// <param name="settings">
        /// A <see cref="TracerSettings"/> instance with the desired settings,
        /// or null to use the default configuration sources.
        /// </param>
        public Tracer(TracerSettings settings)
            : this(settings, agentWriter: null, sampler: null, scopeManager: null, statsd: null)
        {
        }

        internal Tracer(TracerSettings settings, IAgentWriter agentWriter, ISampler sampler, IScopeManager scopeManager, IStatsd statsd)
        {
            // update the count of Tracer instances
            Interlocked.Increment(ref _liveTracerCount);

            Settings = settings ?? TracerSettings.FromDefaultSources();

            // if not configured, try to determine an appropriate service name
            DefaultServiceName = Settings.ServiceName ??
                                 GetApplicationName() ??
                                 UnknownServiceName;

            // only set DogStatsdClient if tracer metrics are enabled
            if (Settings.TracerMetricsEnabled)
            {
                // Run this first in case the port override is ready
                TracingProcessManager.SubscribeToDogStatsDPortOverride(
                    port =>
                    {
                        Log.Debug("Attempting to override dogstatsd port with {0}", port);
                        Statsd = CreateDogStatsdClient(Settings, DefaultServiceName, port);
                    });

                Statsd = statsd ?? CreateDogStatsdClient(Settings, DefaultServiceName, Settings.DogStatsdPort);
            }

            // Run this first in case the port override is ready
            TracingProcessManager.SubscribeToTraceAgentPortOverride(
                port =>
                {
                    Log.Debug("Attempting to override trace agent port with {0}", port);
                    var builder = new UriBuilder(Settings.AgentUri) { Port = port };
                    var baseEndpoint = builder.Uri;
                    IApi overridingApiClient = new Api(baseEndpoint, apiRequestFactory: null, Statsd);
                    if (_agentWriter == null)
                    {
                        _agentWriter = _agentWriter ?? new AgentWriter(overridingApiClient, Statsd);
                    }
                    else
                    {
                        _agentWriter.OverrideApi(overridingApiClient);
                    }
                });

            // fall back to default implementations of each dependency if not provided
            _agentWriter = agentWriter ?? new AgentWriter(new Api(Settings.AgentUri, apiRequestFactory: null, Statsd), Statsd);

            _scopeManager = scopeManager ?? new AsyncLocalScopeManager();
            Sampler = sampler ?? new RuleBasedSampler(new RateLimiter(Settings.MaxTracesSubmittedPerSecond));

            if (!string.IsNullOrWhiteSpace(Settings.CustomSamplingRules))
            {
                // User has opted in, ensure rate limiter is used
                RuleBasedSampler.OptInTracingWithoutLimits();

                foreach (var rule in CustomSamplingRule.BuildFromConfigurationString(Settings.CustomSamplingRules))
                {
                    Sampler.RegisterRule(rule);
                }
            }

            if (Settings.GlobalSamplingRate != null)
            {
                var globalRate = (float)Settings.GlobalSamplingRate;

                if (globalRate < 0f || globalRate > 1f)
                {
                    Log.Warning("{0} configuration of {1} is out of range", ConfigurationKeys.GlobalSamplingRate, Settings.GlobalSamplingRate);
                }
                else
                {
                    Sampler.RegisterRule(new GlobalSamplingRule(globalRate));
                }
            }

            // Register callbacks to make sure we flush the traces before exiting
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Console.CancelKeyPress += Console_CancelKeyPress;

            // start the heartbeat loop
            _heartbeatTimer = new Timer(HeartbeatCallback, state: null, dueTime: TimeSpan.Zero, period: TimeSpan.FromMinutes(1));

            // If configured, add/remove the correlation identifiers into the
            // LibLog logging context when a scope is activated/closed
            if (Settings.LogsInjectionEnabled)
            {
                InitializeLibLogScopeEventSubscriber(_scopeManager, DefaultServiceName, Settings.ServiceVersion, Settings.Environment);
            }

            if (Interlocked.Exchange(ref _firstInitialization, 0) == 1 && Settings.StartupDiagnosticLogEnabled)
            {
                _ = WriteDiagnosticLog();
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Tracer"/> class.
        /// </summary>
        ~Tracer()
        {
            // update the count of Tracer instances
            Interlocked.Decrement(ref _liveTracerCount);
        }

        /// <summary>
        /// Gets or sets the global tracer object
        /// </summary>
        public static Tracer Instance { get; set; }

        /// <summary>
        /// Gets the active scope
        /// </summary>
        public Scope ActiveScope => _scopeManager.Active;

        /// <summary>
        /// Gets the default service name for traces where a service name is not specified.
        /// </summary>
        public string DefaultServiceName { get; }

        /// <summary>
        /// Gets this tracer's settings.
        /// </summary>
        public TracerSettings Settings { get; }

        /// <summary>
        /// Gets the tracer's scope manager, which determines which span is currently active, if any.
        /// </summary>
        IScopeManager IDatadogTracer.ScopeManager => _scopeManager;

        /// <summary>
        /// Gets the <see cref="ISampler"/> instance used by this <see cref="IDatadogTracer"/> instance.
        /// </summary>
        ISampler IDatadogTracer.Sampler => Sampler;

        internal IDiagnosticManager DiagnosticManager { get; set; }

        internal ISampler Sampler { get; }

        internal IStatsd Statsd { get; private set; }

        /// <summary>
        /// Create a new Tracer with the given parameters
        /// </summary>
        /// <param name="agentEndpoint">The agent endpoint where the traces will be sent (default is http://localhost:8126).</param>
        /// <param name="defaultServiceName">Default name of the service (default is the name of the executing assembly).</param>
        /// <param name="isDebugEnabled">Turns on all debug logging (this may have an impact on application performance).</param>
        /// <returns>The newly created tracer</returns>
        public static Tracer Create(Uri agentEndpoint = null, string defaultServiceName = null, bool isDebugEnabled = false)
        {
            // Keep supporting this older public method by creating a TracerConfiguration
            // from default sources, overwriting the specified settings, and passing that to the constructor.
            var configuration = TracerSettings.FromDefaultSources();
            GlobalSettings.SetDebugEnabled(isDebugEnabled);

            if (agentEndpoint != null)
            {
                configuration.AgentUri = agentEndpoint;
            }

            if (defaultServiceName != null)
            {
                configuration.ServiceName = defaultServiceName;
            }

            return new Tracer(configuration);
        }

        /// <summary>
        /// Make a span the active span and return its new scope.
        /// </summary>
        /// <param name="span">The span to activate.</param>
        /// <returns>A Scope object wrapping this span.</returns>
        Scope IDatadogTracer.ActivateSpan(Span span)
        {
            return ActivateSpan(span);
        }

        /// <summary>
        /// Make a span the active span and return its new scope.
        /// </summary>
        /// <param name="span">The span to activate.</param>
        /// <param name="finishOnClose">Determines whether closing the returned scope will also finish the span.</param>
        /// <returns>A Scope object wrapping this span.</returns>
        public Scope ActivateSpan(Span span, bool finishOnClose = true)
        {
            return _scopeManager.Activate(span, finishOnClose);
        }

        /// <summary>
        /// This is a shortcut for <see cref="StartSpan(string, ISpanContext, string, DateTimeOffset?, bool)"/>
        /// and <see cref="ActivateSpan(Span, bool)"/>, it creates a new span with the given parameters and makes it active.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <param name="parent">The span's parent</param>
        /// <param name="serviceName">The span's service name</param>
        /// <param name="startTime">An explicit start time for that span</param>
        /// <param name="ignoreActiveScope">If set the span will not be a child of the currently active span</param>
        /// <param name="finishOnClose">If set to false, closing the returned scope will not close the enclosed span </param>
        /// <returns>A scope wrapping the newly created span</returns>
        public Scope StartActive(string operationName, ISpanContext parent = null, string serviceName = null, DateTimeOffset? startTime = null, bool ignoreActiveScope = false, bool finishOnClose = true)
        {
            var span = StartSpan(operationName, parent, serviceName, startTime, ignoreActiveScope);
            return _scopeManager.Activate(span, finishOnClose);
        }

        /// <summary>
        /// Creates a new <see cref="Span"/> with the specified parameters.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <returns>The newly created span</returns>
        Span IDatadogTracer.StartSpan(string operationName)
        {
            return StartSpan(operationName);
        }

        /// <summary>
        /// Creates a new <see cref="Span"/> with the specified parameters.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <param name="parent">The span's parent</param>
        /// <returns>The newly created span</returns>
        Span IDatadogTracer.StartSpan(string operationName, ISpanContext parent)
        {
            return StartSpan(operationName, parent);
        }

        /// <summary>
        /// Creates a new <see cref="Span"/> with the specified parameters.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <param name="parent">The span's parent</param>
        /// <param name="serviceName">The span's service name</param>
        /// <param name="startTime">An explicit start time for that span</param>
        /// <param name="ignoreActiveScope">If set the span will not be a child of the currently active span</param>
        /// <returns>The newly created span</returns>
        public Span StartSpan(string operationName, ISpanContext parent = null, string serviceName = null, DateTimeOffset? startTime = null, bool ignoreActiveScope = false)
        {
            if (parent == null && !ignoreActiveScope)
            {
                parent = _scopeManager.Active?.Span?.Context;
            }

            ITraceContext traceContext;

            // try to get the trace context (from local spans) or
            // sampling priority (from propagated spans),
            // otherwise start a new trace context
            if (parent is SpanContext parentSpanContext)
            {
                traceContext = parentSpanContext.TraceContext ??
                               new TraceContext(this)
                               {
                                   SamplingPriority = parentSpanContext.SamplingPriority
                               };
            }
            else
            {
                traceContext = new TraceContext(this);
            }

            var finalServiceName = serviceName ?? parent?.ServiceName ?? DefaultServiceName;
            var spanContext = new SpanContext(parent, traceContext, finalServiceName);

            var span = new Span(spanContext, startTime)
            {
                OperationName = operationName,
            };

            // Apply any global tags
            if (Settings.GlobalTags.Count > 0)
            {
                foreach (var entry in Settings.GlobalTags)
                {
                    span.SetTag(entry.Key, entry.Value);
                }
            }

            // automatically add the "env" tag if defined, taking precedence over an "env" tag set from a global tag
            var env = Settings.Environment;
            if (!string.IsNullOrWhiteSpace(env))
            {
                span.SetTag(Tags.Env, env);
            }

            // automatically add the "version" tag if defined, taking precedence over an "version" tag set from a global tag
            var version = Settings.ServiceVersion;
            if (!string.IsNullOrWhiteSpace(version) && string.Equals(finalServiceName, DefaultServiceName))
            {
                span.SetTag(Tags.Version, version);
            }

            traceContext.AddSpan(span);
            return span;
        }

        /// <summary>
        /// Writes the specified <see cref="Span"/> collection to the agent writer.
        /// </summary>
        /// <param name="trace">The <see cref="Span"/> collection to write.</param>
        void IDatadogTracer.Write(Span[] trace)
        {
            if (Settings.TraceEnabled)
            {
                _agentWriter.WriteTrace(trace);
            }
        }

        internal Task FlushAsync()
        {
            return _agentWriter.FlushTracesAsync();
        }

        internal void StartDiagnosticObservers()
        {
            // instead of adding a hard dependency on DiagnosticSource,
            // check if it is available before trying to use it
            var type = Type.GetType("System.Diagnostics.DiagnosticSource, System.Diagnostics.DiagnosticSource", throwOnError: false);

            if (type == null)
            {
                Log.Warning("DiagnosticSource type could not be loaded. Disabling diagnostic observers.");
            }
            else
            {
                // don't call this method unless the necessary types are available
                StartDiagnosticObserversInternal();
            }
        }

        internal void StartDiagnosticObserversInternal()
        {
            DiagnosticManager?.Stop();

            var observers = new List<DiagnosticObserver>();

#if NETSTANDARD
            if (Settings.IsIntegrationEnabled(AspNetCoreDiagnosticObserver.IntegrationName))
            {
                Log.Debug("Adding AspNetCoreDiagnosticObserver");

                var aspNetCoreDiagnosticOptions = new AspNetCoreDiagnosticOptions();
                observers.Add(new AspNetCoreDiagnosticObserver(this, aspNetCoreDiagnosticOptions));
            }
#endif

            if (observers.Count == 0)
            {
                Log.Debug("DiagnosticManager not started, zero observers added.");
            }
            else
            {
                Log.Debug("Starting DiagnosticManager with {0} observers.", observers.Count);

                var diagnosticManager = new DiagnosticManager(observers);
                diagnosticManager.Start();
                DiagnosticManager = diagnosticManager;
            }
        }

        internal async Task WriteDiagnosticLog()
        {
            string agentError = null;

            try
            {
                var success = await _agentWriter.Ping().ConfigureAwait(false);

                if (!success)
                {
                    agentError = "An error occurred while sending traces to the agent";
                }
            }
            catch (Exception ex)
            {
                agentError = ex.Message;
            }

            try
            {
                var frameworkDescription = FrameworkDescription.Create();

                var stringWriter = new StringWriter();

                using (var writer = new JsonTextWriter(stringWriter))
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("date");
                    writer.WriteValue(DateTime.Now);

                    writer.WritePropertyName("os_name");
                    writer.WriteValue(frameworkDescription.OSPlatform);

                    writer.WritePropertyName("os_version");
                    writer.WriteValue(Environment.OSVersion.ToString());

                    writer.WritePropertyName("version");
                    writer.WriteValue(typeof(Tracer).Assembly.GetName().Version.ToString());

                    writer.WritePropertyName("platform");
                    writer.WriteValue(frameworkDescription.ProcessArchitecture);

                    writer.WritePropertyName("lang");
                    writer.WriteValue(frameworkDescription.Name);

                    writer.WritePropertyName("lang_version");
                    writer.WriteValue(frameworkDescription.ProductVersion);

                    writer.WritePropertyName("env");
                    writer.WriteValue(Settings.Environment);

                    writer.WritePropertyName("enabled");
                    writer.WriteValue(Settings.TraceEnabled);

                    writer.WritePropertyName("service");
                    writer.WriteValue(DefaultServiceName);

                    writer.WritePropertyName("agent_url");
                    writer.WriteValue(Settings.AgentUri);

                    writer.WritePropertyName("debug");
                    writer.WriteValue(GlobalSettings.Source.DebugEnabled);

                    writer.WritePropertyName("analytics_enabled");
                    writer.WriteValue(Settings.AnalyticsEnabled);

                    writer.WritePropertyName("sample_rate");
                    writer.WriteValue(Settings.GlobalSamplingRate);

                    writer.WritePropertyName("sampling_rules");
                    writer.WriteValue(Settings.CustomSamplingRules);

                    writer.WritePropertyName("tags");

                    writer.WriteStartArray();

                    foreach (var entry in Settings.GlobalTags)
                    {
                        writer.WriteValue(string.Concat(entry.Key, ":", entry.Value));
                    }

                    writer.WriteEndArray();

                    writer.WritePropertyName("log_injection_enabled");
                    writer.WriteValue(Settings.LogsInjectionEnabled);

                    writer.WritePropertyName("runtime_metrics_enabled");
                    writer.WriteValue(Settings.TracerMetricsEnabled);

                    writer.WritePropertyName("disabled_integrations");
                    writer.WriteStartArray();

                    foreach (var integration in Settings.DisabledIntegrationNames)
                    {
                        writer.WriteValue(integration);
                    }

                    writer.WriteEndArray();

                    writer.WritePropertyName("netstandard_enabled");
                    writer.WriteValue(Settings.IsNetStandardFeatureFlagEnabled());

                    writer.WritePropertyName("agent_reachable");
                    writer.WriteValue(agentError == null);

                    writer.WritePropertyName("agent_error");
                    writer.WriteValue(agentError ?? string.Empty);

                    writer.WriteEndObject();
                }

                Log.Information("DATADOG TRACER CONFIGURATION - {0}", stringWriter.ToString());
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "DATADOG TRACER DIAGNOSTICS - Error fetching configuration");
            }
        }

        /// <summary>
        /// Gets an "application name" for the executing application by looking at
        /// the hosted app name (.NET Framework on IIS only), assembly name, and process name.
        /// </summary>
        /// <returns>The default service name.</returns>
        private static string GetApplicationName()
        {
            try
            {
#if !NETSTANDARD2_0
                // System.Web.dll is only available on .NET Framework
                if (System.Web.Hosting.HostingEnvironment.IsHosted)
                {
                    // if this app is an ASP.NET application, return "SiteName/ApplicationVirtualPath".
                    // note that ApplicationVirtualPath includes a leading slash.
                    return (System.Web.Hosting.HostingEnvironment.SiteName + System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath).TrimEnd('/');
                }
#endif

                return Assembly.GetEntryAssembly()?.GetName().Name ??
                       Process.GetCurrentProcess().ProcessName;
            }
            catch (Exception ex)
            {
                Log.SafeLogError(ex, "Error creating default service name.");
                return null;
            }
        }

        private static IStatsd CreateDogStatsdClient(TracerSettings settings, string serviceName, int port)
        {
            try
            {
                var frameworkDescription = FrameworkDescription.Create();

                var constantTags = new List<string>
                                   {
                                       "lang:.NET",
                                       $"lang_interpreter:{frameworkDescription.Name}",
                                       $"lang_version:{frameworkDescription.ProductVersion}",
                                       $"tracer_version:{TracerConstants.AssemblyVersion}",
                                       $"service:{serviceName}"
                                   };

                if (settings.Environment != null)
                {
                    constantTags.Add($"env:{settings.Environment}");
                }

                if (settings.ServiceVersion != null)
                {
                    constantTags.Add($"version:{settings.ServiceVersion}");
                }

                var statsdUdp = new StatsdUDP(settings.AgentUri.DnsSafeHost, port, StatsdConfig.DefaultStatsdMaxUDPPacketSize);
                return new Statsd(statsdUdp, new RandomGenerator(), new StopWatchFactory(), prefix: string.Empty, constantTags.ToArray());
            }
            catch (Exception ex)
            {
                Log.SafeLogError(ex, $"Unable to instantiate {nameof(Statsd)} client.");
                return new NoOpStatsd();
            }
        }

        private void InitializeLibLogScopeEventSubscriber(IScopeManager scopeManager, string defaultServiceName, string version, string env)
        {
            new LibLogScopeEventSubscriber(scopeManager, defaultServiceName, version ?? string.Empty, env ?? string.Empty);
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            RunShutdownTasks();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            RunShutdownTasks();
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            RunShutdownTasks();
        }

        private void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            RunShutdownTasks();
        }

        private void RunShutdownTasks()
        {
            try
            {
                _agentWriter.FlushAndCloseAsync().Wait();
            }
            catch (Exception ex)
            {
                Log.SafeLogError(ex, "Error flushing traces on shutdown.");
            }

            try
            {
                TracingProcessManager.StopProcesses();
            }
            catch (Exception ex)
            {
                Log.SafeLogError(ex, "Error stopping sub processes on shutdown.");
            }
        }

        private void HeartbeatCallback(object state)
        {
            if (Statsd != null)
            {
                // use the count of Tracer instances as the heartbeat value
                // to estimate the number of "live" Tracers than can potentially
                // send traces to the Agent
                Statsd.AppendSetGauge(TracerMetricNames.Health.Heartbeat, _liveTracerCount);
                Statsd.Send();
            }
        }
    }
}
