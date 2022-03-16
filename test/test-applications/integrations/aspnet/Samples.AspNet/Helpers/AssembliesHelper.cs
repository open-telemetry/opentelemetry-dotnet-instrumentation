// <copyright file="AssembliesHelper.cs" company="OpenTelemetry Authors">
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

namespace Samples.AspNet.Helpers;

public static class AssembliesHelper
{
    public static ICollection<string> GetLoadedTracesAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => x.FullName.StartsWith("OpenTelemetry"))
            .Select(x => x.FullName)
            .OrderBy(x => x)
            .ToList();
    }

    public static ICollection<string> GetLoadedAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(x => x.FullName)
            .OrderBy(x => x)
            .ToList();
    }
}
