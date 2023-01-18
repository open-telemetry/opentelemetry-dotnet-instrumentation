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
[assembly: InternalsVisibleTo("OpenTelemetry.AutoInstrumentation.Bootstrapping.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001008db7c66f4ebdc6aac4196be5ce1ff4b59b020028e6dbd6e46f15aa40b3215975b92d0a8e45aba5f36114a8cb56241fbfa49f4c017e6c62197857e4e9f62451bc23d3a660e20861f95a57f23e20c77d413ad216ff1bb55f94104d4c501e32b03219d8603fb6fa73401c6ae6808c8daa61b9eaee5d2377d3c23c9ca6016c6582d8")]
[assembly: InternalsVisibleTo("IntegrationTests, PublicKey=00240000048000009400000006020000002400005253413100040000010001008db7c66f4ebdc6aac4196be5ce1ff4b59b020028e6dbd6e46f15aa40b3215975b92d0a8e45aba5f36114a8cb56241fbfa49f4c017e6c62197857e4e9f62451bc23d3a660e20861f95a57f23e20c77d413ad216ff1bb55f94104d4c501e32b03219d8603fb6fa73401c6ae6808c8daa61b9eaee5d2377d3c23c9ca6016c6582d8")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

#endif
