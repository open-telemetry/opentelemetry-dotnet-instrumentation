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

using System;
using System.Collections;
using System.Collections.Generic;
using Vendors.YamlDotNet.Core;
using Vendors.YamlDotNet.Helpers;
using Vendors.YamlDotNet.Serialization.Utilities;

namespace Vendors.YamlDotNet.Serialization.NodeDeserializers
{
    internal sealed class FsharpListNodeDeserializer : INodeDeserializer
    {
        private readonly ITypeInspector typeInspector;
        private readonly INamingConvention enumNamingConvention;

        public FsharpListNodeDeserializer(ITypeInspector typeInspector, INamingConvention enumNamingConvention)
        {
            this.typeInspector = typeInspector;
            this.enumNamingConvention = enumNamingConvention;
        }

        public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            if (!FsharpHelper.IsFsharpListType(expectedType))
            {
                value = false;
                return false;
            }

            var itemsType = expectedType.GetGenericArguments()[0];
            var collectionType = expectedType.GetGenericTypeDefinition().MakeGenericType(itemsType);

            var items = new ArrayList();
            CollectionNodeDeserializer.DeserializeHelper(itemsType, parser, nestedObjectDeserializer, items, true, enumNamingConvention, typeInspector);

            var array = Array.CreateInstance(itemsType, items.Count);
            items.CopyTo(array, 0);

            var collection = FsharpHelper.CreateFsharpListFromArray(collectionType, itemsType, array);
            value = collection;
            return true;
        }
    }
}
