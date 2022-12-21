// <copyright file="ClrNames.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation;

internal static class ClrNames
{
    public const string Ignore = "_";

    public const string Void = "System.Void";
    public const string Object = "System.Object";
    public const string Bool = "System.Boolean";
    public const string String = "System.String";

    public const string SByte = "System.SByte";
    public const string Byte = "System.Byte";

    public const string Int16 = "System.Int16";
    public const string Int32 = "System.Int32";
    public const string Int64 = "System.Int64";

    public const string UInt16 = "System.UInt16";
    public const string UInt32 = "System.UInt32";
    public const string UInt64 = "System.UInt64";

    public const string Stream = "System.IO.Stream";

    public const string Task = "System.Threading.Tasks.Task";
    public const string CancellationToken = "System.Threading.CancellationToken";

    // ReSharper disable once InconsistentNaming
    public const string IAsyncResult = "System.IAsyncResult";
    public const string AsyncCallback = "System.AsyncCallback";

    public const string HttpRequestMessage = "System.Net.Http.HttpRequestMessage";
    public const string HttpResponseMessage = "System.Net.Http.HttpResponseMessage";
    public const string HttpResponseMessageTask = "System.Threading.Tasks.Task`1<System.Net.Http.HttpResponseMessage>";

    public const string GenericTask = "System.Threading.Tasks.Task`1";
    public const string IgnoreGenericTask = "System.Threading.Tasks.Task`1<_>";
    public const string GenericParameterTask = "System.Threading.Tasks.Task`1<T>";
    public const string ObjectTask = "System.Threading.Tasks.Task`1<System.Object>";
    public const string Int32Task = "System.Threading.Tasks.Task`1<System.Int32>";
}
