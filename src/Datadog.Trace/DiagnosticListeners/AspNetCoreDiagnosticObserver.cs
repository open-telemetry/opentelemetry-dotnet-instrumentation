#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Abstractions;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Headers;
using Datadog.Trace.Logging;
using Datadog.Trace.Tagging;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.Serilog.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Datadog.Trace.DiagnosticListeners
{
    /// <summary>
    /// Instruments ASP.NET Core.
    /// <para/>
    /// Unfortunately, ASP.NET Core only uses one <see cref="System.Diagnostics.DiagnosticListener"/> instance
    /// for everything so we also only create one observer to ensure best performance.
    /// <para/>
    /// Hosting events: https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/Internal/HostingApplicationDiagnostics.cs
    /// </summary>
    internal sealed class AspNetCoreDiagnosticObserver : DiagnosticObserver
    {
        public const string IntegrationName = "AspNetCore";

        private const string DiagnosticListenerName = "Microsoft.AspNetCore";
        private const string ComponentName = "aspnet_core";
        private const string HttpRequestInOperationName = "aspnet_core.request";
        private const string NoHostSpecified = "UNKNOWN_HOST";

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.For<AspNetCoreDiagnosticObserver>();

        private static readonly PropertyFetcher HttpRequestInStartHttpContextFetcher = new PropertyFetcher("HttpContext");
        private static readonly PropertyFetcher HttpRequestInStopHttpContextFetcher = new PropertyFetcher("HttpContext");
        private static readonly PropertyFetcher UnhandledExceptionHttpContextFetcher = new PropertyFetcher("HttpContext");
        private static readonly PropertyFetcher UnhandledExceptionExceptionFetcher = new PropertyFetcher("Exception");
        private static readonly PropertyFetcher BeforeActionHttpContextFetcher = new PropertyFetcher("httpContext");
        private static readonly PropertyFetcher BeforeActionActionDescriptorFetcher = new PropertyFetcher("actionDescriptor");

        private readonly Tracer _tracer;
        private readonly bool _isLogLevelDebugEnabled = Log.IsEnabled(LogEventLevel.Debug);

        public AspNetCoreDiagnosticObserver()
            : this(null)
        {
        }

        public AspNetCoreDiagnosticObserver(Tracer tracer)
        {
            _tracer = tracer;
        }

        protected override string ListenerName => DiagnosticListenerName;

        protected override void OnNext(string eventName, object arg)
        {
            switch (eventName)
            {
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                    OnHostingHttpRequestInStart(arg);
                    break;

                case "Microsoft.AspNetCore.Mvc.BeforeAction":
                    OnMvcBeforeAction(arg);
                    break;

                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                    OnHostingHttpRequestInStop(arg);
                    break;

                case "Microsoft.AspNetCore.Hosting.UnhandledException":
                case "Microsoft.AspNetCore.Diagnostics.UnhandledException":
                    OnHostingUnhandledException(arg);
                    break;
            }
        }

        private static string GetUrl(HttpRequest request)
        {
            if (request.Host.HasValue)
            {
                return $"{request.Scheme}://{request.Host.Value}{request.PathBase.Value}{request.Path.Value}";
            }

            // HTTP 1.0 requests are not required to provide a Host to be valid
            // Since this is just for display, we can provide a string that is
            // not an actual Uri with only the fields that are specified.
            // request.GetDisplayUrl(), used above, will throw an exception
            // if request.Host is null.
            return $"{request.Scheme}://{NoHostSpecified}{request.PathBase.Value}{request.Path.Value}";
        }

        private static SpanContext ExtractPropagatedContext(HttpRequest request)
        {
            try
            {
                // extract propagation details from http headers
                var requestHeaders = request.Headers;

                if (requestHeaders != null)
                {
                    return SpanContextPropagator.Instance.Extract(new HeadersCollectionAdapter(requestHeaders));
                }
            }
            catch (Exception ex)
            {
                Log.SafeLogError(ex, "Error extracting propagated HTTP headers.");
            }

            return null;
        }

        private static IEnumerable<KeyValuePair<string, string>> ExtractHeaderTags(HttpRequest request, IDatadogTracer tracer)
        {
            var settings = tracer.Settings;

            if (!settings.HeaderTags.IsEmpty())
            {
                try
                {
                    // extract propagation details from http headers
                    var requestHeaders = request.Headers;

                    if (requestHeaders != null)
                    {
                        return SpanContextPropagator.Instance.ExtractHeaderTags(new HeadersCollectionAdapter(requestHeaders), settings.HeaderTags);
                    }
                }
                catch (Exception ex)
                {
                    Log.SafeLogError(ex, "Error extracting propagated HTTP headers.");
                }
            }

            return Enumerable.Empty<KeyValuePair<string, string>>();
        }

        private void OnHostingHttpRequestInStart(object arg)
        {
            var tracer = _tracer ?? Tracer.Instance;

            if (!tracer.Settings.IsIntegrationEnabled(IntegrationName))
            {
                return;
            }

            var httpContext = HttpRequestInStartHttpContextFetcher.Fetch<HttpContext>(arg);
            HttpRequest request = httpContext.Request;
            string host = request.Host.Value;
            string httpMethod = request.Method?.ToUpperInvariant() ?? "UNKNOWN";
            string url = GetUrl(request);

            string absolutePath = request.Path.Value;

            if (request.PathBase.HasValue)
            {
                absolutePath = request.PathBase.Value + absolutePath;
            }

            string resourceUrl = UriHelpers.GetRelativeUrl(absolutePath, tryRemoveIds: true)
                                           .ToLowerInvariant();

            string resourceName = $"{httpMethod} {resourceUrl}";

            SpanContext propagatedContext = ExtractPropagatedContext(request);
            var tagsFromHeaders = ExtractHeaderTags(request, tracer);

            var tags = new AspNetCoreTags();
            var scope = tracer.StartActiveWithTags(HttpRequestInOperationName, propagatedContext, tags: tags);

            tags.InstrumentationName = ComponentName;

            scope.Span.DecorateWebServerSpan(resourceName, httpMethod, host, url, tags, tagsFromHeaders);

            // set analytics sample rate if enabled
            var analyticsSampleRate = tracer.Settings.GetIntegrationAnalyticsSampleRate(IntegrationName, enabledWithGlobalSetting: true);

            if (analyticsSampleRate != null)
            {
                tags.AnalyticsSampleRate = analyticsSampleRate;
            }
        }

        private void OnMvcBeforeAction(object arg)
        {
            var tracer = _tracer ?? Tracer.Instance;

            if (!tracer.Settings.IsIntegrationEnabled(IntegrationName))
            {
                return;
            }

            var httpContext = BeforeActionHttpContextFetcher.Fetch<HttpContext>(arg);

            Span span = tracer.ActiveScope?.Span;

            if (span != null)
            {
                // NOTE: This event is the start of the action pipeline. The action has been selected, the route
                //       has been selected but no filters have run and model binding hasn't occurred.
                var actionDescriptor = BeforeActionActionDescriptorFetcher.Fetch<ActionDescriptor>(arg);
                HttpRequest request = httpContext.Request;

                string httpMethod = request.Method?.ToUpperInvariant() ?? "UNKNOWN";
                string controllerName = actionDescriptor.RouteValues["controller"];
                string actionName = actionDescriptor.RouteValues["action"];
                string routeTemplate = actionDescriptor.AttributeRouteInfo?.Template ?? $"{controllerName}/{actionName}";
                string resourceName = $"{httpMethod} {routeTemplate}";

                // override the parent's resource name with the MVC route template
                span.ResourceName = resourceName;
            }
        }

        private void OnHostingHttpRequestInStop(object arg)
        {
            var tracer = _tracer ?? Tracer.Instance;

            if (!tracer.Settings.IsIntegrationEnabled(IntegrationName))
            {
                return;
            }

            IScope scope = tracer.ActiveScope;

            if (scope != null)
            {
                var httpContext = HttpRequestInStopHttpContextFetcher.Fetch<HttpContext>(arg);

                var statusCode = HttpTags.ConvertStatusCodeToString(httpContext.Response.StatusCode);

                scope.Span.SetTag(Tags.HttpStatusCode, statusCode);

                if (httpContext.Response.StatusCode / 100 == 5)
                {
                    // 5xx codes are server-side errors
                    scope.Span.Error = true;
                }

                scope.Dispose();
            }
        }

        private void OnHostingUnhandledException(object arg)
        {
            var tracer = _tracer ?? Tracer.Instance;

            if (!tracer.Settings.IsIntegrationEnabled(IntegrationName))
            {
                return;
            }

            ISpan span = tracer.ActiveScope?.Span;

            if (span != null)
            {
                var exception = UnhandledExceptionExceptionFetcher.Fetch<Exception>(arg);
                var httpContext = UnhandledExceptionHttpContextFetcher.Fetch<HttpContext>(arg);

                span.SetException(exception);
            }
        }

        private readonly struct HeadersCollectionAdapter : IHeadersCollection
        {
            private readonly IHeaderDictionary _headers;

            public HeadersCollectionAdapter(IHeaderDictionary headers)
            {
                _headers = headers;
            }

            public IEnumerable<string> GetValues(string name)
            {
                if (_headers.TryGetValue(name, out var values))
                {
                    return values.ToArray();
                }

                return Enumerable.Empty<string>();
            }

            public void Set(string name, string value)
            {
                throw new NotImplementedException();
            }

            public void Add(string name, string value)
            {
                throw new NotImplementedException();
            }

            public void Remove(string name)
            {
                throw new NotImplementedException();
            }
        }
    }
}
#endif
