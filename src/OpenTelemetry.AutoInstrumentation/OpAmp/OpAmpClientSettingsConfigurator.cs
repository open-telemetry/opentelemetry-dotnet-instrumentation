// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Util;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Settings;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal static class OpAmpClientSettingsConfigurator
{
    public static void ConfigureDefaults(
        OpAmpClientSettings settings,
        OpAmpSettings opAmpSettings,
        Resource resources)
    {
        if (opAmpSettings.ServerUrl is { } serverUrl)
        {
            settings.ServerUrl = serverUrl;
            settings.ConnectionType = GetConnectionType(serverUrl);
        }

        if (opAmpSettings.MaxPendingCustomMessages.HasValue)
        {
            settings.MaxPendingCustomMessages = opAmpSettings.MaxPendingCustomMessages.Value;
        }

        if (opAmpSettings.MaxPendingCustomMessageBytes.HasValue)
        {
            settings.MaxPendingCustomMessageBytes = opAmpSettings.MaxPendingCustomMessageBytes.Value;
        }

        foreach (var resourceAttribute in resources.Attributes)
        {
            if (resourceAttribute.Key == null || resourceAttribute.Value == null)
            {
                continue;
            }

            AddAttribute(settings.Identification, resourceAttribute.Key, resourceAttribute.Value);
        }

        settings.Identification.AddNonIdentifyingAttribute("opamp.version", GetOpAmpVersion());
    }

    private static string GetOpAmpVersion()
    {
        return typeof(OpAmpClient).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?.Split(['+'], 2)[0] ?? "unknown";
    }

    private static ConnectionType GetConnectionType(Uri serverUrl)
    {
        if (serverUrl.Scheme == UriSchemes.Http || serverUrl.Scheme == UriSchemes.Https)
        {
            return ConnectionType.Http;
        }

        if (serverUrl.Scheme == UriSchemes.Ws || serverUrl.Scheme == UriSchemes.Wss)
        {
            return ConnectionType.WebSocket;
        }

        throw new NotSupportedException($"Connection type '{serverUrl.Scheme}' is not supported.");
    }

    private static void AddAttribute(IdentificationSettings settings, string key, object value)
    {
        switch (value)
        {
            case string stringValue:
                AddTypedAttribute(stringValue, settings.AddIdentifyingAttribute, settings.AddNonIdentifyingAttribute);
                break;
            case int intValue:
                AddTypedAttribute(intValue, settings.AddIdentifyingAttribute, settings.AddNonIdentifyingAttribute);
                break;
            case double doubleValue:
                AddTypedAttribute(doubleValue, settings.AddIdentifyingAttribute, settings.AddNonIdentifyingAttribute);
                break;
            case bool boolValue:
                AddTypedAttribute(boolValue, settings.AddIdentifyingAttribute, settings.AddNonIdentifyingAttribute);
                break;
            default:
                var convertedValue = value.ToString();
                if (!string.IsNullOrWhiteSpace(convertedValue))
                {
                    AddTypedAttribute(convertedValue, settings.AddIdentifyingAttribute, settings.AddNonIdentifyingAttribute);
                }

                break;
        }

        void AddTypedAttribute<T>(
            T attributeValue,
            Action<string, T> addIdentifyingAttribute,
            Action<string, T> addNonIdentifyingAttribute)
        {
            if (IsIdentifyingAttribute(key))
            {
                addIdentifyingAttribute(key, attributeValue);
            }
            else
            {
                addNonIdentifyingAttribute(key, attributeValue);
            }
        }
    }

    private static bool IsIdentifyingAttribute(string attributeName)
    {
        return attributeName
            is Constants.ResourceAttributes.AttributeServiceName
            or Constants.ResourceAttributes.AttributeServiceInstanceId
            or Constants.ResourceAttributes.AttributeServiceNamespace;
    }
}
