// <copyright file="CallTargetState.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget;

/// <summary>
/// Call target execution state
/// </summary>
public readonly struct CallTargetState
{
    private readonly Activity? _previousActivity;
    private readonly Activity? _activity;
    private readonly object? _state;
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
    public CallTargetState(Activity? activity, object? state)
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
    public CallTargetState(Activity? activity, object? state, DateTimeOffset? startTime)
    {
        _previousActivity = null;
        _activity = activity;
        _state = state;
        _startTime = startTime;
    }

    internal CallTargetState(Activity? previousActivity, CallTargetState state)
    {
        _previousActivity = previousActivity;
        _activity = state._activity;
        _state = state._state;
        _startTime = state.StartTime;
    }

    /// <summary>
    /// Gets the CallTarget BeginMethod activity
    /// </summary>
    public Activity? Activity => _activity;

    /// <summary>
    /// Gets the CallTarget BeginMethod state
    /// </summary>
    public object? State => _state;

    /// <summary>
    /// Gets the start time.
    /// </summary>
    public DateTimeOffset? StartTime => _startTime;

    internal Activity? PreviousActivity => _previousActivity;

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
