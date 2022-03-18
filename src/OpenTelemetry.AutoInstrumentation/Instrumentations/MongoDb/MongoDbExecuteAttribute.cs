// <copyright file="MongoDbExecuteAttribute.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDb;

internal class MongoDbExecuteAttribute : MongoDbInstrumentMethodAttribute
{
    internal const string Major2 = "2";
    internal const string Major2Minor2 = "2.2"; // Synchronous methods added in 2.2

    public MongoDbExecuteAttribute(string typeName, bool isGeneric)
        : base(typeName)
    {
        MinimumVersion = Major2Minor2;
        MaximumVersion = Major2;
        MethodName = "Execute";
        ParameterTypeNames = new[] { "MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken };

        if (isGeneric)
        {
            ReturnTypeName = "T";
        }
        else
        {
            ReturnTypeName = ClrNames.Void;
        }
    }
}
