# Copyright The OpenTelemetry Authors
# SPDX-License-Identifier: Apache-2.0

find_package(GTest CONFIG REQUIRED)

add_executable(OpenTelemetry.AutoInstrumentation.Native.Tests
    assembly_version_redirection_test.cpp
    continuous_profiler_batch_test.cpp
    environment_variables_parser_test.cpp
    function_control_wrapper_test.cpp
    integration_test.cpp
    runtime_async_test.cpp
    runtime_sampler_configuration_test.cpp
    runtime_sampler_service_test.cpp
    sampling_cohort_test.cpp
    signature_builder_test.cpp
    stack_capture_buffer_test.cpp
    stack_capture_shutdown_test.cpp
    startup_hook_test.cpp
    string_utils_test.cpp
    thread_span_context_test.cpp
    unsafe_accessor_type_attribute_updater_test.cpp
    version_struct_test.cpp
)

target_link_libraries(OpenTelemetry.AutoInstrumentation.Native.Tests PRIVATE
    OpenTelemetry.AutoInstrumentation.Native.static
    GTest::gtest_main
)
