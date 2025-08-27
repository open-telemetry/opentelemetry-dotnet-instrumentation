﻿// This file is part of YamlDotNet - A .NET library for YAML.
// Copyright (c) Antoine Aubry and contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace Vendors.YamlDotNet.Serialization
{
    /// <summary>
    /// The interface to implement for getting/setting an objects fields and properties when using a static context
    /// </summary>
    internal interface IObjectAccessor
    {
        /// <summary>
        /// Set a field/property value
        /// </summary>
        /// <param name="name">Name of the field or property.</param>
        /// <param name="target">Object to set the field/property on.</param>
        /// <param name="value">Value to set the field/property to.</param>
        void Set(string name, object target, object value);

        /// <summary>
        /// Reads a value from a field/property
        /// </summary>
        /// <param name="name">Name of the field or property.</param>
        /// <param name="target">Object to get the field/property from.</param>
        /// <returns></returns>
        object? Read(string name, object target);
    }
}
