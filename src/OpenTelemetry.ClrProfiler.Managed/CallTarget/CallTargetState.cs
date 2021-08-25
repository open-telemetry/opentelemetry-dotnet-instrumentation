using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.ClrProfiler.CallTarget
{
    /// <summary>
    /// Call target execution state
    /// </summary>
    public readonly struct CallTargetState
    {
        private readonly Activity _previousActivity;
        private readonly Activity _activity;
        private readonly object _state;
        private readonly DateTimeOffset? _startTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallTargetState"/> struct.
        /// </summary>
        /// <param name="activity">Activity instance</param>
        public CallTargetState(Activity activity)
        {
            _previousActivity = null;
            _activity = activity;
            _state = null;
            _startTime = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallTargetState"/> struct.
        /// </summary>
        /// <param name="activity">Activity instance</param>
        /// <param name="state">Object state instance</param>
        public CallTargetState(Activity activity, object state)
        {
            _previousActivity = null;
            _activity = activity;
            _state = state;
            _startTime = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallTargetState"/> struct.
        /// </summary>
        /// <param name="activity">Activity instance</param>
        /// <param name="state">Object state instance</param>
        /// <param name="startTime">The intended start time of the activity, intended for activities created in the OnMethodEnd handler</param>
        public CallTargetState(Activity activity, object state, DateTimeOffset? startTime)
        {
            _previousActivity = null;
            _activity = activity;
            _state = state;
            _startTime = startTime;
        }

        internal CallTargetState(Activity previousActivity, CallTargetState state)
        {
            _previousActivity = previousActivity;
            _activity = state._activity;
            _state = state._state;
            _startTime = state._startTime;
        }

        /// <summary>
        /// Gets the CallTarget BeginMethod activity
        /// </summary>
        public Activity Activity => _activity;

        /// <summary>
        /// Gets the CallTarget BeginMethod state
        /// </summary>
        public object State => _state;

        /// <summary>
        /// Gets the CallTarget state StartTime
        /// </summary>
        public DateTimeOffset? StartTime => _startTime;

        internal Activity PreviousActivity => _previousActivity;

        /// <summary>
        /// Gets the default call target state (used by the native side to initialize the locals)
        /// </summary>
        /// <returns>Default call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState GetDefault()
        {
            return default;
        }

        /// <summary>
        /// ToString override
        /// </summary>
        /// <returns>String value</returns>
        public override string ToString()
        {
            return $"{typeof(CallTargetState).FullName}({_previousActivity}, {_activity}, {_state})";
        }
    }
}
