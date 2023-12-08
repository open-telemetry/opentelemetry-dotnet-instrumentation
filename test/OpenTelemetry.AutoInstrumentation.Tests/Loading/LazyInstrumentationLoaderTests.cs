// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
