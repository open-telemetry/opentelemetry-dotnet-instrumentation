// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class DotNetMetrics
{
    /// <summary>
    /// Gets or sets the list of additional `System.Diagnostics.Metrics.Meter` names to be added to the meter at the startup.
    /// </summary>
    [YamlMember(Alias = "additional_sources")]
    public List<string>? AdditionalSources { get; set; }

    /// <summary>
    /// Gets or sets additional `System.Diagnostics.Metrics.Meter` names list to be added to the meter at the startup.
    /// </summary>
    [YamlMember(Alias = "additional_sources_list")]
    public string? AdditionalSourcesList { get; set; }

#if NETFRAMEWORK
    /// <summary>
    /// Gets or sets the ASP.NET metrics instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "aspnet")]
    public object? AspNet { get; set; }
#endif

#if NET
    /// <summary>
    /// Gets or sets the ASP.NET Core metrics instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "aspnetcore")]
    public object? AspNetCore { get; set; }
#endif

    /// <summary>
    /// Gets or sets the HttpClient metrics instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "httpclient")]
    public object? HttpClient { get; set; }

    /// <summary>
    /// Gets or sets the .NET runtime metrics instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "netruntime")]
    public object? NetRuntime { get; set; }

    /// <summary>
    /// Gets or sets the NServiceBus metrics instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "nservicebus")]
    public object? NServiceBus { get; set; }

    /// <summary>
    /// Gets or sets the process metrics instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "process")]
    public object? Process { get; set; }

    /// <summary>
    /// Gets or sets the SqlClient metrics instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "sqlclient")]
    public object? SqlClient { get; set; }

    /// <summary>
    /// Returns the list of enabled metric instrumentations.
    /// </summary>
    public IReadOnlyList<MetricInstrumentation> GetEnabledInstrumentations()
    {
        var enabled = new List<MetricInstrumentation>();
        var properties = typeof(DotNetMetrics).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(this);
            if (value != null)
            {
                if (Enum.TryParse<MetricInstrumentation>(prop.Name, out var instrumentation))
                {
                    enabled.Add(instrumentation);
                }
            }
        }

        return enabled;
    }
}
