# Copyright The OpenTelemetry Authors
# SPDX-License-Identifier: Apache-2.0

cmake_minimum_required(VERSION 3.16)

include(FetchContent)

set(BUILD_GMOCK OFF)
set(INSTALL_GTEST OFF)
FetchContent_Declare(
    googletest
    GIT_REPOSITORY https://github.com/google/googletest.git
    GIT_TAG 063de7e9578f82b369302001269680b4b1553359 # v1.18.0
)
FetchContent_MakeAvailable(googletest)

# clr_helper_test.cpp and metadata_builder_test.cpp require Windows CLR hosting APIs.
# continuous_profiler_reader_mutex_test.cpp and util_test.cpp use Win32 APIs.
# Add new portable tests here and to OpenTelemetry.AutoInstrumentation.Native.Tests.vcxproj.
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

# Declare C++20 so CMake accounts for the language standard exported by GoogleTest.
set_target_properties(OpenTelemetry.AutoInstrumentation.Native.Tests PROPERTIES
    CXX_STANDARD 20
    CXX_STANDARD_REQUIRED ON
    CXX_EXTENSIONS OFF
)

target_link_libraries(OpenTelemetry.AutoInstrumentation.Native.Tests PRIVATE
    OpenTelemetry.AutoInstrumentation.Native.static
    GTest::gtest_main
)
