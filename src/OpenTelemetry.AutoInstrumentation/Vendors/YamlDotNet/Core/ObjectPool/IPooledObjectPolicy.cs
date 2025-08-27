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


// Adapted from https://github.com/dotnet/aspnetcore/blob/2f1db20456007c9515068a35a65afdf99af70bc6/src/ObjectPool/src/DefaultObjectPool.cs which is
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Vendors.YamlDotNet.Core.ObjectPool
{
    /// <summary>
    /// Represents a policy for managing pooled objects.
    /// </summary>
    /// <typeparam name="T">The type of object which is being pooled.</typeparam>
    internal interface IPooledObjectPolicy<T> where T : notnull
    {
        /// <summary>
        /// Create a <typeparamref name="T"/>.
        /// </summary>
        /// <returns>The <typeparamref name="T"/> which was created.</returns>
        T Create();

        /// <summary>
        /// Runs some processing when an object was returned to the pool. Can be used to reset the state of an object and indicate if the object should be returned to the pool.
        /// </summary>
        /// <param name="obj">The object to return to the pool.</param>
        /// <returns><see langword="true" /> if the object should be returned to the pool. <see langword="false" /> if it's not possible/desirable for the pool to keep the object.</returns>
        bool Return(T obj);
    }
}
