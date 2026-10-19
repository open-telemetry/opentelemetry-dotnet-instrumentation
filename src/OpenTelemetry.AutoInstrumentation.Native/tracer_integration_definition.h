// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma once
#include "integration.h"

namespace trace
{
struct IntegrationDefinition
{
private:
    std::uint32_t categories = 1;
    std::uint32_t enabled_categories = 0;

public:
    const MethodReference target_method;
    const TypeReference integration_type;
    const bool is_derived = false;
    const bool is_interface = false;
    const bool is_exact_signature_match = true;

    IntegrationDefinition()
    {
    }

    IntegrationDefinition(const MethodReference& target_method, const TypeReference& integration_type, bool isDerived,
                          bool isInterface, bool is_exact_signature_match, std::uint32_t categories = 1,
                          std::uint32_t enabledCategories = static_cast<std::uint32_t>(-1)) :
        categories(categories),
        enabled_categories(categories & enabledCategories),
        target_method(target_method),
        integration_type(integration_type),
        is_derived(isDerived),
        is_interface(isInterface),
        is_exact_signature_match(is_exact_signature_match)
    {
    }

    inline bool operator==(const IntegrationDefinition& other) const
    {
        return target_method == other.target_method && integration_type == other.integration_type &&
               is_derived == other.is_derived && is_interface == other.is_interface &&
               is_exact_signature_match == other.is_exact_signature_match && categories == other.categories;
    }

    inline bool GetEnabled() const
    {
        return enabled_categories != 0;
    }

    void Update(const IntegrationDefinition& other)
    {
        enabled_categories = other.enabled_categories;
    }
};

std::vector<IntegrationDefinition> GetIntegrationsFromTraceMethodsConfiguration(
    const TypeReference& integration_type,
    const WSTRING& configuration_string);
} // namespace trace
