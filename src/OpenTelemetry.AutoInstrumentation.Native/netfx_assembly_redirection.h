/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

// Auto-generated file, do not change it - generated by the AssemblyRedirectionSourceGenerator type

#include "cor_profiler.h"

#ifdef _WIN32
#define STR(Z1) #Z1
#define AUTO_MAJOR STR(OTEL_AUTO_VERSION_MAJOR) 

namespace trace
{
void CorProfiler::InitNetFxAssemblyRedirectsMap()
{
    const USHORT auto_major = atoi(AUTO_MAJOR);

    assembly_version_redirect_map_.insert({
        { L"Google.Protobuf", {3, 24, 0, 0} },
        { L"Grpc.Core", {2, 0, 0, 0} },
        { L"Grpc.Core.Api", {2, 0, 0, 0} },
        { L"Microsoft.Bcl.AsyncInterfaces", {7, 0, 0, 0} },
        { L"Microsoft.Extensions.Configuration", {7, 0, 0, 0} },
        { L"Microsoft.Extensions.Configuration.Abstractions", {7, 0, 0, 0} },
        { L"Microsoft.Extensions.Configuration.Binder", {7, 0, 0, 4} },
        { L"Microsoft.Extensions.DependencyInjection", {7, 0, 0, 0} },
        { L"Microsoft.Extensions.DependencyInjection.Abstractions", {7, 0, 0, 0} },
        { L"Microsoft.Extensions.Logging", {7, 0, 0, 0} },
        { L"Microsoft.Extensions.Logging.Abstractions", {7, 0, 0, 1} },
        { L"Microsoft.Extensions.Logging.Configuration", {7, 0, 0, 0} },
        { L"Microsoft.Extensions.Options", {7, 0, 0, 1} },
        { L"Microsoft.Extensions.Options.ConfigurationExtensions", {7, 0, 0, 0} },
        { L"Microsoft.Extensions.Primitives", {7, 0, 0, 0} },
        { L"Microsoft.Win32.Primitives", {4, 0, 3, 0} },
        { L"MongoDB.Driver.Core.Extensions.DiagnosticSources", {1, 0, 0, 0} },
        { L"OpenTelemetry", {1, 0, 0, 0} },
        { L"OpenTelemetry.Api", {1, 0, 0, 0} },
        { L"OpenTelemetry.Api.ProviderBuilderExtensions", {1, 0, 0, 0} },
        { L"OpenTelemetry.AutoInstrumentation", {auto_major, 0, 0, 0} },
        { L"OpenTelemetry.Exporter.Console", {1, 0, 0, 0} },
        { L"OpenTelemetry.Exporter.OpenTelemetryProtocol", {1, 0, 0, 0} },
        { L"OpenTelemetry.Exporter.Prometheus.HttpListener", {1, 0, 0, 0} },
        { L"OpenTelemetry.Exporter.Zipkin", {1, 0, 0, 0} },
        { L"OpenTelemetry.Extensions.Propagators", {1, 0, 0, 0} },
        { L"OpenTelemetry.Instrumentation.AspNet", {1, 0, 0, 9} },
        { L"OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule", {1, 0, 0, 9} },
        { L"OpenTelemetry.Instrumentation.GrpcNetClient", {1, 0, 0, 0} },
        { L"OpenTelemetry.Instrumentation.Http", {1, 0, 0, 0} },
        { L"OpenTelemetry.Instrumentation.Process", {0, 5, 0, 3} },
        { L"OpenTelemetry.Instrumentation.Quartz", {1, 0, 0, 3} },
        { L"OpenTelemetry.Instrumentation.Runtime", {1, 5, 0, 0} },
        { L"OpenTelemetry.Instrumentation.SqlClient", {1, 0, 0, 0} },
        { L"OpenTelemetry.Instrumentation.Wcf", {1, 0, 0, 10} },
        { L"OpenTelemetry.ResourceDetectors.Azure", {1, 0, 0, 2} },
        { L"OpenTelemetry.ResourceDetectors.Container", {1, 0, 0, 4} },
        { L"OpenTelemetry.Shims.OpenTracing", {1, 0, 0, 0} },
        { L"OpenTracing", {0, 12, 1, 0} },
        { L"System.AppContext", {4, 1, 2, 0} },
        { L"System.Buffers", {4, 0, 3, 0} },
        { L"System.Collections", {4, 0, 11, 0} },
        { L"System.Collections.Concurrent", {4, 0, 11, 0} },
        { L"System.Collections.NonGeneric", {4, 0, 3, 0} },
        { L"System.Collections.Specialized", {4, 0, 3, 0} },
        { L"System.ComponentModel", {4, 0, 1, 0} },
        { L"System.ComponentModel.Annotations", {4, 2, 1, 0} },
        { L"System.ComponentModel.EventBasedAsync", {4, 0, 11, 0} },
        { L"System.ComponentModel.Primitives", {4, 1, 2, 0} },
        { L"System.ComponentModel.TypeConverter", {4, 1, 2, 0} },
        { L"System.Console", {4, 0, 2, 0} },
        { L"System.Data.Common", {4, 2, 0, 0} },
        { L"System.Diagnostics.Contracts", {4, 0, 1, 0} },
        { L"System.Diagnostics.Debug", {4, 0, 11, 0} },
        { L"System.Diagnostics.DiagnosticSource", {7, 0, 0, 2} },
        { L"System.Diagnostics.FileVersionInfo", {4, 0, 2, 0} },
        { L"System.Diagnostics.Process", {4, 1, 2, 0} },
        { L"System.Diagnostics.StackTrace", {4, 1, 0, 0} },
        { L"System.Diagnostics.TextWriterTraceListener", {4, 0, 2, 0} },
        { L"System.Diagnostics.Tools", {4, 0, 1, 0} },
        { L"System.Diagnostics.TraceSource", {4, 0, 2, 0} },
        { L"System.Diagnostics.Tracing", {4, 2, 0, 0} },
        { L"System.Drawing.Primitives", {4, 0, 2, 0} },
        { L"System.Dynamic.Runtime", {4, 0, 11, 0} },
        { L"System.Globalization", {4, 0, 11, 0} },
        { L"System.Globalization.Calendars", {4, 0, 3, 0} },
        { L"System.Globalization.Extensions", {4, 1, 0, 0} },
        { L"System.IO", {4, 1, 2, 0} },
        { L"System.IO.Compression", {4, 2, 0, 0} },
        { L"System.IO.Compression.ZipFile", {4, 0, 3, 0} },
        { L"System.IO.FileSystem", {4, 0, 3, 0} },
        { L"System.IO.FileSystem.DriveInfo", {4, 0, 2, 0} },
        { L"System.IO.FileSystem.Primitives", {4, 0, 3, 0} },
        { L"System.IO.FileSystem.Watcher", {4, 0, 2, 0} },
        { L"System.IO.IsolatedStorage", {4, 0, 2, 0} },
        { L"System.IO.MemoryMappedFiles", {4, 0, 2, 0} },
        { L"System.IO.Pipes", {4, 0, 2, 0} },
        { L"System.IO.UnmanagedMemoryStream", {4, 0, 3, 0} },
        { L"System.Linq", {4, 1, 2, 0} },
        { L"System.Linq.Expressions", {4, 1, 2, 0} },
        { L"System.Linq.Parallel", {4, 0, 1, 0} },
        { L"System.Linq.Queryable", {4, 0, 1, 0} },
        { L"System.Memory", {4, 0, 1, 2} },
        { L"System.Net.Http", {4, 2, 0, 0} },
        { L"System.Net.NameResolution", {4, 0, 2, 0} },
        { L"System.Net.NetworkInformation", {4, 1, 2, 0} },
        { L"System.Net.Ping", {4, 0, 2, 0} },
        { L"System.Net.Primitives", {4, 0, 11, 0} },
        { L"System.Net.Requests", {4, 0, 11, 0} },
        { L"System.Net.Security", {4, 0, 2, 0} },
        { L"System.Net.Sockets", {4, 2, 0, 0} },
        { L"System.Net.WebHeaderCollection", {4, 0, 1, 0} },
        { L"System.Net.WebSockets", {4, 0, 2, 0} },
        { L"System.Net.WebSockets.Client", {4, 0, 2, 0} },
        { L"System.Numerics.Vectors", {4, 1, 4, 0} },
        { L"System.ObjectModel", {4, 0, 11, 0} },
        { L"System.Reflection", {4, 1, 2, 0} },
        { L"System.Reflection.Extensions", {4, 0, 1, 0} },
        { L"System.Reflection.Primitives", {4, 0, 1, 0} },
        { L"System.Resources.Reader", {4, 0, 2, 0} },
        { L"System.Resources.ResourceManager", {4, 0, 1, 0} },
        { L"System.Resources.Writer", {4, 0, 2, 0} },
        { L"System.Runtime", {4, 1, 2, 0} },
        { L"System.Runtime.CompilerServices.Unsafe", {6, 0, 0, 0} },
        { L"System.Runtime.CompilerServices.VisualC", {4, 0, 2, 0} },
        { L"System.Runtime.Extensions", {4, 1, 2, 0} },
        { L"System.Runtime.Handles", {4, 0, 1, 0} },
        { L"System.Runtime.InteropServices", {4, 1, 2, 0} },
        { L"System.Runtime.InteropServices.RuntimeInformation", {4, 0, 2, 0} },
        { L"System.Runtime.Numerics", {4, 0, 1, 0} },
        { L"System.Runtime.Serialization.Formatters", {4, 0, 2, 0} },
        { L"System.Runtime.Serialization.Json", {4, 0, 1, 0} },
        { L"System.Runtime.Serialization.Primitives", {4, 2, 0, 0} },
        { L"System.Runtime.Serialization.Xml", {4, 1, 3, 0} },
        { L"System.Security.Claims", {4, 0, 3, 0} },
        { L"System.Security.Cryptography.Algorithms", {4, 3, 0, 0} },
        { L"System.Security.Cryptography.Csp", {4, 0, 2, 0} },
        { L"System.Security.Cryptography.Encoding", {4, 0, 2, 0} },
        { L"System.Security.Cryptography.Primitives", {4, 0, 2, 0} },
        { L"System.Security.Cryptography.X509Certificates", {4, 1, 2, 0} },
        { L"System.Security.Principal", {4, 0, 1, 0} },
        { L"System.Security.SecureString", {4, 1, 0, 0} },
        { L"System.Text.Encoding", {4, 0, 11, 0} },
        { L"System.Text.Encoding.Extensions", {4, 0, 11, 0} },
        { L"System.Text.Encodings.Web", {7, 0, 0, 0} },
        { L"System.Text.Json", {7, 0, 0, 3} },
        { L"System.Text.RegularExpressions", {4, 1, 1, 0} },
        { L"System.Threading", {4, 0, 11, 0} },
        { L"System.Threading.Overlapped", {4, 1, 0, 0} },
        { L"System.Threading.Tasks", {4, 0, 11, 0} },
        { L"System.Threading.Tasks.Extensions", {4, 2, 0, 1} },
        { L"System.Threading.Tasks.Parallel", {4, 0, 1, 0} },
        { L"System.Threading.Thread", {4, 0, 2, 0} },
        { L"System.Threading.ThreadPool", {4, 0, 12, 0} },
        { L"System.Threading.Timer", {4, 0, 1, 0} },
        { L"System.ValueTuple", {4, 0, 3, 0} },
        { L"System.Xml.ReaderWriter", {4, 1, 1, 0} },
        { L"System.Xml.XDocument", {4, 0, 11, 0} },
        { L"System.Xml.XmlDocument", {4, 0, 3, 0} },
        { L"System.Xml.XmlSerializer", {4, 0, 11, 0} },
        { L"System.Xml.XPath", {4, 0, 3, 0} },
        { L"System.Xml.XPath.XDocument", {4, 1, 0, 0} }
    });
}
}
#endif
