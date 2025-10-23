// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "integration.h"

// Undefine macros from PAL headers that conflict with <regex>
#ifdef __pre
#undef __pre
#endif
#ifdef __post
#undef __post
#endif
#ifdef __inner
#undef __inner
#endif

#include <regex>
#include <sstream>

#include <unordered_map>

#include "logger.h"
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

    if (str.empty())
    {
        return {major, minor, build, revision};
    }

#ifdef _WIN32
    static auto re = std::wregex(WStr("Version=([0-9]+)\\.([0-9]+)\\.([0-9]+)\\.([0-9]+)"));
    std::wsmatch match;

    if (std::regex_search(str, match, re) && match.size() == 5)
    {
        WSTRINGSTREAM(match.str(1)) >> major;
        WSTRINGSTREAM(match.str(2)) >> minor;
        WSTRINGSTREAM(match.str(3)) >> build;
        WSTRINGSTREAM(match.str(4)) >> revision;
    }
#else
    // On Linux/MacOS, convert to narrow string and use regular regex
    static std::regex re("Version=([0-9]+)\\.([0-9]+)\\.([0-9]+)\\.([0-9]+)");
    std::string narrow_str = ToString(str);
    std::smatch match;

    if (std::regex_search(narrow_str, match, re) && match.size() == 5)
    {
        major = (unsigned short)std::stoi(match.str(1));
        minor = (unsigned short)std::stoi(match.str(2));
        build = (unsigned short)std::stoi(match.str(3));
        revision = (unsigned short)std::stoi(match.str(4));
    }
#endif

    return {major, minor, build, revision};
}

WSTRING GetLocaleFromAssemblyReferenceString(const WSTRING& str)
{
    WSTRING locale = WStr("neutral");

    if (str.empty())
    {
        return locale;
    }

#ifdef _WIN32
    static auto  re = std::wregex(WStr("Culture=([a-zA-Z0-9]+)"));
    std::wsmatch match;

    if (std::regex_search(str, match, re) && match.size() == 2)
    {
        locale = match.str(1);
    }
#else
    // On Linux/MacOS, convert to narrow string and use regular regex
    static std::regex re("Culture=([a-zA-Z0-9]+)");
    std::string narrow_str = ToString(str);
    std::smatch match;

    if (std::regex_search(narrow_str, match, re) && match.size() == 2)
    {
        locale = ToWSTRING(match.str(1));
    }
#endif

    return locale;
}

PublicKey GetPublicKeyFromAssemblyReferenceString(const WSTRING& str)
{
    BYTE data[8] = {0};

    if (str.empty())
    {
        return PublicKey(data);
    }

#ifdef _WIN32
    static auto  re = std::wregex(WStr("PublicKeyToken=([a-fA-F0-9]{16})"));
    std::wsmatch match;

    if (std::regex_search(str, match, re) && match.size() == 2)
    {
        for (int i = 0; i < 8; i++)
        {
            auto          s = match.str(1).substr(i * 2, 2);
            unsigned long x;
            WSTRINGSTREAM(s) >> std::hex >> x;
            data[i] = BYTE(x);
        }
    }
#else
    // On Linux/MacOS, convert to narrow string and use regular regex
    static std::regex re("PublicKeyToken=([a-fA-F0-9]{16})");
    std::string narrow_str = ToString(str);
    std::smatch match;

    if (std::regex_search(narrow_str, match, re) && match.size() == 2)
    {
        for (int i = 0; i < 8; i++)
        {
            auto s = match.str(1).substr(i * 2, 2);
            unsigned long x;
            std::stringstream(s) >> std::hex >> x;
            data[i] = BYTE(x);
        }
    }
#endif

    return PublicKey(data);
}

} // namespace

} // namespace trace
