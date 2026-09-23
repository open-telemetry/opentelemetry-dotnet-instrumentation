// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// OpAMP extensions of the plugin.
/// </summary>
public partial class Plugin : IPlugin, IOpAmpPlugin, IProvideEffectiveConfig, IProvideRemoteConfigStatus, IOpAmpListener<CustomCapabilitiesMessage>, IOpAmpListener<CustomMessageMessage>
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    private const string PostStartReportingEnvironmentVariable = "TEST_OPAMP_POST_START_REPORTING";
    private const string PostStartCustomCapability = "com.example.opamp.post-start";
    private readonly TaskCompletionSource<bool> _serverCustomCapabilitiesReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private IOpAmpClient? _opAmpClient;
    private int _postStartReportingStateChanged;

    public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        ThrowIfMissing(settings);
        settings.EffectiveConfigurationReporting.EnableReporting = true;
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = true;
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureOpAmpOptions)}() invoked.");
        Console.WriteLine($"{nameof(settings.MaxPendingCustomMessages)}: {settings.MaxPendingCustomMessages}");
        Console.WriteLine($"{nameof(settings.MaxPendingCustomMessageBytes)}: {settings.MaxPendingCustomMessageBytes}");
    }

    public void ConfigureOpAmpClient(IOpAmpClient client)
    {
        ThrowIfMissing(client);
        _opAmpClient = client;
        client.Subscribe<CustomCapabilitiesMessage>(this);
        client.Subscribe<CustomMessageMessage>(this);
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureOpAmpClient)}() invoked.");
    }

    public void AfterOpAmpClientStarted()
    {
        if (IsPostStartReportingEnabled())
        {
            WaitForPostStartReporting(ReportStateAfterStartupAsync());
        }

        Console.WriteLine($"{nameof(Plugin)}.{nameof(AfterOpAmpClientStarted)}() invoked.");
    }

    public void BeforeOpAmpClientStopped()
    {
        _opAmpClient?.Unsubscribe<CustomCapabilitiesMessage>(this);
        _opAmpClient?.Unsubscribe<CustomMessageMessage>(this);
        _opAmpClient = null;
        Console.WriteLine($"{nameof(Plugin)}.{nameof(BeforeOpAmpClientStopped)}() invoked.");
    }

    public void HandleMessage(CustomMessageMessage message)
    {
        ThrowIfMissing(message);
        Console.WriteLine($"{nameof(Plugin)}.{nameof(HandleMessage)}({nameof(CustomMessageMessage)}) invoked: {message.Type}.");
    }

    public void HandleMessage(CustomCapabilitiesMessage message)
    {
        ThrowIfMissing(message);
        if (message.Capabilities.Contains(PostStartCustomCapability))
        {
            _serverCustomCapabilitiesReceived.TrySetResult(true);
        }
    }

    public IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(GetEffectiveConfig)}() invoked.");

        return Volatile.Read(ref _postStartReportingStateChanged) == 0
            ? []
            : [new EffectiveConfigFile("post-start"u8.ToArray(), "text/plain", "plugin")];
    }

    public RemoteConfigStatusReport? GetRemoteConfigStatus()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(GetRemoteConfigStatus)}() invoked.");

        return Volatile.Read(ref _postStartReportingStateChanged) == 0
            ? new RemoteConfigStatusReport("temporary"u8, RemoteConfigStatusCode.Unset)
            : new RemoteConfigStatusReport("post-start"u8, RemoteConfigStatusCode.Applied);
    }

    private static bool IsPostStartReportingEnabled()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable(PostStartReportingEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static void WaitForPostStartReporting(Task reportingTask)
    {
        var completedTask = Task.WhenAny(reportingTask, Task.Delay(TimeSpan.FromSeconds(5))).GetAwaiter().GetResult();
        if (!ReferenceEquals(completedTask, reportingTask))
        {
            throw new TimeoutException("Timed out waiting for post-start OpAMP reporting.");
        }

        reportingTask.GetAwaiter().GetResult();
    }

    private async Task ReportStateAfterStartupAsync()
    {
        var client = _opAmpClient ?? throw new InvalidOperationException("The OpAMP client was not configured.");
        Volatile.Write(ref _postStartReportingStateChanged, 1);
        client.ReportCustomCapabilities([PostStartCustomCapability]);

        // Custom messages are accepted only after the capability has been submitted.
        await client.FlushAsync(CancellationToken.None).ConfigureAwait(false);

        await _serverCustomCapabilitiesReceived.Task.ConfigureAwait(false);
        client.SendCustomMessage(PostStartCustomCapability, "post-start", "payload"u8.ToArray());
        client.NotifyEffectiveConfigChanged();
        client.NotifyRemoteConfigStatusChanged();
        await client.FlushAsync(CancellationToken.None).ConfigureAwait(false);

        Console.WriteLine($"{nameof(Plugin)} post-start OpAMP reporting completed.");
    }
}
