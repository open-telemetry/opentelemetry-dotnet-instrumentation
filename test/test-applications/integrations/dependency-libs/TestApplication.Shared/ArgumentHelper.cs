// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.Shared;

internal static class ArgumentHelper
{
    public static bool HasArgument(string[] args, string name)
    {
#if NET
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(name);
#else
        if (args == null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }
#endif

        return args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetArgument(string[] args, string name, string defaultValue)
    {
#if NET
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(defaultValue);
#else
        if (args == null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (defaultValue == null)
        {
            throw new ArgumentNullException(nameof(defaultValue));
        }
#endif

        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return defaultValue;
    }

    public static string GetRequiredArgument(string[] args, string name)
    {
#if NET
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(name);
#else
        if (args == null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }
#endif

        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        throw new NotSupportedException($"Lack of required argument '{name}' for the test application.");
    }
}
