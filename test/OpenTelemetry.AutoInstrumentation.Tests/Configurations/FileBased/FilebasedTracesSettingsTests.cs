// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedTracesSettingsTests
{
    public static TheoryData<SkipConfigurationTestCase> LoadMethod_SkipWrongExporterConfiguration_Data()
    {
        return
        [
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors = []
                }
            }),
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors = [new ProcessorConfig()]
                }
            }),
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors =
                    [
                        new ProcessorConfig
                        {
                            Batch = new BatchProcessorConfig(),
                            Simple = new SimpleProcessorConfig()
                        }
                    ]
                }
            }),
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors =
                    [
                        new ProcessorConfig
                        {
                            Batch = new BatchProcessorConfig
                            {
                                Exporter = new BatchTracerExporterConfig
                                {
                                    OtlpHttp = new OtlpHttpExporterConfig(),
                                    OtlpGrpc = new OtlpGrpcExporterConfig(),
                                }
                            },
                        }
                    ]
                }
            }),
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors =
                    [
                        new ProcessorConfig
                        {
                            Batch = new BatchProcessorConfig
                            {
                                Exporter = new BatchTracerExporterConfig
                                {
                                    OtlpHttp = new OtlpHttpExporterConfig(),
                                    Zipkin = new ZipkinExporterConfig()
                                }
                            },
                        }
                    ]
                }
            }),
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors =
                    [
                        new ProcessorConfig
                        {
                            Batch = new BatchProcessorConfig
                            {
                                Exporter = new BatchTracerExporterConfig
                                {
                                    OtlpGrpc = new OtlpGrpcExporterConfig(),
                                    Zipkin = new ZipkinExporterConfig()
                                }
                            },
                        }
                    ]
                }
            }),
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors =
                    [
                        new ProcessorConfig
                        {
                            Simple = new SimpleProcessorConfig
                            {
                                Exporter = new SimpleTracerExporterConfig
                                {
                                    OtlpHttp = new OtlpHttpExporterConfig(),
                                    OtlpGrpc = new OtlpGrpcExporterConfig()
                                }
                            },
                        }
                    ]
                }
            }),
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors =
                    [
                        new ProcessorConfig
                        {
                            Simple = new SimpleProcessorConfig
                            {
                                Exporter = new SimpleTracerExporterConfig
                                {
                                    OtlpHttp = new OtlpHttpExporterConfig(),
                                    Zipkin = new ZipkinExporterConfig()
                                }
                            },
                        }
                    ]
                }
            }),
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors =
                    [
                        new ProcessorConfig
                        {
                            Simple = new SimpleProcessorConfig
                            {
                                Exporter = new SimpleTracerExporterConfig
                                {
                                    OtlpHttp = new OtlpHttpExporterConfig(),
                                    Console = new object()
                                }
                            },
                        }
                    ]
                }
            }),
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors =
                    [
                        new ProcessorConfig
                        {
                            Simple = new SimpleProcessorConfig
                            {
                                Exporter = new SimpleTracerExporterConfig
                                {
                                    OtlpGrpc = new OtlpGrpcExporterConfig(),
                                    Zipkin = new ZipkinExporterConfig()
                                }
                            },
                        }
                    ]
                }
            }),
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors =
                    [
                        new ProcessorConfig
                        {
                            Simple = new SimpleProcessorConfig
                            {
                                Exporter = new SimpleTracerExporterConfig
                                {
                                    OtlpGrpc = new OtlpGrpcExporterConfig(),
                                    Console = new object()
                                }
                            },
                        }
                    ]
                }
            }),
            new(new YamlConfiguration
            {
                TracerProvider = new TracerProviderConfiguration
                {
                    Processors =
                    [
                        new ProcessorConfig
                        {
                            Simple = new SimpleProcessorConfig
                            {
                                Exporter = new SimpleTracerExporterConfig
                                {
                                    Zipkin = new ZipkinExporterConfig(),
                                    Console = new object()
                                }
                            },
                        }
                    ]
                }
            }),
        ];
    }

    [Fact]
    public void LoadFile_SetsBatchProcessorAndExportersCorrectly()
    {
        var exporter1 = new BatchTracerExporterConfig
        {
            OtlpGrpc = new OtlpGrpcExporterConfig
            {
                Endpoint = "http://localhost:4317/"
            }
        };

        var exporter2 = new BatchTracerExporterConfig
        {
            Zipkin = new ZipkinExporterConfig
            {
                Endpoint = "http://localhost:9411/"
            }
        };

        var batchProcessorConfig1 = new BatchProcessorConfig
        {
            ScheduleDelay = 1000,
            ExportTimeout = 30000,
            MaxQueueSize = 2048,
            MaxExportBatchSize = 512,
            Exporter = exporter1
        };

        var batchProcessorConfig2 = new BatchProcessorConfig
        {
            Exporter = exporter2
        };

        var conf = new YamlConfiguration
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Processors =
                [
                    new ProcessorConfig
                    {
                        Batch = batchProcessorConfig1
                    },
                    new ProcessorConfig
                    {
                        Batch = batchProcessorConfig2
                    }
                ]
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.True(settings.TracesEnabled);
        Assert.NotNull(settings.Processors);
        Assert.Equal(2, settings.Processors.Count);

        Assert.Empty(settings.TracesExporters);
    }

    [Fact]
    public void LoadFile_DisablesTraces_WhenNoBatchProcessorConfigured()
    {
        var conf = new YamlConfiguration
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Processors = []
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.False(settings.TracesEnabled);
        Assert.Empty(settings.TracesExporters);
        Assert.Null(settings.OtlpSettings);
        Assert.NotNull(settings.Processors);
        Assert.Empty(settings.Processors);
    }

    [Fact]
    public void LoadFile_HandlesNullExporterGracefully()
    {
        var batchProcessorConfig = new BatchProcessorConfig
        {
            Exporter = null
        };

        var conf = new YamlConfiguration
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Processors =
                [
                    new ProcessorConfig
                    {
                        Batch = batchProcessorConfig
                    }
                ]
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.True(settings.TracesEnabled);
        Assert.Empty(settings.TracesExporters);
    }

    [Theory]
    [MemberData(nameof(LoadMethod_SkipWrongExporterConfiguration_Data))]
    public void LoadMethod_SkipWrongExporterConfiguration(SkipConfigurationTestCase skipConfigurationTestCase)
    {
#if NET
        ArgumentNullException.ThrowIfNull(skipConfigurationTestCase);
#else
        if (skipConfigurationTestCase == null)
        {
            throw new ArgumentNullException(nameof(skipConfigurationTestCase));
        }
#endif

        var settings = new TracerSettings();

        settings.LoadFile(skipConfigurationTestCase.Configuration);

        Assert.Empty(settings.TracesExporters);
    }

    [Fact]
    public void LoadFile_ConfiguresParentBasedSampler()
    {
        var samplerConfig = new SamplerConfig
        {
            ParentBased = new ParentBasedSamplerConfig
            {
                Root = new SamplerVariantsConfig { AlwaysOn = new object() },
                RemoteParentSampled = new SamplerVariantsConfig { AlwaysOn = new object() },
                RemoteParentNotSampled = new SamplerVariantsConfig { AlwaysOff = new object() },
                LocalParentSampled = new SamplerVariantsConfig { AlwaysOn = new object() },
                LocalParentNotSampled = new SamplerVariantsConfig { AlwaysOff = new object() }
            }
        };

        var conf = new YamlConfiguration
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Sampler = samplerConfig
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        var sampler = Assert.IsType<ParentBasedSampler>(settings.Sampler);

        Assert.Equal(SamplingDecision.RecordAndSample, sampler.ShouldSample(CreateSamplingParameters(default)).Decision);

        var remoteSampledParent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, traceState: null, isRemote: true);
        Assert.Equal(SamplingDecision.RecordAndSample, sampler.ShouldSample(CreateSamplingParameters(remoteSampledParent)).Decision);

        var remoteNotSampledParent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, traceState: null, isRemote: true);
        Assert.Equal(SamplingDecision.Drop, sampler.ShouldSample(CreateSamplingParameters(remoteNotSampledParent)).Decision);

        var localSampledParent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, traceState: null, isRemote: false);
        Assert.Equal(SamplingDecision.RecordAndSample, sampler.ShouldSample(CreateSamplingParameters(localSampledParent)).Decision);

        var localNotSampledParent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, traceState: null, isRemote: false);
        Assert.Equal(SamplingDecision.Drop, sampler.ShouldSample(CreateSamplingParameters(localNotSampledParent)).Decision);
    }

    [Fact]
    public void LoadFile_ConfiguresParentBasedSamplerWithTraceIdRatio()
    {
        const double ratio = 0.25;

        var samplerConfig = new SamplerConfig
        {
            ParentBased = new ParentBasedSamplerConfig
            {
                Root = new SamplerVariantsConfig
                {
                    TraceIdRatio = new TraceIdRatioSamplerConfig { Ratio = ratio }
                }
            }
        };

        var conf = new YamlConfiguration
        {
            TracerProvider = new TracerProviderConfiguration { Sampler = samplerConfig }
        };

        var settings = new TracerSettings();
        settings.LoadFile(conf);

        var sampler = Assert.IsType<ParentBasedSampler>(settings.Sampler);

        var pbType = typeof(ParentBasedSampler);
        var pbFieldVals = pbType
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(f => f.GetValue(sampler))
            .Where(v => v is not null)
            .ToList();
        var pbPropVals = pbType
            .GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Select(p => p.GetValue(sampler))
            .Where(v => v is not null)
            .ToList();

        var rootObj = pbFieldVals.Concat(pbPropVals)
            .FirstOrDefault(v => v is TraceIdRatioBasedSampler);
        Assert.NotNull(rootObj);

        var rootSampler = Assert.IsType<TraceIdRatioBasedSampler>(rootObj);
        var tirType = typeof(TraceIdRatioBasedSampler);
        var ratioCandidates = tirType
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(double))
            .Select(f => f.GetValue(rootSampler))
            .OfType<double>()
            .ToList();

        Assert.NotEmpty(ratioCandidates);

        // Using an epsilon-based comparison instead of direct equality
        // because floating-point values can differ slightly due to precision errors.
        // This ensures the test is stable.
        Assert.Contains(ratioCandidates, v => Math.Abs(v - ratio) < 1e-9);

        var noParent = default(ActivityContext);
        var decision = sampler.ShouldSample(CreateSamplingParameters(noParent)).Decision;
        Assert.True(decision == SamplingDecision.RecordAndSample || decision == SamplingDecision.Drop);

        var remoteSampledParent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, traceState: null, isRemote: true);
        Assert.Equal(SamplingDecision.RecordAndSample, sampler.ShouldSample(CreateSamplingParameters(remoteSampledParent)).Decision);

        var remoteNotSampledParent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, traceState: null, isRemote: true);
        Assert.Equal(SamplingDecision.Drop, sampler.ShouldSample(CreateSamplingParameters(remoteNotSampledParent)).Decision);

        var localSampledParent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, traceState: null, isRemote: false);
        Assert.Equal(SamplingDecision.RecordAndSample, sampler.ShouldSample(CreateSamplingParameters(localSampledParent)).Decision);

        var localNotSampledParent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, traceState: null, isRemote: false);
        Assert.Equal(SamplingDecision.Drop, sampler.ShouldSample(CreateSamplingParameters(localNotSampledParent)).Decision);
    }

    [Fact]
    public void LoadFile_ConfiguresAlwaysOnSampler()
    {
        var conf = new YamlConfiguration
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Sampler = new SamplerConfig { AlwaysOn = new object() }
            }
        };

        var settings = new TracerSettings();
        settings.LoadFile(conf);

        var sampler = Assert.IsType<AlwaysOnSampler>(settings.Sampler);
        var decision = sampler.ShouldSample(CreateSamplingParameters(default)).Decision;
        Assert.Equal(SamplingDecision.RecordAndSample, decision);
    }

    [Fact]
    public void LoadFile_ConfiguresAlwaysOffSampler()
    {
        var conf = new YamlConfiguration
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Sampler = new SamplerConfig { AlwaysOff = new object() }
            }
        };

        var settings = new TracerSettings();
        settings.LoadFile(conf);

        var sampler = Assert.IsType<AlwaysOffSampler>(settings.Sampler);
        var decision = sampler.ShouldSample(CreateSamplingParameters(default)).Decision;
        Assert.Equal(SamplingDecision.Drop, decision);
    }

    [Fact]
    public void LoadFile_ConfiguresTraceIdRatioSampler()
    {
        const double ratio = 0.25;

        var conf = new YamlConfiguration
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Sampler = new SamplerConfig
                {
                    TraceIdRatio = new TraceIdRatioSamplerConfig
                    {
                        Ratio = ratio
                    }
                }
            }
        };

        var settings = new TracerSettings();
        settings.LoadFile(conf);

        var sampler = Assert.IsType<TraceIdRatioBasedSampler>(settings.Sampler);
        var decision = sampler.ShouldSample(CreateSamplingParameters(default)).Decision;
        Assert.True(decision == SamplingDecision.Drop || decision == SamplingDecision.RecordAndSample);
    }

    private static SamplingParameters CreateSamplingParameters(ActivityContext parentContext)
    {
        return new SamplingParameters(parentContext, ActivityTraceId.CreateRandom(), "span", ActivityKind.Internal, default(TagList), []);
    }

#pragma warning disable CA1515 // Consider making public types internal. Needed for xunit test purposes test cases.
#pragma warning disable CA1034 // Nested types should not be visible. It is used only for test purposes.
    public class SkipConfigurationTestCase
#pragma warning restore CA1034 // Nested types should not be visible. It is used only for test purposes.
#pragma warning restore CA1515 // Consider making public types internal. Needed for xunit test purposes test cases.
    {
        internal SkipConfigurationTestCase(YamlConfiguration configuration)
        {
            Configuration = configuration;
        }

        internal YamlConfiguration Configuration { get; }
    }
}
