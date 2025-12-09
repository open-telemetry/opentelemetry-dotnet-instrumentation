// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Reflection.Emit;

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// Duck Type
/// </summary>
internal static partial class DuckType
{
    private static List<MethodInfo> GetMethods(Type baseType)
    {
        var selectedMethods = new List<MethodInfo>(GetBaseMethods(baseType));
        // If the base type is an interface we must make sure we implement all methods, including from other interfaces
        if (baseType.IsInterface)
        {
            var implementedInterfaces = baseType.GetInterfaces();
            foreach (var imInterface in implementedInterfaces)
            {
                if (imInterface == typeof(IDuckType))
                {
                    continue;
                }

                foreach (var interfaceMethod in imInterface.GetMethods())
                {
                    if (interfaceMethod.IsSpecialName)
                    {
                        continue;
                    }

                    var interfaceMethodName = interfaceMethod.ToString();
                    var methodAlreadySelected = false;
                    foreach (var currentMethod in selectedMethods)
                    {
                        if (currentMethod.ToString() == interfaceMethodName)
                        {
                            methodAlreadySelected = true;
                            break;
                        }
                    }

                    if (!methodAlreadySelected)
                    {
                        var prevMethod = baseType.GetMethod(interfaceMethod.Name, DuckAttribute.DefaultFlags, null, interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray(), null);
                        if (prevMethod == null || prevMethod.GetCustomAttribute<DuckIgnoreAttribute>() is null)
                        {
                            selectedMethods.Add(interfaceMethod);
                        }
                    }
                }
            }
        }

        return selectedMethods;

        static IEnumerable<MethodInfo> GetBaseMethods(Type baseType)
        {
            foreach (var method in baseType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // Avoid proxying object methods like ToString(), GetHashCode()
                // or the Finalize() that creates problems by keeping alive the object to another collection.
                // You can still proxy those methods if they are defined in an interface, or if you add the DuckInclude attribute.
                if (method.DeclaringType == typeof(object))
                {
                    var include = method.GetCustomAttribute<DuckIncludeAttribute>(true) is not null;

                    if (!include)
                    {
                        continue;
                    }
                }

                if (method.IsSpecialName || method.IsFinal || method.IsPrivate)
                {
                    continue;
                }

                if (baseType.IsInterface || method.IsAbstract || method.IsVirtual)
                {
                    yield return method;
                }
            }
        }
    }

    private static void CreateMethods(
        TypeBuilder? proxyTypeBuilder,
        Type proxyType,
        Type targetType,
        FieldInfo? instanceField)
    {
        var proxyMethodsDefinitions = GetMethods(proxyType);

        var targetMethodsDefinitions = GetMethods(targetType);

        // These are the methods that we can attempt to duck type
        var allTargetMethods = targetType.GetMethods(DuckAttribute.DefaultFlags);

        foreach (var method in targetMethodsDefinitions)
        {
            if (method.GetCustomAttribute<DuckIncludeAttribute>(true) is not null)
            {
                proxyMethodsDefinitions.Add(method);
            }
        }

        foreach (var proxyMethodDefinition in proxyMethodsDefinitions)
        {
            // Ignore the method marked with `DuckIgnore` attribute
            if (proxyMethodDefinition.GetCustomAttribute<DuckIgnoreAttribute>(true) is not null)
            {
                continue;
            }

            // Check if proxy method is a reverse method (shouldn't be called from here)
            if (proxyMethodDefinition.GetCustomAttribute<DuckReverseMethodAttribute>(true) is not null)
            {
                DuckTypeIncorrectReverseMethodUsageException.Throw(proxyMethodDefinition);
            }

            // Extract the method parameters types
            var proxyMethodDefinitionParameters = proxyMethodDefinition.GetParameters();
            var proxyMethodDefinitionParametersTypes = proxyMethodDefinitionParameters.Select(p => p.ParameterType).ToArray();

            // We select the target method to call
            var targetMethod = SelectTargetMethod<DuckAttribute>(targetType, proxyMethodDefinition, proxyMethodDefinitionParameters, proxyMethodDefinitionParametersTypes, allTargetMethods);

            // If the target method couldn't be found we throw.
            if (targetMethod is null)
            {
                DuckTypeTargetMethodNotFoundException.Throw(proxyMethodDefinition);
                continue;
            }

            // Check if target method is a reverse method (shouldn't be called from here)
            if (targetMethod.GetCustomAttribute<DuckReverseMethodAttribute>(true) is not null)
            {
                DuckTypeIncorrectReverseMethodUsageException.Throw(targetMethod);
            }

            // Gets the proxy method definition generic arguments
            var proxyMethodDefinitionGenericArguments = proxyMethodDefinition.GetGenericArguments();
            var proxyMethodDefinitionGenericArgumentsNames = proxyMethodDefinitionGenericArguments.Select(a => a.Name).ToArray();

            // Checks if the target method is a generic method while the proxy method is non generic (checks if the Duck attribute contains the generic parameters)
            var targetMethodGenericArguments = targetMethod.GetGenericArguments();
            if (proxyMethodDefinitionGenericArguments.Length == 0 && targetMethodGenericArguments.Length > 0)
            {
                var proxyDuckAttribute = proxyMethodDefinition.GetCustomAttribute<DuckAttribute>();
                if (proxyDuckAttribute is null)
                {
                    DuckTypeTargetMethodNotFoundException.Throw(proxyMethodDefinition);
                }

                if (proxyDuckAttribute.GenericParameterTypeNames is null || proxyDuckAttribute.GenericParameterTypeNames.Length != targetMethodGenericArguments.Length)
                {
                    DuckTypeTargetMethodNotFoundException.Throw(proxyMethodDefinition);
                }

                targetMethod = targetMethod.MakeGenericMethod(proxyDuckAttribute.GenericParameterTypeNames.Select(name => Type.GetType(name, throwOnError: true)!).ToArray());
            }

            // Gets target method parameters
            var targetMethodParameters = targetMethod.GetParameters();
            var targetMethodParametersTypes = targetMethodParameters.Select(p => p.ParameterType).ToArray();

            // Make sure we have the right methods attributes.
            var proxyMethodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig;

            // Create the proxy method implementation
            var proxyMethod = proxyTypeBuilder?.DefineMethod(proxyMethodDefinition.Name, proxyMethodAttributes, proxyMethodDefinition.ReturnType, proxyMethodDefinitionParametersTypes);
            var il = MethodIlHelper.InitialiseProxyMethod(proxyMethod, proxyMethodDefinitionParameters, proxyMethodDefinitionGenericArgumentsNames, targetMethod, instanceField);

            // Load all the arguments / parameters
            var outputAndRefParameters = MethodIlHelper.AddIlToLoadArguments(
                proxyTypeBuilder,
                il,
                innerMethod: targetMethod,
                innerMethodParameters: targetMethodParameters,
                innerMethodParametersTypes: targetMethodParametersTypes,
                outerMethod: proxyMethodDefinition,
                outerMethodParameters: proxyMethodDefinitionParameters,
                outerMethodGenericArguments: proxyMethodDefinitionGenericArguments,
                duckCastParameterFunc: MethodIlHelper.AddIlToExtractDuckType,
                needsDuckChaining: NeedsDuckChaining);

            // Call the target method
            var returnType = targetMethod.ReturnType;
            if (UseDirectAccessTo(proxyTypeBuilder, targetType))
            {
                // If the instance is public we can emit directly without any dynamic method

                targetMethod = MethodIlHelper.AddIlForDirectMethodCall(il, targetMethod, proxyMethodDefinitionGenericArguments);
            }
            else
            {
                // A generic method call can't be made from a DynamicMethod
                if (proxyMethodDefinitionGenericArguments.Length > 0)
                {
                    DuckTypeProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesException.Throw(proxyMethodDefinition);
                }

                returnType = MethodIlHelper.AddIlForDynamicMethodCall(proxyTypeBuilder, il, targetMethod, targetMethodParametersTypes);
            }

            // We check if we have output or ref parameters to set in the proxy method
            if (outputAndRefParameters is not null)
            {
                MethodIlHelper.AddIlToSetOutputAndRefParameters(il, outputAndRefParameters, MethodIlHelper.AddIlToDuckChain, NeedsDuckChaining);
            }

            if (!MethodIlHelper.TryAddReturnIl(
                    proxyTypeBuilder,
                    il,
                    currentReturnType: returnType,
                    innerMethodReturnType: targetMethod.ReturnType,
                    outerMethodReturnType: proxyMethodDefinition.ReturnType,
                    needsDuckChainingFunc: NeedsDuckChaining,
                    addDuckChainIlFunc: MethodIlHelper.AddIlToDuckChain))
            {
                DuckTypeProxyAndTargetMethodReturnTypeMismatchException.Throw(proxyMethodDefinition, targetMethod);
            }

            if (proxyMethod is not null)
            {
                MethodBuilderGetToken.Invoke(proxyMethod, null);
            }
        }
    }

    private static void CreateReverseProxyMethods(TypeBuilder? proxyTypeBuilder, Type typeToDeriveFrom, Type typeToDelegateTo, FieldInfo? instanceField)
    {
        // Gets all methods that _can_ be overriden/implemented
        var overriddenMethods = GetMethods(typeToDeriveFrom);

        // Get all the methods on our delegation type that we're going to delegate to
        // Note that these don't need to be abstract/virtual, unlike in a normal (forward) proxy
        var implementationMethods = new List<MethodInfo>(typeToDelegateTo.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));

        foreach (var immutableImplementationMethod in implementationMethods)
        {
            var implementationMethod = immutableImplementationMethod;
            // Ignore methods without a `DuckReverse` attribute
            if (implementationMethod.GetCustomAttribute<DuckReverseMethodAttribute>(true) is null)
            {
                continue;
            }

            // Extract the method parameters types
            var implementationMethodParameters = implementationMethod.GetParameters();
            var implementationMethodParametersTypes = implementationMethodParameters.Select(p => p.ParameterType).ToArray();

            // We select the target method to call
            var overriddenMethod = SelectTargetMethod<DuckReverseMethodAttribute>(typeToDeriveFrom, implementationMethod, implementationMethodParameters, implementationMethodParametersTypes, overriddenMethods);

            // If the target method couldn't be found we throw.
            if (overriddenMethod is null)
            {
                DuckTypeTargetMethodNotFoundException.Throw(implementationMethod);
                continue;
            }

            overriddenMethods.Remove(overriddenMethod);

            // Gets the proxy method definition generic arguments
            var overriddenMethodGenericArguments = overriddenMethod.GetGenericArguments();
            var implementationDefinitionGenericArguments = implementationMethod.GetGenericArguments();
            var implementationDefinitionGenericArgumentsNames = implementationDefinitionGenericArguments.Select(a => a.Name).ToArray();

            // Reverse duck typing doesn't support providing a non-generic implementation for a generic method
            if (overriddenMethodGenericArguments.Length > 0
                && implementationDefinitionGenericArguments.Length != overriddenMethodGenericArguments.Length)
            {
                DuckTypeReverseProxyMustImplementGenericMethodAsGenericException.Throw(implementationMethod, overriddenMethod);
                continue;
            }

            // Gets target method parameters
            var overriddenMethodParameters = overriddenMethod.GetParameters();
            if (implementationMethodParameters.Length > overriddenMethodParameters.Length)
            {
                DuckTypeProxyAndTargetMethodParameterSignatureMismatchException.Throw(implementationMethod, overriddenMethod);
            }

            var overriddenMethodParametersTypes = overriddenMethodParameters.Select(p => p.ParameterType).ToArray();

            // Make sure we have the right methods attributes.
            var proxyMethodAttributes = overriddenMethod.IsFamily
                                                            ? MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig
                                                            : MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig;

            // Create the proxy method implementation
            var proxyMethod = proxyTypeBuilder?.DefineMethod(overriddenMethod.Name, proxyMethodAttributes, overriddenMethod.ReturnType, overriddenMethodParametersTypes);
            var il = MethodIlHelper.InitialiseProxyMethod(proxyMethod, overriddenMethodParameters, implementationDefinitionGenericArgumentsNames, implementationMethod, instanceField);

            // Load all the arguments / parameters
            var outputAndRefParameters = MethodIlHelper.AddIlToLoadArguments(
                proxyTypeBuilder,
                il,
                innerMethod: implementationMethod,
                innerMethodParameters: implementationMethodParameters,
                innerMethodParametersTypes: implementationMethodParametersTypes,
                outerMethod: overriddenMethod,
                outerMethodParameters: overriddenMethodParameters,
                outerMethodGenericArguments: implementationDefinitionGenericArguments,
                duckCastParameterFunc: MethodIlHelper.AddIlToDuckChain,
                needsDuckChaining: MethodIlHelper.NeedsDuckChainingReverse);

            // Call the target method
            // We know we have direct access to the target method because we defined it in our proxy

            // If the instance is public we can emit directly without any dynamic method
            implementationMethod = MethodIlHelper.AddIlForDirectMethodCall(il, implementationMethod, overriddenMethodGenericArguments);

            // We check if we have output or ref parameters to set in the proxy method
            if (outputAndRefParameters is not null)
            {
                MethodIlHelper.AddIlToSetOutputAndRefParameters(il, outputAndRefParameters, MethodIlHelper.AddIlToExtractDuckType, MethodIlHelper.NeedsDuckChainingReverse);
            }

            // We always do a direct method call, so return type is always the implementation method's return type
            var returnType = implementationMethod.ReturnType;
            if (!MethodIlHelper.TryAddReturnIl(
                    proxyTypeBuilder,
                    il,
                    currentReturnType: returnType,
                    innerMethodReturnType: implementationMethod.ReturnType,
                    outerMethodReturnType: overriddenMethod.ReturnType,
                    needsDuckChainingFunc: MethodIlHelper.NeedsDuckChainingReverse,
                    addDuckChainIlFunc: MethodIlHelper.AddIlToDuckChainReverse))
            {
                DuckTypeProxyAndTargetMethodReturnTypeMismatchException.Throw(implementationMethod, overriddenMethod);
            }

            if (proxyMethod is not null)
            {
                MethodBuilderGetToken.Invoke(proxyMethod, null);
            }
        }

        if (overriddenMethods.Any(x => x.IsAbstract))
        {
            DuckTypeReverseProxyMissingMethodImplementationException.Throw(overriddenMethods.Where(x => x.IsAbstract));
        }
    }

    private static MethodInfo? SelectTargetMethod<T>(
        Type targetType,
        MethodInfo proxyMethod,
        ParameterInfo[] proxyMethodParameters,
        Type[] proxyMethodParametersTypes,
        IEnumerable<MethodInfo> allTargetMethods)
        where T : DuckAttributeBase, new()
    {
        var proxyMethodDuckAttribute = proxyMethod.GetCustomAttribute<T>(true) ?? new T();
        proxyMethodDuckAttribute.Name ??= proxyMethod.Name;

        MethodInfo? targetMethod;

        // Check if the duck attribute has the parameter type names to use for selecting the target method
        // If any of the parameter types can't be loaded (happens if it's a generic parameter for example)
        // then carry on searching.
        var proxyMethodDuckAttributeParameterTypeNames = proxyMethodDuckAttribute.ParameterTypeNames;
        if (proxyMethodDuckAttributeParameterTypeNames is not null)
        {
            // Duck reverse attributes must never have a mismatch between the number of proxy method parameters
            // and the number of parameters specified in the [DuckReverseMethod] attribute
            if (typeof(T) == typeof(DuckReverseMethodAttribute)
                    && (proxyMethodParameters.Length != proxyMethodDuckAttributeParameterTypeNames.Length))
            {
                DuckTypeReverseAttributeParameterNamesMismatchException.Throw(proxyMethod);
            }

            Type[] parameterTypes = proxyMethodDuckAttributeParameterTypeNames
                                                            .Select(pName => Type.GetType(pName, throwOnError: false))
                                                            .Where(type => type is not null)
                                                            .ToArray()!;
            if (parameterTypes.Length == proxyMethodDuckAttributeParameterTypeNames.Length)
            {
                // all types were loaded
                targetMethod = targetType.GetMethod(proxyMethodDuckAttribute.Name, proxyMethodDuckAttribute.BindingFlags, null, parameterTypes, null);
                if (targetMethod is not null)
                {
                    return targetMethod;
                }
            }
        }

        // If the duck attribute doesn't specify the parameters to use, we do the best effor to find a target method without any ambiguity.

        // First we try with the current proxy parameter types
        targetMethod = targetType.GetMethod(proxyMethodDuckAttribute.Name, proxyMethodDuckAttribute.BindingFlags, null, proxyMethodParametersTypes, null);
        if (targetMethod is not null)
        {
            return targetMethod;
        }

        // If the method wasn't found could be because a DuckType interface is being use in the parameters or in the return value.
        // Also this can happen if the proxy parameters type uses a base object (ex: System.Object) instead the type.
        // In this case we try to find a method that we can match, in case of ambiguity (> 1 method found) we throw an exception.

        foreach (var candidateMethod in allTargetMethods)
        {
            var name = proxyMethodDuckAttribute.Name;
            var useRelaxedNameComparison = false;

            // If there is an explicit interface type name we add it to the name
            if (!string.IsNullOrEmpty(proxyMethodDuckAttribute.ExplicitInterfaceTypeName))
            {
                var interfaceTypeName = proxyMethodDuckAttribute.ExplicitInterfaceTypeName!;

                if (interfaceTypeName == "*")
                {
                    // If a wildcard is use, then we relax the name comparison so it can be an implicit or explicity implementation
                    useRelaxedNameComparison = true;
                }
                else
                {
                    // Nested types are separated with a "." on explicit implementation.
#if NET
                    interfaceTypeName = interfaceTypeName.Replace("+", ".", StringComparison.Ordinal);
#else
                    interfaceTypeName = interfaceTypeName.Replace("+", ".");
#endif

                    name = interfaceTypeName + "." + name;
                }
            }

            // We omit target methods with different names.
            if (candidateMethod.Name != name)
            {
                if (!useRelaxedNameComparison || !candidateMethod.Name.EndsWith("." + name, StringComparison.Ordinal))
                {
                    continue;
                }
            }

            // Check if the candidate method is a reverse mapped method
            var candidateParameters = candidateMethod.GetParameters();
            if (proxyMethodDuckAttributeParameterTypeNames is not null)
            {
                var arguments = proxyMethodDuckAttributeParameterTypeNames;
                if (arguments.Length != candidateParameters.Length)
                {
                    continue;
                }

                var match = true;
                for (var i = 0; i < arguments.Length; i++)
                {
                    var candidateParameter = candidateParameters[i].ParameterType;
                    if (arguments[i] != candidateParameter.FullName &&
                        arguments[i] != candidateParameter.Name &&
                        arguments[i] != $"{candidateParameter.FullName}, {candidateParameter.Assembly.GetName().Name}")
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return candidateMethod;
                }
            }

            // The proxy must have the same or less parameters than the candidate ( less is due to possible optional parameters in the candidate ).
            if (proxyMethodParameters.Length > candidateParameters.Length)
            {
                continue;
            }

            // We compare the target method candidate parameter by parameter.
            var skip = false;
            for (var i = 0; i < proxyMethodParametersTypes.Length; i++)
            {
                var proxyParam = proxyMethodParameters[i];
                var candidateParam = candidateParameters[i];

                var proxyParamType = proxyParam.ParameterType;
                var candidateParamType = candidateParam.ParameterType;

                // both needs to have the same parameter direction
                if (proxyParam.IsOut != candidateParam.IsOut)
                {
                    skip = true;
                    break;
                }

                // Both need to have the same element type or byref type signature.
                if (proxyParamType.IsByRef != candidateParamType.IsByRef)
                {
                    skip = true;
                    break;
                }

                // If the parameters are by ref we unwrap them to have the actual type
                proxyParamType = proxyParamType.IsByRef ? proxyParamType.GetElementType()! : proxyParamType;
                candidateParamType = candidateParamType.IsByRef ? candidateParamType.GetElementType()! : candidateParamType;

                // We can't compare generic parameters
                if (candidateParamType.IsGenericParameter)
                {
                    continue;
                }

                // If the proxy parameter type is a value type (no ducktyping neither a base class) both types must match
                if (proxyParamType.IsValueType && !proxyParamType.IsEnum && proxyParamType != candidateParamType)
                {
                    skip = true;
                    break;
                }

                // If the proxy parameter is a class and not is an abstract class (only interface and abstract class can be used as ducktype base type)
                if (proxyParamType.IsClass && !proxyParamType.IsAbstract && proxyParamType != typeof(object))
                {
                    if (!candidateParamType.IsAssignableFrom(proxyParamType))
                    {
                        // Check if the parameter type contains generic types before skipping
                        if (!candidateParamType.IsGenericType || !proxyParamType.IsGenericType)
                        {
                            skip = true;
                            break;
                        }

                        // if the string representation of the generic parameter types is not the same we need to analyze the
                        // GenericTypeArguments array before skipping it
                        if (candidateParamType.ToString() != proxyParamType.ToString())
                        {
                            if (candidateParamType.GenericTypeArguments.Length != proxyParamType.GenericTypeArguments.Length)
                            {
                                skip = true;
                                break;
                            }

                            for (var paramIndex = 0; paramIndex < candidateParamType.GenericTypeArguments.Length; paramIndex++)
                            {
                                var candidateParamTypeGenericType = candidateParamType.GenericTypeArguments[paramIndex];
                                var proxyParamTypeGenericType = proxyParamType.GenericTypeArguments[paramIndex];

                                // Both need to have the same element type or byref type signature.
                                if (proxyParamTypeGenericType.IsByRef != candidateParamTypeGenericType.IsByRef)
                                {
                                    skip = true;
                                    break;
                                }

                                // If the parameters are by ref we unwrap them to have the actual type
                                proxyParamTypeGenericType = proxyParamTypeGenericType.IsByRef ? proxyParamTypeGenericType.GetElementType()! : proxyParamTypeGenericType;
                                candidateParamTypeGenericType = candidateParamTypeGenericType.IsByRef ? candidateParamTypeGenericType.GetElementType()! : candidateParamTypeGenericType;

                                // We can't compare generic parameters
                                if (candidateParamTypeGenericType.IsGenericParameter)
                                {
                                    continue;
                                }

                                // If the proxy parameter type is a value type (no ducktyping neither a base class) both types must match
                                if (proxyParamTypeGenericType.IsValueType && !proxyParamTypeGenericType.IsEnum && proxyParamTypeGenericType != candidateParamTypeGenericType)
                                {
                                    skip = true;
                                    break;
                                }

                                // If the proxy parameter is a class and not is an abstract class (only interface and abstract class can be used as ducktype base type)
                                if (proxyParamTypeGenericType.IsClass && !proxyParamTypeGenericType.IsAbstract && proxyParamTypeGenericType != typeof(object))
                                {
                                    if (!candidateParamTypeGenericType.IsAssignableFrom(proxyParamTypeGenericType))
                                    {
                                        skip = true;
                                        break;
                                    }
                                }
                            }

                            if (skip)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            if (skip)
            {
                continue;
            }

            // The target method may have optional parameters with default values so we have to skip those
            for (var i = proxyMethodParametersTypes.Length; i < candidateParameters.Length; i++)
            {
                if (!candidateParameters[i].IsOptional)
                {
                    skip = true;
                    break;
                }
            }

            if (skip)
            {
                continue;
            }

            if (targetMethod is null)
            {
                targetMethod = candidateMethod;
            }
            else
            {
                DuckTypeTargetMethodAmbiguousMatchException.Throw(proxyMethod, targetMethod, candidateMethod);
            }
        }

        return targetMethod;
    }

    private static void WriteSafeTypeConversion(this LazyILGenerator il, Type actualType, Type expectedType)
    {
        // If both types are generics, we expect that the generic parameter are the same type (passthrough)
        if (actualType.IsGenericParameter && expectedType.IsGenericParameter)
        {
            return;
        }

        il.WriteTypeConversion(actualType, expectedType);
    }

    private readonly struct OutputAndRefParameterData
    {
        public readonly Type LocalType;
        public readonly Type ProxyArgumentType;
        public readonly int LocalIndex;
        public readonly int ProxyArgumentIndex;

        public OutputAndRefParameterData(int localIndex, Type localType, int proxyArgumentIndex, Type proxyArgumentType)
        {
            LocalIndex = localIndex;
            LocalType = localType;
            ProxyArgumentIndex = proxyArgumentIndex;
            ProxyArgumentType = proxyArgumentType;
        }
    }

    private static class MethodIlHelper
    {
        internal static LazyILGenerator InitialiseProxyMethod(
            MethodBuilder? proxyMethod,
            ParameterInfo[] proxyMethodDefinitionParameters,
            string[] proxyMethodDefinitionGenericArgumentsNames,
            MethodInfo targetMethod,
            FieldInfo? instanceField)
        {
            if (proxyMethod is null)
            {
                return new LazyILGenerator(null);
            }

            var proxyMethodParametersBuilders = new ParameterBuilder[proxyMethodDefinitionParameters.Length];
            if (proxyMethodDefinitionGenericArgumentsNames.Length > 0)
            {
                _ = proxyMethod.DefineGenericParameters(proxyMethodDefinitionGenericArgumentsNames);
            }

            // Define the proxy method implementation parameters for optional parameters with default values
            for (var j = 0; j < proxyMethodDefinitionParameters.Length; j++)
            {
                var pmDefParameter = proxyMethodDefinitionParameters[j];
                var pmImpParameter = proxyMethod.DefineParameter(j, pmDefParameter.Attributes, pmDefParameter.Name);
                if (pmDefParameter.HasDefaultValue)
                {
                    pmImpParameter.SetConstant(pmDefParameter.RawDefaultValue);
                }

                proxyMethodParametersBuilders[j] = pmImpParameter;
            }

            var il = new LazyILGenerator(proxyMethod.GetILGenerator());

            // Load the instance if needed
            if (!targetMethod.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (instanceField is not null)
                {
                    il.Emit(instanceField.FieldType.IsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, instanceField);
                }
            }

            return il;
        }

        internal static List<OutputAndRefParameterData>? AddIlToLoadArguments(
            TypeBuilder? proxyTypeBuilder,
            LazyILGenerator il,
            MethodInfo innerMethod,
            ParameterInfo[] innerMethodParameters,
            Type[] innerMethodParametersTypes,
            MethodInfo outerMethod,
            ParameterInfo[] outerMethodParameters,
            Type[] outerMethodGenericArguments,
            Func<LazyILGenerator, Type, Type, Type> duckCastParameterFunc,
            Func<Type, Type, bool> needsDuckChaining)
        {
            List<OutputAndRefParameterData>? outputAndRefParameters = null;
            var maxParamLength = Math.Max(outerMethodParameters.Length, innerMethodParameters.Length);

            for (var idx = 0; idx < maxParamLength; idx++)
            {
                var outerParamInfo = idx < outerMethodParameters.Length ? outerMethodParameters[idx] : null;
                var innerParamInfo = innerMethodParameters[idx];

                if (outerParamInfo is null)
                {
                    // The outer (proxy) method is missing parameters, we check if the target parameter is optional
                    // This will not occur for reverse proxies, where the parameter count must match
                    if (!innerParamInfo.IsOptional)
                    {
                        // The target method parameter is not optional.
                        DuckTypeProxyMethodParameterIsMissingException.Throw(outerMethod, innerParamInfo);
                    }
                }
                else
                {
                    if (outerParamInfo.IsOut != innerParamInfo.IsOut || outerParamInfo.IsIn != innerParamInfo.IsIn)
                    {
                        // the proxy and target parameters doesn't have the same signature
                        DuckTypeProxyAndTargetMethodParameterSignatureMismatchException.Throw(outerMethod, innerMethod);
                    }

                    var outerParamType = outerParamInfo.ParameterType;
                    var innerParamType = innerParamInfo.ParameterType;

                    if (outerParamType.IsByRef != innerParamType.IsByRef)
                    {
                        // the proxy and target parameters doesn't have the same signature
                        DuckTypeProxyAndTargetMethodParameterSignatureMismatchException.Throw(outerMethod, innerMethod);
                    }

                    if (outerParamType.IsGenericParameter != innerParamType.IsGenericParameter
                        && outerMethodGenericArguments.Length > 0)
                    {
                        // We're in a generic proxy method (i.e. we haven't created a specialized version)
                        // of a generic target, but we _don't_ have a generic parameter where the original does
                        DuckTypeProxyAndTargetMethodParameterSignatureMismatchException.Throw(outerMethod, innerMethod);
                    }

                    // We check if we have to handle an output parameter, by ref parameter or a normal parameter
                    if (outerParamInfo.IsOut)
                    {
                        // If is an output parameter with different types we need to handle differently
                        // by creating a local var first to store the target parameter out value
                        // and then try to set the output parameter of the proxy method by converting the value (a base class or a duck typing)
                        if (outerParamType != innerParamType)
                        {
                            var localTargetArg = il.DeclareLocal(innerParamType.GetElementType() ?? innerParamType);
                            var localIndex = localTargetArg?.LocalIndex ?? 0;

                            // We need to store the output parameter data to set the proxy parameter value after we call the target method
                            outputAndRefParameters ??= [];
                            outputAndRefParameters.Add(new OutputAndRefParameterData(localIndex, innerParamType, idx, outerParamType));

                            // Load the local var ref (to be used in the target method param as output)
                            il.Emit(OpCodes.Ldloca_S, localIndex);
                        }
                        else
                        {
                            il.WriteLoadArgument(idx, false);
                        }
                    }
                    else if (outerParamType.IsByRef)
                    {
                        // If is a ref parameter with different types we need to handle differently
                        // by creating a local var first to store the initial proxy parameter ref value casted to the target parameter type ( this cast may fail at runtime )
                        // later pass this local var ref to the target method, and then, modify the proxy parameter ref with the new reference from the target method
                        // by converting the value (a base class or a duck typing)
                        if (outerParamType != innerParamType)
                        {
                            var outerParamTypeElementType = outerParamType.GetElementType() ?? outerParamType;
                            var innerParamTypeElementType = innerParamType.GetElementType() ?? innerParamType;

                            if (!UseDirectAccessTo(proxyTypeBuilder, innerParamTypeElementType))
                            {
                                innerParamType = typeof(object).MakeByRefType();
                                innerParamTypeElementType = typeof(object);
                            }

                            var localTargetArg = il.DeclareLocal(innerParamTypeElementType);
                            var localIndex = localTargetArg?.LocalIndex ?? 0;

                            // We need to store the ref parameter data to set the proxy parameter value after we call the target method
                            outputAndRefParameters ??= [];
                            outputAndRefParameters.Add(new OutputAndRefParameterData(localIndex, innerParamType, idx, outerParamType));

                            // Load the argument (ref)
                            il.WriteLoadArgument(idx, false);

                            // Load the value inside the ref
                            il.Emit(OpCodes.Ldind_Ref);

                            // Check if the type can be converted of if we need to enable duck chaining
                            if (needsDuckChaining(innerParamTypeElementType, outerParamTypeElementType))
                            {
                                // First we check if the value is null before trying to get the instance value
                                var lblCallGetInstance = il.DefineLabel();
                                var lblAfterGetInstance = il.DefineLabel();

                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Brtrue_S, lblCallGetInstance);

                                il.Emit(OpCodes.Pop);
                                il.Emit(OpCodes.Ldnull);
                                il.Emit(OpCodes.Br_S, lblAfterGetInstance);

                                il.MarkLabel(lblCallGetInstance);

                                // If this is a forward duck type, we need to cast to IDuckType and extract the original instance
                                // If this is a reverse duck type, we need to create a duck type from the original instance
                                duckCastParameterFunc(il, innerParamTypeElementType, outerParamTypeElementType);

                                il.MarkLabel(lblAfterGetInstance);
                            }

                            // Cast the value to the target type
                            il.WriteSafeTypeConversion(outerParamTypeElementType, innerParamTypeElementType);

                            // Store the casted value to the local var
                            il.WriteStoreLocal(localIndex);

                            // Load the local var ref (to be used in the target method param)
                            il.Emit(OpCodes.Ldloca_S, localIndex);
                        }
                        else
                        {
                            il.WriteLoadArgument(idx, false);
                        }
                    }
                    else
                    {
                        // Check if the type can be converted of if we need to enable duck chaining
                        if (needsDuckChaining(innerParamType, outerParamType))
                        {
                            // Load the argument
                            il.WriteLoadArgument(idx, false);

                            // If this is a forward duck type, we need to cast to IDuckType and extract the original instance
                            // If this is a reverse duck type, we need to create a duck type from the original instance
                            duckCastParameterFunc(il, innerParamType, outerParamType);
                        }
                        else
                        {
                            il.WriteLoadArgument(idx, false);
                        }

                        // If the target parameter type is public or if it's by ref we have to actually use the original target type.
                        innerParamType = UseDirectAccessTo(proxyTypeBuilder, innerParamType) ? innerParamType : typeof(object);
                        il.WriteSafeTypeConversion(outerParamType, innerParamType);

                        innerMethodParametersTypes[idx] = innerParamType;
                    }
                }
            }

            return outputAndRefParameters;
        }

        internal static MethodInfo AddIlForDirectMethodCall(
            LazyILGenerator il,
            MethodInfo targetMethod,
            Type[] proxyMethodDefinitionGenericArguments)
        {
            // Create generic method call
            if (proxyMethodDefinitionGenericArguments.Length > 0)
            {
                targetMethod = targetMethod.MakeGenericMethod(proxyMethodDefinitionGenericArguments);
            }

            // Method call
            // A generic method cannot be called using calli (throws System.InvalidOperationException)
            if (targetMethod.IsPublic || targetMethod.IsGenericMethod)
            {
                // We can emit a normal call if we have a public instance with a public target method.
                il.EmitCall(targetMethod.IsStatic || (targetMethod.DeclaringType?.IsValueType ?? false) ? OpCodes.Call : OpCodes.Callvirt, targetMethod, null!);
            }
            else
            {
                // In case we have a public instance and a non public target method we can use [Calli] with the function pointer
                il.WriteMethodCalli(targetMethod);
            }

            return targetMethod;
        }

        internal static Type AddIlForDynamicMethodCall(
            TypeBuilder? proxyTypeBuilder,
            LazyILGenerator il,
            MethodInfo targetMethod,
            Type[] targetMethodParametersTypes)
        {
            // If the instance is not public we need to create a Dynamic method to overpass the visibility checks
            // we can't access non public types so we have to cast to object type (in the instance object and the return type).

            var dynMethodName = $"_callMethod_{targetMethod.DeclaringType?.Name}_{targetMethod.Name}";
            var returnType = UseDirectAccessTo(proxyTypeBuilder, targetMethod.ReturnType) && !targetMethod.ReturnType.IsGenericParameter ? targetMethod.ReturnType : typeof(object);

            if (proxyTypeBuilder is null)
            {
                return returnType;
            }

            // We create the dynamic method
            var originalTargetParameters = targetMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            var targetParameters = targetMethod.IsStatic ? originalTargetParameters : (new[] { typeof(object) }).Concat(originalTargetParameters).ToArray();
            var dynParameters = targetMethod.IsStatic ? targetMethodParametersTypes : (new[] { typeof(object) }).Concat(targetMethodParametersTypes).ToArray();
            var dynMethod = new DynamicMethod(dynMethodName, returnType, dynParameters, proxyTypeBuilder.Module, true);

            // Emit the dynamic method body
            var dynIL = new LazyILGenerator(dynMethod.GetILGenerator());

            if (!targetMethod.IsStatic && targetMethod.DeclaringType is not null)
            {
                dynIL.LoadInstanceArgument(typeof(object), targetMethod.DeclaringType);
            }

            for (var idx = targetMethod.IsStatic ? 0 : 1; idx < dynParameters.Length; idx++)
            {
                dynIL.WriteLoadArgument(idx, true);
                dynIL.WriteSafeTypeConversion(dynParameters[idx], targetParameters[idx]);
            }

            // Check if we can emit a normal Call/CallVirt to the target method
            if (!targetMethod.ContainsGenericParameters)
            {
                dynIL.EmitCall(targetMethod.IsStatic || (targetMethod.DeclaringType?.IsValueType ?? false) ? OpCodes.Call : OpCodes.Callvirt, targetMethod, null!);
            }
            else
            {
                // We can't emit a call to a method with generics from a DynamicMethod
                // Instead we emit a Calli with the function pointer.
                dynIL.WriteMethodCalli(targetMethod);
            }

            dynIL.WriteSafeTypeConversion(targetMethod.ReturnType, returnType);
            dynIL.Emit(OpCodes.Ret);
            dynIL.Flush();

            // Emit the call to the dynamic method
            il.WriteDynamicMethodCall(dynMethod, proxyTypeBuilder);

            return returnType;
        }

        internal static void AddIlToSetOutputAndRefParameters(
            LazyILGenerator il,
            List<OutputAndRefParameterData> outputAndRefParameters,
            Func<LazyILGenerator, Type, Type, Type> duckChainFunc,
            Func<Type, Type, bool> needsDuckChaining)
        {
            foreach (var outOrRefParameter in outputAndRefParameters)
            {
                var proxyArgumentType = outOrRefParameter.ProxyArgumentType.GetElementType() ?? outOrRefParameter.ProxyArgumentType;
                var localType = outOrRefParameter.LocalType.GetElementType() ?? outOrRefParameter.LocalType;

                // We load the argument to be set
                il.WriteLoadArgument(outOrRefParameter.ProxyArgumentIndex, false);

                // We load the value from the local
                il.WriteLoadLocal(outOrRefParameter.LocalIndex);

                // If we detect duck chaining we create a new proxy instance with the output of the original target method
                if (needsDuckChaining(localType, proxyArgumentType))
                {
                    duckChainFunc(il, proxyArgumentType, localType);
                }
                else
                {
                    il.WriteSafeTypeConversion(localType, proxyArgumentType);
                }

                // We store the value
                il.Emit(OpCodes.Stind_Ref);
            }
        }

        internal static bool TryAddReturnIl(
            TypeBuilder? proxyTypeBuilder,
            LazyILGenerator il,
            Type currentReturnType,
            Type innerMethodReturnType,
            Type outerMethodReturnType,
            Func<Type, Type, bool> needsDuckChainingFunc,
            Func<LazyILGenerator, Type, Type, Type> addDuckChainIlFunc)
        {
            // Check if the target method returns something

            if ((innerMethodReturnType == typeof(void) && outerMethodReturnType != typeof(void))
                || (innerMethodReturnType != typeof(void) && outerMethodReturnType == typeof(void)))
            {
                // ERROR
                return false;
            }
            else if (innerMethodReturnType != typeof(void))
            {
                // Handle the return value
                // Check if the type can be converted or if we need to enable duck chaining
                if (needsDuckChainingFunc(innerMethodReturnType, outerMethodReturnType))
                {
                    UseDirectAccessTo(proxyTypeBuilder, innerMethodReturnType);

                    // We call DuckType.CreateCache<>.Create() or DuckType.CreateCache<>.CreateReverse()
                    addDuckChainIlFunc(il, outerMethodReturnType, innerMethodReturnType);
                }
                else if (currentReturnType != outerMethodReturnType)
                {
                    // If the type is not the expected type we try a conversion.
                    il.WriteSafeTypeConversion(currentReturnType, outerMethodReturnType);
                }
            }

            il.Emit(OpCodes.Ret);
            il.Flush();
            return true;
        }

        internal static bool NeedsDuckChainingReverse(Type targetType, Type proxyType)
            => NeedsDuckChaining(targetType: proxyType, proxyType: targetType);

        internal static Type AddIlToDuckChain(LazyILGenerator il, Type genericType, Type fromType)
        {
            MethodInfo? getProxyMethodInfo;
            if (fromType.IsValueType)
            {
                getProxyMethodInfo = typeof(CreateCache<>)
                                    .MakeGenericType(genericType)
                                    .GetMethod("CreateFrom")?
                                    .MakeGenericMethod(fromType);

                if (getProxyMethodInfo is null)
                {
                    DuckTypeException.Throw($"CreateCache<{genericType}>.CreateFrom<{fromType}>() cannot be found!");
                }
            }
            else
            {
                getProxyMethodInfo = typeof(CreateCache<>)
                                    .MakeGenericType(genericType)
                                    .GetMethod("Create");

                if (getProxyMethodInfo is null)
                {
                    DuckTypeException.Throw($"CreateCache<{genericType}>.Create() cannot be found!");
                }
            }

            il.Emit(OpCodes.Call, getProxyMethodInfo);
            return genericType;
        }

        internal static Type AddIlToDuckChainReverse(LazyILGenerator il, Type genericType, Type originalType)
        {
            var getProxyMethodInfo = typeof(CreateCache<>)
                                    .MakeGenericType(genericType)
                                    .GetMethod("CreateReverse");

            if (getProxyMethodInfo is null)
            {
                DuckTypeException.Throw($"CreateCache<{genericType}>.CreateReverse() cannot be found!");
            }

            il.Emit(OpCodes.Call, getProxyMethodInfo);
            return genericType;
        }

        internal static Type AddIlToExtractDuckType(LazyILGenerator il, Type toType, Type fromType)
        {
            // outer is a duck type, so extract it
            // Call IDuckType.Instance property to get the actual value
            il.Emit(OpCodes.Castclass, typeof(IDuckType));
            il.EmitCall(OpCodes.Callvirt, DuckTypeInstancePropertyInfo.GetMethod!, null!);
            return typeof(object);
        }
    }
}
