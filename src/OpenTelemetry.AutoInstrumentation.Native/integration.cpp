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

namespace
{

WSTRING GetNameFromAssemblyReferenceString(const WSTRING& wstr)
{
    WSTRING name = wstr;

    auto pos = name.find(WStr(','));
    if (pos != WSTRING::npos)
    {
        name.resize(pos);
    }

    // strip spaces
    pos = name.rfind(WStr(' '));
    if (pos != WSTRING::npos)
    {
        name.resize(pos);
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
