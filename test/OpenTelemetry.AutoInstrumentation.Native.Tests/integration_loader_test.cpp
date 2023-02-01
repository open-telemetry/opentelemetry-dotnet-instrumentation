#include "pch.h"

#include <codecvt>
#include <filesystem>
#include <fstream>
#include <locale>
#include <sstream>
#include <string>
#include <vector>

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/environment_variables.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/integration_loader.h"

using namespace trace;

TEST(IntegrationLoaderTest, HandlesMissingFile)
{
    std::vector<IntegrationMethod> integrations;
    const LoadIntegrationConfiguration configuration(true, {}, true, {}, true, {});
    LoadIntegrationsFromFile(L"missing-file", integrations, configuration);
    EXPECT_EQ(0, integrations.size());
}

TEST(IntegrationLoaderTest, HandlesInvalidIntegrationNoName)
{
    std::vector<IntegrationMethod> integrations;
    std::stringstream str("[{}]");
    const LoadIntegrationConfiguration configuration(true, {}, true, {}, true, {});
    LoadIntegrationsFromStream(str, integrations, configuration);
    // 0 because name is required
    EXPECT_EQ(0, integrations.size());
}

TEST(IntegrationLoaderTest, HandlesInvalidIntegrationBadJson)
{
    std::vector<IntegrationMethod> integrations;
    std::stringstream str("[");
    const LoadIntegrationConfiguration configuration(true, {}, true, {}, true, {});
    LoadIntegrationsFromStream(str, integrations, configuration);
    EXPECT_EQ(0, integrations.size());
}

TEST(IntegrationLoaderTest, HandlesInvalidIntegrationNotAnObject)
{
    std::vector<IntegrationMethod> integrations;
    std::stringstream str("[1,2,3]");
    const LoadIntegrationConfiguration configuration(true, {}, true, {}, true, {});
    LoadIntegrationsFromStream(str, integrations, configuration);
    EXPECT_EQ(0, integrations.size());
}

TEST(IntegrationLoaderTest, HandlesInvalidIntegrationNotAnArray)
{
    std::vector<IntegrationMethod> integrations;
    std::stringstream str(R"TEXT(
        {"name": "test-integration"}
    )TEXT");
    const LoadIntegrationConfiguration configuration(true, {L"test-integration"}, true, {}, true, {});
    LoadIntegrationsFromStream(str, integrations, configuration);
    EXPECT_EQ(0, integrations.size());
}

TEST(IntegrationLoaderTest, HandlesSingleIntegrationWithMethodReplacements)
{
    std::vector<IntegrationMethod> integrations;
    std::stringstream str(R"TEXT(
        [{
            "name": "test-integration",
            "type": "Trace",
            "method_replacements": [{
                "caller": { },
                "target": { "assembly": "Assembly.One", "type": "Type.One", "method": "Method.One", "minimum_major": 0, "minimum_minor": 1, "maximum_major": 10, "maximum_minor": 0 },
                "wrapper": { "assembly": "Assembly.Two", "type": "Type.Two", "method": "Method.Two", "signature": [0, 1, 1, 28] }
            }]
        }]
    )TEXT");

    const LoadIntegrationConfiguration configuration(true, {L"test-integration"}, true, {}, true, {});
    LoadIntegrationsFromStream(str, integrations, configuration);
    EXPECT_EQ(1, integrations.size());
    EXPECT_STREQ(L"test-integration", integrations[0].integration_name.c_str());
}

TEST(IntegrationLoaderTest, DoesNotCrashWithOutOfRangeVersion)
{
    std::vector<IntegrationMethod> integrations;
    std::stringstream str(R"TEXT(
        [{
            "name": "test-integration",
            "type": "Trace",
            "method_replacements": [{
                "caller": { },
                "target": { "assembly": "Assembly.One", "type": "Type.One", "method": "Method.One", "minimum_major": 0, "minimum_minor": 1, "maximum_major": 75555, "maximum_minor": 0 },
                "wrapper": { "assembly": "Assembly.Two", "type": "Type.Two", "method": "Method.Two", "signature": [0, 1, 1, 28] }
            }]
        }]
    )TEXT");

    const LoadIntegrationConfiguration configuration(true, {L"test-integration"}, true, {}, true, {});
    LoadIntegrationsFromStream(str, integrations, configuration);
    EXPECT_EQ(1, integrations.size());
    EXPECT_STREQ(L"test-integration", integrations[0].integration_name.c_str());

    const auto mr = integrations[0].replacement;
    EXPECT_STREQ(L"", mr.caller_method.assembly.name.c_str());
    EXPECT_STREQ(L"", mr.caller_method.type_name.c_str());
    EXPECT_STREQ(L"", mr.caller_method.method_name.c_str());
    EXPECT_STREQ(L"Assembly.One", mr.target_method.assembly.name.c_str());
    EXPECT_STREQ(L"Type.One", mr.target_method.type_name.c_str());
    EXPECT_STREQ(L"Method.One", mr.target_method.method_name.c_str());
    EXPECT_STREQ(L"Assembly.Two", mr.wrapper_method.assembly.name.c_str());
    EXPECT_STREQ(L"Type.Two", mr.wrapper_method.type_name.c_str());
    EXPECT_STREQ(L"Method.Two", mr.wrapper_method.method_name.c_str());
    EXPECT_EQ(std::vector<uint8_t>({0, 1, 1, 28}), mr.wrapper_method.method_signature.data);
}

TEST(IntegrationLoaderTest, HandlesSingleIntegrationWithMissingCaller)
{
    std::vector<IntegrationMethod> integrations;
    std::stringstream str(R"TEXT(
        [{
            "name": "test-integration",
            "type": "Trace",
            "method_replacements": [{
                "target": { "assembly": "Assembly.One", "type": "Type.One", "method": "Method.One", "minimum_major": 1, "minimum_minor": 2, "maximum_major": 10, "maximum_minor": 99 },
                "wrapper": { "assembly": "Assembly.Two", "type": "Type.Two", "method": "Method.Two", "signature": [0, 1, 1, 28] }
            }]
        }]
    )TEXT");

    const LoadIntegrationConfiguration configuration(true, {L"test-integration"}, true, {}, true, {});
    LoadIntegrationsFromStream(str, integrations, configuration);
    EXPECT_EQ(1, integrations.size());
    EXPECT_STREQ(L"test-integration", integrations[0].integration_name.c_str());

    auto mr = integrations[0].replacement;
    EXPECT_STREQ(L"", mr.caller_method.assembly.name.c_str());
    EXPECT_STREQ(L"", mr.caller_method.type_name.c_str());
    EXPECT_STREQ(L"", mr.caller_method.method_name.c_str());
    EXPECT_STREQ(L"Assembly.One", mr.target_method.assembly.name.c_str());
    EXPECT_STREQ(L"Type.One", mr.target_method.type_name.c_str());
    EXPECT_STREQ(L"Method.One", mr.target_method.method_name.c_str());
    EXPECT_STREQ(L"Assembly.Two", mr.wrapper_method.assembly.name.c_str());
    EXPECT_STREQ(L"Type.Two", mr.wrapper_method.type_name.c_str());
    EXPECT_STREQ(L"Method.Two", mr.wrapper_method.method_name.c_str());
    EXPECT_STREQ(L"Method.Two", mr.wrapper_method.method_name.c_str());
    EXPECT_EQ(1, mr.target_method.min_version.major);
    EXPECT_EQ(2, mr.target_method.min_version.minor);
    EXPECT_EQ(0, mr.target_method.min_version.build);
    EXPECT_EQ(10, mr.target_method.max_version.major);
    EXPECT_EQ(99, mr.target_method.max_version.minor);
    EXPECT_EQ(USHRT_MAX, mr.target_method.max_version.build);
    EXPECT_EQ(std::vector<uint8_t>({0, 1, 1, 28}), mr.wrapper_method.method_signature.data);
}

TEST(IntegrationLoaderTest, HandlesSingleIntegrationWithInvalidTarget)
{
    std::vector<IntegrationMethod> integrations;
    std::stringstream str(R"TEXT(
        [{
            "name": "test-integration",
            "type": "Trace",
            "method_replacements": [{
                "target": 1234,
                "wrapper": { "assembly": "Assembly.Two", "type": "Type.Two", "method": "Method.Two" }
            }]
        }]
    )TEXT");

    const LoadIntegrationConfiguration configuration(true, {L"test-integration"}, true, {}, true, {});
    LoadIntegrationsFromStream(str, integrations, configuration);
    EXPECT_EQ(1, integrations.size());
    EXPECT_STREQ(L"test-integration", integrations[0].integration_name.c_str());

    auto mr = integrations[0].replacement;
    EXPECT_STREQ(L"", mr.target_method.assembly.name.c_str());
    EXPECT_STREQ(L"", mr.target_method.type_name.c_str());
    EXPECT_STREQ(L"", mr.target_method.method_name.c_str());
}

TEST(IntegrationLoaderTest, LoadsFromEnvironment)
{
    auto temp_name1 = std::filesystem::temp_directory_path() / "test-1.json";
    auto temp_name2 = std::filesystem::temp_directory_path() / "test-2.json";
    std::ofstream f;
    f.open(temp_name1);
    f << R"TEXT(
        [{ "name": "test-integration-1", "type": "Trace", "method_replacements": [{ "caller": {}, "target": {}, "wrapper": {} }] }]
    )TEXT";
    f.close();
    f.open(temp_name2);
    f << R"TEXT(
        [{ "name": "test-integration-2", "type": "Trace", "method_replacements": [{ "caller": {}, "target": {}, "wrapper": {} }] }]
    )TEXT";
    f.close();

    auto name = temp_name1.wstring() + L";" + temp_name2.wstring();

    SetEnvironmentVariableW(trace::environment::integrations_path.data(), name.data());

    const std::vector<std::wstring> expected_names = {L"test-integration-1", L"test-integration-2"};
    std::vector<std::wstring> actual_names;
    std::vector<IntegrationMethod> integrations;
    const LoadIntegrationConfiguration configuration(true, {L"test-integration-1", L"test-integration-2"}, true, {}, true, {});
    LoadIntegrationsFromEnvironment(integrations, configuration);
    for (auto& integration : integrations)
    {
        actual_names.push_back(integration.integration_name);
    }
    EXPECT_EQ(expected_names, actual_names);

    std::filesystem::remove(temp_name1);
    std::filesystem::remove(temp_name2);
}

TEST(IntegrationLoaderTest, DeserializesSignatureTypeArray)
{
    std::vector<IntegrationMethod> integrations;
    std::stringstream str(R"TEXT(
        [{
            "name": "test-integration",
            "type": "Trace",
            "method_replacements": [{
                "caller": { },
                "target": { "assembly": "Assembly.One", "type": "Type.One", "method": "Method.One", "signature_types": ["System.Void", "_", "FakeClient.Pipeline'1<T>"] },
                "wrapper": { "assembly": "Assembly.Two", "type": "Type.Two", "method": "Method.One", "signature": [0, 1, 1, 28] }
            }]
        }]
    )TEXT");

    const LoadIntegrationConfiguration configuration(true, {L"test-integration"}, true, {}, true, {});
    LoadIntegrationsFromStream(str, integrations, configuration);
    const auto target = integrations[0].replacement.target_method;
    EXPECT_STREQ(L"System.Void", target.signature_types[0].c_str());
    EXPECT_STREQ(L"_", target.signature_types[1].c_str());
    EXPECT_STREQ(L"FakeClient.Pipeline'1<T>", target.signature_types[2].c_str());
}

TEST(IntegrationLoaderTest, SupportsEnabledTraceIntegrations) {
    std::vector<IntegrationMethod> integrations;
    std::stringstream str(R"TEXT(
        [
            { "name": "test-trace-integration-1", "type": "Trace", "method_replacements": [{ "caller": {}, "target": {}, "wrapper": {} }] },
            { "name": "test-trace-integration-2", "type": "Trace", "method_replacements": [{ "caller": {}, "target": {}, "wrapper": {} }] }
        ]
    )TEXT");

    const std::vector<std::wstring> expected_names = {L"test-trace-integration-2"};
    std::vector<std::wstring> actual_names;
    const LoadIntegrationConfiguration configuration(true, {L"test-trace-integration-2"}, true, {}, true, {});
    LoadIntegrationsFromStream(str, integrations, configuration);
    for (auto& integration : integrations)
    {
        actual_names.push_back(integration.integration_name);
    }
    EXPECT_EQ(expected_names, actual_names);
}

TEST(IntegrationLoaderTest, SupportsEnabledMetricIntegrations) {
    std::vector<IntegrationMethod> integrations;
    std::stringstream str(R"TEXT(
          [
              { "name": "test-metric-integration-1", "type": "Metric", "method_replacements": [{ "caller": {}, "target": {}, "wrapper": {} }] },
              { "name": "test-metric-integration-2", "type": "Metric", "method_replacements": [{ "caller": {}, "target": {}, "wrapper": {} }] }
          ]
      )TEXT");

    const std::vector<std::wstring> expected_names = {
        L"test-metric-integration-2"};
    std::vector<std::wstring> actual_names;
    const LoadIntegrationConfiguration configuration(true, {}, true, {L"test-metric-integration-2"}, true, {});
    LoadIntegrationsFromStream(str, integrations, configuration);
    for (auto& integration : integrations) {
        actual_names.push_back(integration.integration_name);
    }
    EXPECT_EQ(expected_names, actual_names);
}

TEST(IntegrationLoaderTest, SupportsEnabledLogIntegrations)
{
    std::vector<IntegrationMethod> integrations;
    std::stringstream str(R"TEXT(
          [
              { "name": "test-log-integration-1", "type": "Log", "method_replacements": [{ "caller": {}, "target": {}, "wrapper": {} }] },
              { "name": "test-log-integration-2", "type": "Log", "method_replacements": [{ "caller": {}, "target": {}, "wrapper": {} }] }
          ]
      )TEXT");
  
    const std::vector<std::wstring> expected_names = {L"test-log-integration-2"};
    std::vector<std::wstring> actual_names;
    const LoadIntegrationConfiguration configuration(true, {}, true, {}, true, {L"test-log-integration-2"});
    LoadIntegrationsFromStream(str, integrations, configuration);
    for (auto& integration : integrations)
    {
        actual_names.push_back(integration.integration_name);
    }
    EXPECT_EQ(expected_names, actual_names);
}

TEST(IntegrationLoaderTest, SupportsDisableAllIntegrations) {
    std::vector<IntegrationMethod> integrations;
    std::stringstream str(R"TEXT(
        [
            { "name": "test-trace-integration-1", "type": "Trace", "method_replacements": [{ "caller": {}, "target": {}, "wrapper": {} }] },
            { "name": "test-metric-integration-1", "type": "Metric", "method_replacements": [{ "caller": {}, "target": {}, "wrapper": {} }] },
            { "name": "test-log-integration-1", "type": "Log", "method_replacements": [{ "caller": {}, "target": {}, "wrapper": {} }] }
        ]
    )TEXT");

    const std::vector<std::wstring> expected_names = {};
    std::vector<std::wstring> actual_names;
    const LoadIntegrationConfiguration configuration(false, {}, false, {}, false, {});
    LoadIntegrationsFromStream(str, integrations, configuration);

    for (auto& integration : integrations) {
        actual_names.push_back(integration.integration_name);
    }
    EXPECT_EQ(expected_names, actual_names);
}

TEST(IntegrationLoaderTest, DuplicatedIntegrations) {
    std::vector<IntegrationMethod> integrations;
    std::stringstream str(R"TEXT(
        [
            { "name": "test-integration-1", "type": "Trace", "method_replacements": [{ "caller": {}, "target": { "assembly": "Assembly.One", "type": "Type.One", "method": "Method.One", "signature_types": ["System.Void", "_", "FakeClient.Pipeline'1<T>"] }, "wrapper": { "assembly": "Assembly.Two", "type": "Type.Two", "method": "Method.Two", "signature": [0, 1, 1, 28] } }] },
            { "name": "test-integration-1", "type": "Trace", "method_replacements": [{ "caller": {}, "target": { "assembly": "Assembly.One", "type": "Type.One", "method": "Method.One", "signature_types": ["System.Void", "_", "FakeClient.PipelineForTrace'1<T>"] }, "wrapper": { "assembly": "Assembly.Two", "type": "Type.Two", "method": "Method.Two", "signature": [0, 1, 1, 28] } }] },
            { "name": "test-integration-1", "type": "Metric", "method_replacements": [{ "caller": {}, "target": { "assembly": "Assembly.One", "type": "Type.One", "method": "Method.One", "signature_types": ["System.Void", "_", "FakeClient.Pipeline'1<T>"] }, "wrapper": { "assembly": "Assembly.Two", "type": "Type.Two", "method": "Method.Two", "signature": [0, 1, 1, 28] } }] },
            { "name": "test-integration-1", "type": "Metric", "method_replacements": [{ "caller": {}, "target": { "assembly": "Assembly.One", "type": "Type.One", "method": "Method.One", "signature_types": ["System.Void", "_", "FakeClient.PipelineForMetric'1<T>"] }, "wrapper": { "assembly": "Assembly.Two", "type": "Type.Two", "method": "Method.Two", "signature": [0, 1, 1, 28] } }] },
            { "name": "test-integration-1", "type": "Log", "method_replacements": [{ "caller": {}, "target": { "assembly": "Assembly.One", "type": "Type.One", "method": "Method.One", "signature_types": ["System.Void", "_", "FakeClient.Pipeline'1<T>"] }, "wrapper": { "assembly": "Assembly.Two", "type": "Type.Two", "method": "Method.Two", "signature": [0, 1, 1, 28] } }] },
            { "name": "test-integration-1", "type": "Log", "method_replacements": [{ "caller": {}, "target": { "assembly": "Assembly.One", "type": "Type.One", "method": "Method.One", "signature_types": ["System.Void", "_", "FakeClient.PipelineForLog'1<T>"] }, "wrapper": { "assembly": "Assembly.Two", "type": "Type.Two", "method": "Method.Two", "signature": [0, 1, 1, 28] } }] }
        ]
    )TEXT");

    const LoadIntegrationConfiguration configuration(true, {L"test-integration-1"}, true, {L"test-integration-1"}, true, {L"test-integration-1"});
    LoadIntegrationsFromStream(str, integrations, configuration);

    EXPECT_EQ(4, integrations.size());
}
