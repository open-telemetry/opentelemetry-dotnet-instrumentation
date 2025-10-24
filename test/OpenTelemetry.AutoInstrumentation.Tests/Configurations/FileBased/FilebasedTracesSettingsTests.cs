// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
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
        var settings = new TracerSettings();

        settings.LoadFile(skipConfigurationTestCase.Configuration);

        Assert.Empty(settings.TracesExporters);
    }

    public class SkipConfigurationTestCase
    {
        internal SkipConfigurationTestCase(YamlConfiguration configuration)
        {
            Configuration = configuration;
        }

        internal YamlConfiguration Configuration { get; }
    }

    [Fact]
    public void LoadFile_ConfiguresParentBasedSampler()
    {
        var samplerConfig = new SamplerConfig
        {
            ParentBased = new ParentBasedSamplerConfig
            {
                Root = new SamplerConfig { AlwaysOn = new AlwaysOnSamplerConfig() },
                RemoteParentSampled = new SamplerConfig { AlwaysOn = new AlwaysOnSamplerConfig() },
                RemoteParentNotSampled = new SamplerConfig { AlwaysOff = new AlwaysOffSamplerConfig() },
                LocalParentSampled = new SamplerConfig { AlwaysOn = new AlwaysOnSamplerConfig() },
                LocalParentNotSampled = new SamplerConfig { AlwaysOff = new AlwaysOffSamplerConfig() }
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

    private static SamplingParameters CreateSamplingParameters(ActivityContext parentContext)
    {
        return new SamplingParameters(parentContext, ActivityTraceId.CreateRandom(), "span", ActivityKind.Internal, new TagList(), new ActivityLink[] { });
    }
}
