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

using System;
using System.Collections;
using System.Collections.Generic;

namespace Samples.ExampleLibrary
{
    public class Class1
    {
        public int Add(int x, int y)
        {
            return x + y;
        }

        public virtual int Multiply(int x, int y)
        {
            return x * y;
        }

        public Func<int, int, int> Divide = (int x, int y) => x / y;

        public string ToCustomString()
        {
            return "Custom";
        }

        public object ToObject()
        {
            return this;
        }

        public Class1[] ToArray()
        {
            return new Class1[] { this };
        }

        public Array ToCustomArray()
        {
            var lengthsArray = new int[2] { 5, 10 };
            var lowerBoundsArray = new int[2] { 20, 15 };
            return Array.CreateInstance(typeof(Class1), lengthsArray, lowerBoundsArray);
        }

        public Class1[, ,] ToMdArray()
        {
            return new Class1[4, 2, 3];
        }

        public Class1[][] ToJaggedArray()
        {
            return new Class1[][]
            {
                new Class1[] { this },
                new Class1[] { null, null }
            };
        }

        public List<Class1> ToList()
        {
            return new List<Class1>() { this };
        }

        public List<Class1>.Enumerator ToEnumerator()
        {
            return ToList().GetEnumerator();
        }

        public DictionaryEntry ToDictionaryEntry()
        {
            return new DictionaryEntry("Class1", this);
        }

        public bool ToBool()
        {
            return false;
        }

        public char ToChar()
        {
            return 'b';
        }

        public sbyte ToSByte()
        {
            return 0x1;
        }

        public byte ToByte()
        {
            return 0x1;
        }

        public Int16 ToInt16()
        {
            return 16;
        }

        public UInt16 ToUInt16()
        {
            return 16;
        }

        public Int32 ToInt32()
        {
            return 32;
        }

        public UInt32 ToUInt32()
        {
            return 32;
        }

        public Int64 ToInt64()
        {
            return 64;
        }

        public UInt64 ToUInt64()
        {
            return 64;
        }

        public float ToSingle()
        {
            return 0.1f;
        }

        public double ToDouble()
        {
            return 0.1;
        }
    }
}
