using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace OpenTelemetry.Instrumentation.DiagnosticSourceProxy
{
    internal class DiagnosticSourceSubscriber : IDisposable, IObserver<DiagnosticListener>
    {
        private readonly List<IDisposable> listenerSubscriptions;
        private readonly Func<string, ListenerHandler> handlerFactory;
        private readonly Func<DiagnosticListener, bool> diagnosticSourceFilter;
        private readonly Func<string, object, object, bool> isEnabledFilter;
        private long disposed;
        private IDisposable allSourcesSubscription;

        public DiagnosticSourceSubscriber(
            ListenerHandler handler,
            Func<string, object, object, bool> isEnabledFilter)
            : this(_ => handler, value => handler.SourceName == value.Name, isEnabledFilter)
        {
        }

        public DiagnosticSourceSubscriber(
            Func<string, ListenerHandler> handlerFactory,
            Func<DiagnosticListener, bool> diagnosticSourceFilter,
            Func<string, object, object, bool> isEnabledFilter)
        {
            listenerSubscriptions = new List<IDisposable>();
            this.handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
            this.diagnosticSourceFilter = diagnosticSourceFilter;
            this.isEnabledFilter = isEnabledFilter;
        }

        public void Subscribe()
        {
            if (allSourcesSubscription == null)
            {
                allSourcesSubscription = DiagnosticListener.AllListeners.Subscribe(this);
            }
        }

        public void OnNext(DiagnosticListener value)
        {
            if ((Interlocked.Read(ref disposed) == 0) &&
                diagnosticSourceFilter(value))
            {
                var handler = handlerFactory(value.Name);
                var listener = new DiagnosticSourceListener(handler);
                var subscription = isEnabledFilter == null ?
                    value.Subscribe(listener) :
                    value.Subscribe(listener, isEnabledFilter);

                lock (listenerSubscriptions)
                {
                    listenerSubscriptions.Add(subscription);
                }
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref disposed, 1, 0) == 1)
            {
                return;
            }

            lock (listenerSubscriptions)
            {
                foreach (var listenerSubscription in listenerSubscriptions)
                {
                    listenerSubscription?.Dispose();
                }

                listenerSubscriptions.Clear();
            }

            allSourcesSubscription?.Dispose();
            allSourcesSubscription = null;
        }
    }
}
