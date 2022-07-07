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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.AutoInstrumentation.Loading;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Loading
{
    public class LazyInstrumentationLoaderTests
    {
        [Fact]
        public void LazyInstrumentationLoader_DefaultLoad()
        {
            var detectors = new Dictionary<TracerInstrumentation, Func<AssemblyLoadDetector>>()
            {
                { default(TracerInstrumentation), () => new DummyDetector() }
            };

            using var loader = CreateLoader(new NameValueCollection(), detectors);

            CreateDummyAssembly();

            var instrumentation = loader.Instrumentations.FirstOrDefault();

            Assert.True(instrumentation is DummyInstrumentation);
        }

        [Fact]
        public void LazyInstrumentationLoader_InstrumentationNotEnabled()
        {
            var detectors = new Dictionary<TracerInstrumentation, Func<AssemblyLoadDetector>>()
            {
                // Random instrumentation here that doesn't match with configuration
                // below to simulate non enabled instrumentation
                { TracerInstrumentation.HttpClient, () => new DummyDetector() }
            };

            var config = new NameValueCollection()
            {
                // Random enabled instrumentation configuration that doesn't match with the Detector.
                { ConfigurationKeys.Traces.Instrumentations, nameof(TracerInstrumentation.AspNet) }
            };

            using var loader = CreateLoader(config, detectors);

            CreateDummyAssembly();

            var instrumentation = loader.Instrumentations.FirstOrDefault();

            Assert.Null(instrumentation);
        }

        [Fact]
        public void LazyInstrumentationLoader_Disposing()
        {
            var detectors = new Dictionary<TracerInstrumentation, Func<AssemblyLoadDetector>>()
            {
                { default(TracerInstrumentation), () => new DummyDetector() }
            };

            var loader = CreateLoader(new NameValueCollection(), detectors);

            CreateDummyAssembly();

            var instrumentation = loader.Instrumentations.FirstOrDefault();

            loader.Dispose();

            Assert.True(instrumentation is DummyInstrumentation);
            Assert.True((instrumentation as DummyInstrumentation).IsDisposed);
        }

        private LazyInstrumentationLoader CreateLoader(NameValueCollection settingsConfig, Dictionary<TracerInstrumentation, Func<AssemblyLoadDetector>> detectors)
        {
            var settings = new TracerSettings(new NameValueConfigurationSource(settingsConfig));
            var builders = new LazyInstrumentationBuilders(settings, detectors);
            var loader = new LazyInstrumentationLoader(builders);

            return loader;
        }

        private void CreateDummyAssembly()
        {
            // Creates and loads assembly dynamically.
            // This should trigger also assembly load event.

            AssemblyName assemblyName = new AssemblyName(DummyDetector.DummyAssemblyName);
            AssemblyBuilder ab =
                AssemblyBuilder.DefineDynamicAssembly(
                    assemblyName,
                    AssemblyBuilderAccess.Run);

            ModuleBuilder mb = ab.DefineDynamicModule(assemblyName.Name);
        }

        private class DummyDetector : AssemblyLoadDetector
        {
            public const string DummyAssemblyName = "Dummy.Assembly";

            private static readonly string[] _requiredAssemblies = new[] { DummyAssemblyName };

            public DummyDetector()
                : base(_requiredAssemblies)
            {
            }

            internal override Func<object> GetInstrumentationBuilder()
            {
                return () => new DummyInstrumentation();
            }
        }

        private class DummyInstrumentation : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}
