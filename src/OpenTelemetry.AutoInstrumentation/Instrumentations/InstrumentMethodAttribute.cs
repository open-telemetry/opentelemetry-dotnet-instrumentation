// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations;

/// <summary>
/// Attribute that indicates that the decorated class is meant to intercept a method
/// by modifying the method body with callbacks. Used to generate the integration definitions file.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
internal sealed class InstrumentMethodAttribute : Attribute
{
    public InstrumentMethodAttribute(string assemblyName, string typeName, string methodName, string returnTypeName, string[] parameterTypeNames, string minimumVersion, string maximumVersion, string integrationName, InstrumentationType type, IntegrationKind integrationKind = IntegrationKind.Direct)
    {
        AssemblyName = assemblyName;
        TypeName = typeName;
        MethodName = methodName;
        ReturnTypeName = returnTypeName;
        ParameterTypeNames = parameterTypeNames;
        VersionRange = new IntegrationVersionRange
        {
            MinimumVersion = minimumVersion,
            MaximumVersion = maximumVersion
        };
        IntegrationName = integrationName;
        Type = type;
        Kind = integrationKind;
    }

    /// <summary>
    /// Gets the name of the assembly that contains the target method to be intercepted.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// Gets the name of the type that contains the target method to be intercepted.
    /// Required.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Gets the name of the target method to be intercepted.
    /// If null, default to the name of the decorated method.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets the return type name
    /// </summary>
    public string ReturnTypeName { get; }

    /// <summary>
    /// Gets the parameters type array for the target method to be intercepted.
    /// </summary>
    public string[] ParameterTypeNames { get; }

    /// <summary>
    /// Gets the target version range for <see cref="AssemblyName"/>.
    /// </summary>
    public IntegrationVersionRange VersionRange { get; }

    /// <summary>
    /// Gets the target minimum version.
    /// </summary>
    public string MinimumVersion
    {
        get => VersionRange.MinimumVersion;
    }

    /// <summary>
    /// Gets the target maximum version.
    /// </summary>
    public string MaximumVersion
    {
        get => VersionRange.MaximumVersion;
    }

    /// <summary>
    /// Gets or sets the integration name. Allows to group several integration with a single integration name.
    /// </summary>
    public string IntegrationName { get; set; }

    /// <summary>
    /// Gets or sets the integration type.
    /// </summary>
    public InstrumentationType Type { get; set; }

    /// <summary>
    /// Gets or sets the integration kind.
    /// </summary>
    public IntegrationKind Kind { get; set; }
}
