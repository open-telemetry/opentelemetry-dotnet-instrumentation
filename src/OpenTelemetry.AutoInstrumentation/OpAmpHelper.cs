// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Settings;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation;

internal static class OpAmpHelper
{
    private static readonly CancellationTokenSource _cts = new();
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    private static OpAmpClient? _client;
    private static Task? _clientRunningTask;

    public static bool IsRunning { get; private set; }

    public static void EnableOpAmpClient(Resource resources, OpAmpSettings opAmpSettings)
    {
        try
        {
            _client = new OpAmpClient(settings => ConfigureClient(settings, opAmpSettings, resources));

            _clientRunningTask = Task.Run(async () =>
            {
                try
                {
                    IsRunning = true;

                    await _client.StartAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "OpAmp client stopped unexpectedly.");
                }
                finally
                {
                    IsRunning = false;
                }
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while initializing the OpAmp client.");
        }
    }

    public static void StopOpAmpClientIfRunning()
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            _client?.StopAsync().GetAwaiter().GetResult();

            if (_clientRunningTask != null)
            {
                _cts.Cancel();
                _clientRunningTask.GetAwaiter().GetResult();
            }

            _client?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while stopping the OpAmp client.");
        }
        finally
        {
            IsRunning = false;
        }
    }

    private static void ConfigureClient(OpAmpClientSettings settings, OpAmpSettings opAmpSettings, Resource resources)
    {
        // Late parse ensures that OpenTelemetry.OpAmp.Client.dll is loaded only when OpAmp is enabled.

        // Configure server URL.
        var serverUrl = opAmpSettings.ServerUrl;
        if (serverUrl != null)
        {
            settings.ServerUrl = serverUrl;
            settings.ConnectionType = GetConnectionType(serverUrl);
        }

        // Configure resource attributes for identification.
        foreach (var resourceAttribute in resources.Attributes)
        {
            if (resourceAttribute.Key == null ||
                resourceAttribute.Value == null)
            {
                continue;
            }

            var value = resourceAttribute.Value.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (IsIdentifyingAttribute(resourceAttribute.Key))
            {
                settings.Identification.AddIdentifyingAttribute(resourceAttribute.Key, value);
            }
            else
            {
                settings.Identification.AddNonIdentifyingAttribute(resourceAttribute.Key, value);
            }
        }

        settings.Identification.AddNonIdentifyingAttribute("opamp.version", GetOpAmpVersion());
    }

    private static ConnectionType GetConnectionType(Uri serverUrl)
    {
        if (serverUrl.Scheme == UriSchemes.Http ||
            serverUrl.Scheme == UriSchemes.Https)
        {
            return ConnectionType.Http;
        }

        if (serverUrl.Scheme == UriSchemes.Ws ||
            serverUrl.Scheme == UriSchemes.Wss)
        {
            return ConnectionType.WebSocket;
        }

        throw new NotSupportedException($"Connection type '{serverUrl.Scheme}' is not supported.");
    }

    private static string GetOpAmpVersion()
    {
        var assembly = typeof(OpAmpClient).Assembly;

        return assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?.Split(['+'], 2)[0] ?? "unknown";
    }

    private static bool IsIdentifyingAttribute(string attributeName)
    {
        return attributeName
            is Constants.ResourceAttributes.AttributeServiceName
            or Constants.ResourceAttributes.AttributeServiceInstanceId
            or Constants.ResourceAttributes.AttributeServiceNamespaceName;
    }
}
