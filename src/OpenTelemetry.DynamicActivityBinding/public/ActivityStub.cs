using System;
using System.Collections.Generic;

namespace OpenTelemetry.DynamicActivityBinding
{
    /// <summary>
    /// This is the type used by auto-instrumentation code to generate trace data. It is based
    /// on the <c>System.Diagnostics.Activity</c> type. The latter can't be used directly by the
    /// instrumentation code since it isn't feasible to determine prior runtime if it is going
    /// to be possible to load it and if possible the exact version that is going to be loaded.
    /// </summary>
    public struct ActivityStub : IDisposable
    {
        // Temporary constant to be used with NotImplementedException.
        // TODO: remove it when functionatily is implemented.
        private const string MissingCapability = "Currently ActivityStub only supports no-op";

        private ActivityStub(object? activityInstance)
        {
            TraceId = null;
            SpanId = null;
            ParentSpanId = null;
            ForceDefaultIdFormat = false;

            ActivityInstance = activityInstance;
        }

        /// <summary>
        /// Gets the current ActivityStub for the current thread. This flows across async calls.
        /// </summary>
        /// <remarks>
        /// Unlike <c>System.Diagnostics.Activity</c> ActivityStub is a struct so <c>null</c> can't be
        /// used to indicate "no current activity".
        /// <para/>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.Current</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public static ActivityStub Current
        {
            get
            {
                if (!DynamicLoader.EnsureInitialized())
                {
                    // TODO: is the no-op stub enough to represent "no current activity"? no need to differentiate
                    // "no current activity" from "no-op activity"?
                    return NoOpSingletons.ActivityStub;
                }

                throw new NotImplementedException(MissingCapability);
            }
        }

        /// <summary>
        /// Gets the default ID format for the Activity.
        /// </summary>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.DefaultIdFormat</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.4.0
        /// </remarks>
        public static ActivityIdFormatStub DefaultIdFormat
        {
            get
            {
                if (!DynamicLoader.EnsureInitialized())
                {
                    return ActivityIdFormatStub.Unknown;
                }

                throw new NotImplementedException(MissingCapability);
            }
        }

        /// <summary>
        /// Gets the actual object instance backing the ActivityStub.
        /// </summary>
        /// <remarks>
        /// This property is exclusive of <see cref="ActivityStub"/> as a way to provide
        /// access to the underlying providing the actual functionality.
        /// </remarks>
        public object? ActivityInstance { get; private set; }

        /// <summary>
        /// Gets a collection of key/value pairs that represents information that is passed to children of this Activity.
        /// </summary>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.Baggage</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public IEnumerable<KeyValuePair<string, string?>> Baggage
        {
            get
            {
                if (ActivityInstance == null)
                {
                    return NoOpSingletons.KvpEnumerable;
                }

                throw new NotImplementedException(MissingCapability);
            }
        }

        /// <summary>
        /// Gets the duration of the operation.
        /// </summary>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.Duration</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public TimeSpan Duration
        {
            get
            {
                if (ActivityInstance == null)
                {
                    return NoOpSingletons.TimeSpan;
                }

                throw new NotImplementedException(MissingCapability);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="DefaultIdFormat"/> is always used to define the default ID format.
        /// </summary>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.ForceDefaultIdFormat</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.4.0
        /// </remarks>
        public bool ForceDefaultIdFormat
        {
            // TODO: actual implementation
            get; set;
        }

        /// <summary>
        /// Gets an identifier that is specific to a particular request.
        /// </summary>
        /// <remarks>
        /// This is an ID that is specific to a particular request.   Filtering
        /// to a particular ID insures that you get only one request that matches.
        /// Id has a hierarchical structure: '|root-id.id1_id2.id3_' Id is generated when
        /// <see cref="Start"/> is called by appending suffix to Parent.Id
        /// or ParentId; Activity has no Id until it started
        /// <para/>
        /// See <see href="https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md#id-format"/>
        /// for more details.
        /// <para/>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.Id</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public string? Id
        {
            get
            {
                if (ActivityInstance == null)
                {
                    return string.Empty;
                }

                throw new NotImplementedException(MissingCapability);
            }
        }

        /// <summary>
        /// Gets the format for the <see cref="Id"/>.
        /// </summary>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.IdFormat</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.4.0
        /// </remarks>
        public ActivityIdFormatStub IdFormat
        {
            get
            {
                if (ActivityInstance == null)
                {
                    return NoOpSingletons.ActivityIdFormat;
                }

                throw new NotImplementedException(MissingCapability);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ActivityStub is a no-op or not.
        /// </summary>
        public bool IsNoOpStub
        {
            get { return ActivityInstance == null; }
        }

        /// <summary>
        /// Gets the relationship between the activity, its parents, and its children in a trace.
        /// </summary>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.Kind</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=5.0.0.0
        /// </remarks>
        public ActivityKindStub Kind
        {
            get
            {
                if (ActivityInstance == null)
                {
                    return NoOpSingletons.ActivityKind;
                }

                throw new NotImplementedException(MissingCapability);
            }
        }

        /// <summary>
        /// Gets the operation name.
        /// </summary>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.OperationName</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public string? OperationName
        {
            get
            {
                if (ActivityInstance == null)
                {
                    return string.Empty;
                }

                throw new NotImplementedException(MissingCapability);
            }
        }

        /// <summary>
        /// Gets or sets the parent's <see cref="SpanId"/>.
        /// </summary>
        /// <remarks>
        /// If the Activity.ParentId is in the W3C format, this property
        /// returns the <see cref="SpanId"/> part of the ParentId. Otherwise
        /// it returns the zero-value for the property type.
        /// <para/>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.ParentSpanId</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.4.0
        /// </remarks>
        public string? ParentSpanId
        {
            get; set;
        }

        /// <summary>
        /// Gets the root ID of this activity.
        /// </summary>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.RootId</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public string? RootId
        {
            get
            {
                if (ActivityInstance == null)
                {
                    return null;
                }

                throw new NotImplementedException(MissingCapability);
            }
        }

        /// <summary>
        /// Gets or sets the SPAN part of the <see cref="Id"/>.
        /// </summary>
        /// <remarks>
        /// The ID for the SPAN part of <see cref="Id"/>,
        /// if the ID has the W3C format; otherwise, the zero-value for the property type.
        /// <para/>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.SpanId</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.4.0
        /// </remarks>
        public string? SpanId
        {
            get; set;
        }

        /// <summary>
        /// Gets a collection of key/value pairs that represent information that will be logged
        /// along with the activity to the tracing system.
        /// </summary>
        /// <seealso cref="Baggage"/>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.Tags</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public IEnumerable<KeyValuePair<string, string?>> Tags
        {
            get
            {
                if (ActivityInstance == null)
                {
                    return NoOpSingletons.KvpEnumerable;
                }

                throw new NotImplementedException(MissingCapability);
            }
        }

        /// <summary>
        /// Gets or sets the TraceId part of the <see cref="Id"/>.
        /// </summary>
        /// <remarks>
        /// The ID for the TraceId part of the <see cref="Id"/>,
        /// if the ID has the W3C format; otherwise, the zero-value for the property type.
        /// <para/>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.TraceId</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.4.0
        /// </remarks>
        public string? TraceId
        {
            get; set;
        }

        /// <summary>
        /// Creates an ActivityStub wrapping the given object.
        /// </summary>
        /// <param name="activity">
        /// Instance object that will be wrapped by the ActivityStub to
        /// provide its actual functionality.
        /// </param>
        /// <returns>
        /// An ActivityStub wrapping the given object.
        /// </returns>
        public static ActivityStub Wrap(object? activity)
        {
            if (activity == null)
            {
                return NoOpSingletons.ActivityStub;
            }
            else
            {
                return new ActivityStub(activity);
            }
        }

        /// <summary>
        /// Updates the <see cref="ActivityStub"/> to have a new baggage item with the specified key and value.
        /// </summary>
        /// <param name="key">
        /// The baggage key.
        /// </param>
        /// <param name="value">
        /// The baggage value.
        /// </param>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.AddBaggage</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public void AddBaggage(string key, string? value)
        {
            if (ActivityInstance == null)
            {
                return;
            }

            throw new NotImplementedException(MissingCapability);
        }

        /// <summary>
        /// Updates the <see cref="ActivityStub"/> to have a new tag with the provided key and value.
        /// </summary>
        /// <param name="key">
        /// The tag key.
        /// </param>
        /// <param name="value">
        /// The tag value.
        /// </param>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.OperationName</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public void AddTag(string key, string? value)
        {
            if (ActivityInstance == null)
            {
                return;
            }

            throw new NotImplementedException(MissingCapability);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.Dispose</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=5.0.0.0
        /// </remarks>
        public void Dispose()
        {
            IDisposable? disposableActivity = ActivityInstance as IDisposable;
            if (disposableActivity != null)
            {
                disposableActivity.Dispose();
            }
            else
            {
                Stop();
            }
        }

        /// <summary>
        /// Returns the value of the key-value pair added to the activity with <see cref="AddBaggage(string, string)"/>.
        /// Returns null if that key does not exist.
        /// </summary>
        /// <param name="key">
        /// The baggage key.
        /// </param>
        /// <returns>
        /// The value of the key-value pair added to the activity with <see cref="AddBaggage(string, string)"/>.
        /// Returns null if that key does not exist.
        /// </returns>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.OperationName</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public string? GetBaggageItem(string key)
        {
            if (ActivityInstance == null)
            {
                return null;
            }

            throw new NotImplementedException(MissingCapability);
        }

        /// <summary>
        /// GetCustomProperty retrieve previously attached object mapped to the property name.
        /// </summary>
        /// <param name="propertyName">
        /// The name to get the associated object with.
        /// </param>
        /// <returns>
        /// The object mapped to the property name. Or null if there is no mapping previously
        /// done with this property name.
        /// </returns>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.GetCustomProperty</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=5.0.0.0
        /// </remarks>
        public object? GetCustomProperty(string propertyName)
        {
            if (ActivityInstance == null)
            {
                return null;
            }

            throw new NotImplementedException(MissingCapability);
        }

        /// <summary>
        /// Gets the parent activity that created <c>this</c> activity.
        /// </summary>
        /// <param name="hasParent">
        /// Out parameter used to indicate if the activity actually has a parent.
        /// </param>
        /// <returns>
        /// The parent activity or a no-op stub, check <paramref name="hasParent"/> to
        /// determine the actual meaning.
        /// </returns>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.Parent</c>
        /// plus the <paramref name="hasParent"/> to compensate for the fact that <see cref="ActivityStub"/>
        /// is a value type unlike <c>System.Diagnostics.DiagnosticSource.Activity</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public ActivityStub Parent(out bool hasParent)
        {
            // A more appropriate shape for this API would be
            //   bool TryGetParentStub(out ActivityStub parentStub);
            // However, we want Intellisense to place this next to Parent.
            if (ActivityInstance == null)
            {
                hasParent = false;
                return NoOpSingletons.ActivityStub;
            }

            throw new NotImplementedException(MissingCapability);
        }

        /// <summary>
        /// SetCustomProperty allow attaching any custom object to this Activity object.
        /// If the property name was previously associated with other object, SetCustomProperty will update
        /// to use the new propert value instead.
        /// </summary>
        /// <param name="propertyName"> The name to associate the value with.<see cref="OperationName"/></param>
        /// <param name="propertyValue">The object to attach and map to the property name.</param>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.GetCustomProperty</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=5.0.0.0
        /// </remarks>
        public void SetCustomProperty(string propertyName, object? propertyValue)
        {
            if (ActivityInstance == null)
            {
                return;
            }

            throw new NotImplementedException(MissingCapability);
        }

        /// <summary>
        /// Updates the Activity To indicate that the activity with ID <paramref name="parentId"/>
        /// caused this activity.   This is intended to be used only at 'boundary'
        /// scenarios where an activity from another process logically started
        /// this activity. The Parent ID shows up the Tags (as well as the ParentID
        /// property), and can be used to reconstruct the causal tree.
        /// </summary>
        /// <param name="parentId">
        /// The id of the parent operation.
        /// </param>
        /// <returns>
        /// <c>this</c> for convenient chaining.
        /// </returns>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.SetParentId</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public ActivityStub SetParentId(string parentId)
        {
            if (ActivityInstance == null)
            {
                return this;
            }

            throw new NotImplementedException(MissingCapability);
        }

        /// <summary>
        /// Starts the activity.
        /// </summary>
        /// <returns>
        /// <c>this</c> for convenient chaining.
        /// </returns>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.Start</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public ActivityStub Start()
        {
            if (ActivityInstance == null)
            {
                return this;
            }

            throw new NotImplementedException(MissingCapability);
        }

        /// <summary>
        /// Stops the activity.
        /// </summary>
        /// <remarks>
        /// Provides functionality of <c>System.Diagnostics.DiagnosticSource.Activity.Start</c>.
        /// Introduction: System.Diagnostics.DiagnosticSource, Version=4.0.2.1
        /// </remarks>
        public void Stop()
        {
            if (ActivityInstance == null)
            {
                return;
            }

            throw new NotImplementedException(MissingCapability);
        }

        private static class NoOpSingletons
        {
            internal static readonly ActivityStub ActivityStub = new ActivityStub(null);
            internal static readonly IEnumerable<KeyValuePair<string, string?>> KvpEnumerable = new KeyValuePair<string, string?>[0];
            internal static readonly TimeSpan TimeSpan = TimeSpan.Zero;
            internal static readonly DateTime DateTimeUtc = default(DateTime).ToUniversalTime();
            internal static readonly ActivityIdFormatStub ActivityIdFormat = ActivityIdFormatStub.Unknown;
            internal static readonly ActivityKindStub ActivityKind = ActivityKindStub.Internal;
        }
    }
}
