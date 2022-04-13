// <copyright file="Biscuit.cs" company="OpenTelemetry Authors">
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

namespace TestApplication.ExampleLibrary.FakeClient
{
    public class Biscuit<T> : Biscuit
    {
        public T Reward { get; set; }
    }

    public class Biscuit
    {
        public Guid Id { get; set; }

        public string Message { get; set; }

        public List<object> Treats { get; set; } = new List<object>();

        public class Cookie
        {
            public bool IsYummy { get; set; }

            public class Raisin
            {
                public bool IsPurple { get; set; }
            }
        }
    }

    public struct StructBiscuit
    {
        public Guid Id { get; set; }

        public struct Cookie
        {
            public bool IsYummy { get; set; }
        }
    }
}
