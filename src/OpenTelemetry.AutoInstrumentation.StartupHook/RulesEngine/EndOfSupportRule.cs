// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal class EndOfSupportRule : Rule
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public EndOfSupportRule()
    {
        Name = ".NET End of Support Validator";
        Description = "Warns if application is executed on the framework expected to reach EoS withing 3 months.";
    }

    internal override bool Evaluate()
    {
        var netVersion = Environment.Version.Major;

        // dates from https://dotnet.microsoft.com/en-us/download/dotnet
        DateTime eosDate;
        switch (netVersion)
        {
            case 8:
            case 9:
                eosDate = new DateTime(2026, 11, 10);
                break;
            case 10:
                eosDate = new DateTime(2028, 11, 14);
                break;
            default:
                return true; // just return, not able to verify anything here
        }

        var now = DateTime.UtcNow;

        if (now > eosDate)
        {
            Logger.Warning("Rule Engine: .NET{0} reached End Of Support on {1}. Automatic Instrumentation and .NET is no longer supported. Upgrade your .NET version.", netVersion, eosDate.ToShortDateString());
        }
        else if (now > eosDate.AddMonths(-3))
        {
            Logger.Warning("Rule Engine: .NET{0} will reach End Of Support on {1}. After that date Automatic Instrumentation and .NET will be no longer supported. Consider upgrading your .NET version.", netVersion, eosDate.ToShortDateString());
        }

        return true;
    }
}
