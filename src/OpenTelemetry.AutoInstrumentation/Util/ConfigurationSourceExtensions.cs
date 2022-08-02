// <copyright file="ConfigurationSourceExtensions.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.AutoInstrumentation.Configuration;

namespace OpenTelemetry.AutoInstrumentation.Util
{
    internal static class ConfigurationSourceExtensions
    {
        public static IList<TEnum> ParseEnabledEnumList<TEnum>(this IConfigurationSource source, string enabledConfiguration, string disabledConfiguration, string error)
            where TEnum : struct, IConvertible
        {
            var instrumentations = new Dictionary<string, TEnum>();
            var enabledInstrumentations = source.GetString(enabledConfiguration);
            if (enabledInstrumentations != null)
            {
                if (enabledInstrumentations == Constants.ConfigurationValues.None)
                {
                    return Array.Empty<TEnum>();
                }

                foreach (var instrumentation in enabledInstrumentations.Split(Constants.ConfigurationValues.Separator))
                {
                    if (Enum.TryParse(instrumentation, out TEnum parsedType))
                    {
                        instrumentations[instrumentation] = parsedType;
                    }
                    else
                    {
                        throw new FormatException(string.Format(error, instrumentation));
                    }
                }
            }
            else
            {
                instrumentations = Enum.GetValues(typeof(TEnum))
                    .Cast<TEnum>()
                    .ToDictionary(
                        key => Enum.GetName(typeof(TEnum), key),
                        val => val);
            }

            var disabledInstrumentations = source.GetString(disabledConfiguration);
            if (disabledInstrumentations != null)
            {
                foreach (var instrumentation in disabledInstrumentations.Split(Constants.ConfigurationValues.Separator))
                {
                    instrumentations.Remove(instrumentation);
                }
            }

            return instrumentations.Values.ToList();
        }
    }
}
