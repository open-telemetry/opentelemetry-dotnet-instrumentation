// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation;
using TestLibrary.InstrumentationTarget.StrongNamedValidation;

namespace TestLibrary.InstrumentationTarget;

internal class Plugin
{
    public void Initializing()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(Initializing)}() invoked.");
    }

    internal InstrumentationDefinitions.Payload GetAllDefinitionsPayload()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(GetAllDefinitionsPayload)}() invoked.");
        var payload = new InstrumentationDefinitions.Payload
        {
            DefinitionsId = "AA83654D58B24C67A4D35ED9E6716271",
            Definitions = new NativeCallTargetDefinition[]
            {
                // Trace - StrongNameValidation
                new("TestLibrary.InstrumentationTarget", "TestLibrary.InstrumentationTarget.Command", "Execute",  new[] { "System.Void" }, 1, 0, 0, 1, 65535, 65535, typeof(Validation).Assembly.FullName!, "TestLibrary.InstrumentationTarget.StrongNamedValidation.Validation"),
            }
        };

        return payload;
    }
}
