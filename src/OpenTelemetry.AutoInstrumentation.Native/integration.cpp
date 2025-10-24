// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "integration.h"

#include <sstream>
#include <unordered_map>

#include "logger.h"
#include "regex_utils.h"
#include "util.h"

namespace trace
{

std::mutex                                                      m_assemblyReferenceCacheMutex;
std::unordered_map<WSTRING, std::unique_ptr<AssemblyReference>> m_assemblyReferenceCache;

AssemblyReference::AssemblyReference(const WSTRING& str)
    : name(GetNameFromAssemblyReferenceString(str))
    , version(GetVersionFromAssemblyReferenceString(str))
    , locale(GetLocaleFromAssemblyReferenceString(str))
    , public_key(GetPublicKeyFromAssemblyReferenceString(str))
{
}

AssemblyReference* AssemblyReference::GetFromCache(const WSTRING& str)
{
    std::lock_guard<std::mutex> guard(m_assemblyReferenceCacheMutex);
    auto                        findRes = m_assemblyReferenceCache.find(str);
    if (findRes != m_assemblyReferenceCache.end())
    {
        return findRes->second.get();
    }
    AssemblyReference* aref       = new AssemblyReference(str);
    m_assemblyReferenceCache[str] = std::unique_ptr<AssemblyReference>(aref);
    return aref;
}

std::vector<IntegrationDefinition> GetIntegrationsFromTraceMethodsConfiguration(
    const WSTRING& integration_assembly_name, const WSTRING& integration_type_name, const WSTRING& configuration_string)
{
    std::vector<IntegrationDefinition> integrationDefinitions;
    const auto& integration_type = TypeReference(integration_assembly_name, integration_type_name, {}, {});

    auto dd_trace_methods_type = Split(configuration_string, ';');

    for (const WSTRING& trace_method_type : dd_trace_methods_type)
    {
        WSTRING type_name          = EmptyWStr;
        WSTRING method_definitions = EmptyWStr;

        // [] are required. Is either "*" or comma-separated values
        // We don't know the assembly name, only the type name
        auto firstOpenBracket = trace_method_type.find_first_of('[');
        if (firstOpenBracket != std::string::npos)
        {
            auto firstCloseBracket = trace_method_type.find_first_of(']', firstOpenBracket + 1);
            auto secondOpenBracket = trace_method_type.find_first_of('[', firstOpenBracket + 1);
            if (firstCloseBracket != std::string::npos &&
                (secondOpenBracket == std::string::npos || firstCloseBracket < secondOpenBracket))
            {
                auto length        = firstCloseBracket - firstOpenBracket - 1;
                method_definitions = trace_method_type.substr(firstOpenBracket + 1, length);
            }
        }

        if (method_definitions.empty())
        {
            continue;
        }

        type_name                     = trace_method_type.substr(0, firstOpenBracket);
        auto method_definitions_array = Split(method_definitions, ',');
        for (const WSTRING& method_definition : method_definitions_array)
        {
            // TODO handle a * wildcard, where a * wildcard invalidates other entries for the same type
            std::vector<WSTRING> signatureTypes;
            integrationDefinitions.push_back(
                IntegrationDefinition(MethodReference(tracemethodintegration_assemblyname, type_name, method_definition,
                                                      Version(0, 0, 0, 0),
                                                      Version(USHRT_MAX, USHRT_MAX, USHRT_MAX, USHRT_MAX),
                                                      signatureTypes),
                                      integration_type, false, false));

            if (Logger::IsDebugEnabled())
            {
                Logger::Debug("GetIntegrationsFromTraceMethodsConfiguration:  * Target: ", type_name, ".",
                              method_definition, "(", signatureTypes.size(), ")");
            }
        }
    }

    return integrationDefinitions;
}

namespace
{

WSTRING GetNameFromAssemblyReferenceString(const WSTRING& wstr)
{
    WSTRING name = wstr;

    auto pos = name.find(WStr(','));
    if (pos != WSTRING::npos)
    {
        name = name.substr(0, pos);
    }

    // strip spaces
    pos = name.rfind(WStr(' '));
    if (pos != WSTRING::npos)
    {
        name = name.substr(0, pos);
    }

    return name;
}

Version GetVersionFromAssemblyReferenceString(const WSTRING& str)
{
    unsigned short major    = 0;
    unsigned short minor    = 0;
    unsigned short build    = 0;
    unsigned short revision = 0;

    if (!str.empty())
    {
        ExtractVersion(str, major, minor, build, revision);
    }

    return {major, minor, build, revision};
}

WSTRING GetLocaleFromAssemblyReferenceString(const WSTRING& str)
{
    WSTRING locale = WStr("neutral");

    if (!str.empty())
    {
        WSTRING culture = ExtractCulture(str);
        if (!culture.empty())
        {
            locale = culture;
        }
    }

    return locale;
}

PublicKey GetPublicKeyFromAssemblyReferenceString(const WSTRING& str)
{
    BYTE data[kPublicKeySize] = {0};

    if (!str.empty())
    {
        ExtractPublicKeyToken(str, data, kPublicKeySize);
    }

    return PublicKey(data);
}

} // namespace

} // namespace trace
