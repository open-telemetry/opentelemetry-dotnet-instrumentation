// <copyright file="NoThrowAwaiter.cs" company="OpenTelemetry Authors">
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

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#pragma warning disable SA1649 // File name must match first type name

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

internal struct NoThrowAwaiter : ICriticalNotifyCompletion
{
    private readonly Task _task;
    private readonly bool _preserveContext;

    public NoThrowAwaiter(Task task, bool preserveContext)
    {
        _task = task;
        _preserveContext = preserveContext;
    }

    public bool IsCompleted => _task.IsCompleted;

    public NoThrowAwaiter GetAwaiter() => this;

    public void GetResult()
    {
    }

    public void OnCompleted(Action continuation) => _task.ConfigureAwait(_preserveContext).GetAwaiter().OnCompleted(continuation);

    public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
}
