// <copyright file="DelegateMetadata.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Emit;

internal class DelegateMetadata
{
    public Type Type { get; set; }

    public Type ReturnType { get; set; }

    public Type[] Generics { get; set; }

    public Type[] Parameters { get; set; }

    public static DelegateMetadata Create<TDelegate>()
        where TDelegate : Delegate
    {
        Type delegateType = typeof(TDelegate);
        Type[] genericTypeArguments = delegateType.GenericTypeArguments;

        Type[] parameterTypes;
        Type returnType;

        if (delegateType.Name.StartsWith("Func`"))
        {
            // last generic type argument is the return type
            int parameterCount = genericTypeArguments.Length - 1;
            parameterTypes = new Type[parameterCount];
            Array.Copy(genericTypeArguments, parameterTypes, parameterCount);

            returnType = genericTypeArguments[parameterCount];
        }
        else if (delegateType.Name.StartsWith("Action`"))
        {
            parameterTypes = genericTypeArguments;
            returnType = typeof(void);
        }
        else
        {
            throw new Exception($"Only Func<> or Action<> are supported in {nameof(DelegateMetadata)}.");
        }

        return new DelegateMetadata()
        {
            Generics = genericTypeArguments,
            Parameters = parameterTypes,
            ReturnType = returnType,
            Type = delegateType
        };
    }
}
