// <copyright file="InstrumentationLifespanManager.cs" company="OpenTelemetry Authors">
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

#if NETCOREAPP3_1_OR_GREATER
using System;
using System.Collections.Concurrent;

namespace OpenTelemetry.AutoInstrumentation.Loading
{
    internal class InstrumentationLifespanManager : ILifespanManager, IDisposable
    {
        // some instrumentations requires to keep references to objects
        // so that they are not garbage collected
        private readonly ConcurrentBag<object> _instrumentations = new();

        public void Track(object instance)
        {
            _instrumentations.Add(instance);
        }

        public void Dispose()
        {
            while (_instrumentations.TryTake(out var instrumentation))
            {
                if (instrumentation is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
#endif
