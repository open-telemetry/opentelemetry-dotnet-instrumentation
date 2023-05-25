// <copyright file="Plugin.cs" company="OpenTelemetry Authors">
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
