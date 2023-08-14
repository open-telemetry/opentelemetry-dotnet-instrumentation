// <copyright file="LazyInstrumentationLoaderTests.cs" company="OpenTelemetry Authors">
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

using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using FluentAssertions.Execution;
using OpenTelemetry.AutoInstrumentation.Loading;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Loading;

public class LazyInstrumentationLoaderTests
{
    [Fact]
    public void InitializesOnAssemblyLoad()
    {
        var initializer1 = new DummyInitializer();
        var initializer2 = new DummyInitializer();
        using (var loader = new LazyInstrumentationLoader())
        {
            loader.Add(initializer1); // Before loading the assembly

            CreateDummyAssembly(); // Creates and loads assembly dynamically. This should trigger also assembly load event.

            loader.Add(initializer2); // After loading the assembly
        }

        using (new AssertionScope())
        {
            initializer1.Initialized.Should().BeTrue();
            initializer1.Disposed.Should().BeTrue();

            initializer2.Initialized.Should().BeTrue();
            initializer2.Disposed.Should().BeTrue();
        }
    }

    private static void CreateDummyAssembly()
    {
        var assemblyName = new AssemblyName(DummyInitializer.DummyAssemblyName);
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
    }

    private class DummyInitializer : InstrumentationInitializer, IDisposable
    {
        public const string DummyAssemblyName = "Dummy.Assembly";

        public DummyInitializer()
            : base(DummyAssemblyName)
        {
        }

        public bool Initialized { get; private set; }

        public bool Disposed { get; private set; }

        public override void Initialize(ILifespanManager lifespanManager)
        {
            Initialized = true;
            lifespanManager.Track(this);
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
