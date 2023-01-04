#include "integration_loader.h"

#include <exception>
#include <unordered_set>

#include "environment_variables.h"
#include "logger.h"
#include "util.h"

template <>
struct std::hash<trace::IntegrationMethod> {
  // needed for creating unordered_set to load all integration methods
  std::size_t operator()(const trace::IntegrationMethod& integration_method) const noexcept {
    // it is enough to use integration_name to calculate hash
    return std::hash<trace::WSTRING>()(integration_method.integration_name);
  }
};

namespace trace
{

using json = nlohmann::json;

void LoadIntegrationsFromEnvironment(
    std::vector<IntegrationMethod>& integrationMethods,
    const LoadIntegrationConfiguration& configuration) {
    for (const WSTRING& filePath : GetEnvironmentValues(environment::integrations_path, ENV_VAR_PATH_SEPARATOR))
    {
        Logger::Debug("Loading integrations from file: ", filePath);
      LoadIntegrationsFromFile(filePath, integrationMethods, configuration);
    }
}

void LoadIntegrationsFromFile(
    const WSTRING& file_path,
    std::vector<IntegrationMethod>& integrationMethods,
    const LoadIntegrationConfiguration& configuration) {
    try
    {
        std::ifstream stream(ToString(file_path));

        if (static_cast<bool>(stream))
        {
            LoadIntegrationsFromStream(stream, integrationMethods, configuration);
        }
        else
        {
            Logger::Warn("Failed to load integrations from file ", file_path);
        }

        stream.close();
    }
    catch (...)
    {
        auto ex = std::current_exception();
        try
        {
            if (ex)
            {
                std::rethrow_exception(ex);
            }
        }
        catch (const std::exception& ex)
        {
            Logger::Warn("Failed to load integrations: ", ex.what());
        }
    }
}

void LoadIntegrationsFromStream(
    std::istream& stream,
    std::vector<IntegrationMethod>& integrationMethods,
    const LoadIntegrationConfiguration& configuration) {
    try
    {
        json j;
        // parse the stream
        stream >> j;

        std::unordered_set<IntegrationMethod> set;

        for (const auto& el : j)
        {
            IntegrationFromJson(el, set, configuration);
        }

        integrationMethods.reserve(set.size());
        for (auto &integration_method : set)
        {
            integrationMethods.push_back(integration_method);
        }
    }
    catch (const json::parse_error& e)
    {
        Logger::Warn("Invalid integrations:", e.what());
    }
    catch (const json::type_error& e)
    {
        Logger::Warn("Invalid integrations:", e.what());
    }
    catch (...)
    {
        auto ex = std::current_exception();
        try
        {
            if (ex)
            {
                std::rethrow_exception(ex);
            }
        }
        catch (const std::exception& ex)
        {
            Logger::Warn("Failed to load integrations: ", ex.what());
        }
    }
}

namespace
{
    bool InstrumentationEnabled(
        const WSTRING name,
        const std::vector<WSTRING>& enabledIntegrationNames,
        const std::vector<WSTRING>& disabledIntegrationNames)
    {
        // check if the integration is disabled
        for (const WSTRING& disabledName : disabledIntegrationNames)
        {
            if (name == disabledName) 
            {
                return false;
            }
        }

        if (enabledIntegrationNames.empty())
        {
            return true;
        }

        // check if the integration is enabled
        for (const WSTRING& enabledName : enabledIntegrationNames)
        {
            if (name == enabledName)
            {
                return true;
            }
        }
        return false;
    }

    void IntegrationFromJson(const json::value_type& src,
                         std::unordered_set<IntegrationMethod>& integrationMethods,
                         const LoadIntegrationConfiguration& configuration)
    {
        if (!src.is_object())
        {
            return;
        }

        // first get the name, which is required
        const WSTRING name = ToWSTRING(src.value("name", ""));
        if (name.empty())
        {
            Logger::Warn("Integration name is missing for integration: ", src.dump());
            return;
        }

        const WSTRING type = ToWSTRING(src.value("type", ""));
        if (name.empty())
        {
            Logger::Warn("Integration type is missing for integration: ", src.dump());
            return;
        }

        if (type == WStr("Trace"))
        {
            if (!configuration.traces_enabled)
            {
                  return;
            }

            if (!InstrumentationEnabled(name, configuration.enabledTraceIntegrationNames, configuration.disabledTraceIntegrationNames)) 
            {
                return;
            }
        }
        else if (type == WStr("Metric"))
        {
            if (!configuration.metrics_enabled)
            {
                return;
            }
            if (!InstrumentationEnabled(name, configuration.enabledMetricIntegrationNames, configuration.disabledMetricIntegrationNames))
            {
                return;
            }
        }
        else if (type == WStr("Log"))
        {
            if (!configuration.logs_enabled)
            {
                return;
            }
            if (!InstrumentationEnabled(name, configuration.enabledLogIntegrationNames, configuration.disabledLogIntegrationNames))
            {
                return;
            }
        }
        else
        {
            Logger::Warn("Unsupported type for integration: ", src.dump());
            return;
        }

        auto arr = src.value("method_replacements", json::array());
        if (arr.is_array())
        {
            for (const auto& el : arr)
            {
                MethodReplacementFromJson(el, name, integrationMethods);
            }
        }
    }

    void MethodReplacementFromJson(const json::value_type& src, const WSTRING& integrationName, std::unordered_set<IntegrationMethod>& integrationMethods)
    {
        if (src.is_object())
        {
            const MethodReference wrapper = MethodReferenceFromJson(src.value("wrapper", json::object()), false, true);
            const MethodReference target =
                MethodReferenceFromJson(src.value("target", json::object()), true, false);

            integrationMethods.insert({integrationName, {{}, target, wrapper}});
        }
    }

    MethodReference MethodReferenceFromJson(const json::value_type& src, const bool is_target_method,
                                            const bool is_wrapper_method)
    {
        if (!src.is_object())
        {
            return {};
        }

        const auto assembly = ToWSTRING(src.value("assembly", ""));
        const auto type = ToWSTRING(src.value("type", ""));
        const auto method = ToWSTRING(src.value("method", ""));
        auto raw_signature = src.value("signature", json::array());

        const auto eoj = src.end();
        USHORT min_major = 0;
        USHORT min_minor = 0;
        USHORT min_patch = 0;
        USHORT max_major = USHRT_MAX;
        USHORT max_minor = USHRT_MAX;
        USHORT max_patch = USHRT_MAX;
        std::vector<WSTRING> signature_type_array;

        if (is_target_method)
        {
            // these fields only exist in the target definition

            if (src.find("minimum_major") != eoj)
            {
                min_major = src["minimum_major"].get<USHORT>();
            }
            if (src.find("minimum_minor") != eoj)
            {
                min_minor = src["minimum_minor"].get<USHORT>();
            }
            if (src.find("minimum_patch") != eoj)
            {
                min_patch = src["minimum_patch"].get<USHORT>();
            }
            if (src.find("maximum_major") != eoj)
            {
                max_major = src["maximum_major"].get<USHORT>();
            }
            if (src.find("maximum_minor") != eoj)
            {
                max_minor = src["maximum_minor"].get<USHORT>();
            }
            if (src.find("maximum_patch") != eoj)
            {
                max_patch = src["maximum_patch"].get<USHORT>();
            }

            if (src.find("signature_types") != eoj)
            {
                // c++ is unable to handle null values in this array
                // we would need to write out own parsing here for null values
                auto sig_types = src["signature_types"].get<std::vector<std::string>>();
                signature_type_array = std::vector<WSTRING>(sig_types.size());
                for (auto i = sig_types.size() - 1; i < sig_types.size(); i--)
                {
                    signature_type_array[i] = ToWSTRING(sig_types[i]);
                }
            }
        }

        std::vector<BYTE> signature;
        if (raw_signature.is_array())
        {
            for (auto& el : raw_signature)
            {
                if (el.is_number_unsigned())
                {
                    signature.push_back(BYTE(el.get<BYTE>()));
                }
            }
        }
        else if (raw_signature.is_string())
        {
            // load as a hex string
            std::string str = raw_signature;
            bool flip = false;
            char prev = 0;
            for (auto& c : str)
            {
                BYTE b = 0;
                if ('0' <= c && c <= '9')
                {
                    b = c - '0';
                }
                else if ('a' <= c && c <= 'f')
                {
                    b = c - 'a' + 10;
                }
                else if ('A' <= c && c <= 'F')
                {
                    b = c - 'A' + 10;
                }
                else
                {
                    // skip any non-hex character
                    continue;
                }
                if (flip)
                {
                    signature.push_back((prev << 4) + b);
                }
                flip = !flip;
                prev = b;
            }
        }
        return MethodReference(assembly, type, method, Version(min_major, min_minor, min_patch, 0),
                               Version(max_major, max_minor, max_patch, USHRT_MAX), signature, signature_type_array);
    }

} // namespace

} // namespace trace
