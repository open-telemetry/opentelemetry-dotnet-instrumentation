// <copyright file="AssemblyInfo.NetFramework.cs" company="OpenTelemetry Authors">
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

#if NETFRAMEWORK
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyKeyFile("../keypair.snk")]

[assembly: InternalsVisibleTo("OpenTelemetry.AutoInstrumentation.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001008db7c66f4ebdc6aac4196be5ce1ff4b59b020028e6dbd6e46f15aa40b3215975b92d0a8e45aba5f36114a8cb56241fbfa49f4c017e6c62197857e4e9f62451bc23d3a660e20861f95a57f23e20c77d413ad216ff1bb55f94104d4c501e32b03219d8603fb6fa73401c6ae6808c8daa61b9eaee5d2377d3c23c9ca6016c6582d8")]

#endif
