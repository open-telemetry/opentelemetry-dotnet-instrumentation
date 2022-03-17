// <copyright file="Class1.cs" company="OpenTelemetry Authors">
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

namespace Samples.ExampleLibraryTracer
{
    public class Class1
    {
        public int Add(int x, int y)
        {
            return 2 * (x + y);
        }

        public virtual int Multiply(int x, int y)
        {
            return 2 * (x * y);
        }
    }
}
