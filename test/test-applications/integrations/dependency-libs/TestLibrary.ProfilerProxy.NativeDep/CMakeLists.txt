# Copyright The OpenTelemetry Authors
# SPDX-License-Identifier: Apache-2.0

cmake_minimum_required(VERSION 3.13..3.20)
project(ProfilerProxy LANGUAGES CXX)

add_library(TestLibrary.ProfilerProxy.NativeDep SHARED profiler_proxy.cpp)
target_compile_features(TestLibrary.ProfilerProxy.NativeDep PRIVATE cxx_std_17)
target_compile_options(TestLibrary.ProfilerProxy.NativeDep PRIVATE -Wall -Wextra -Werror)
target_link_libraries(TestLibrary.ProfilerProxy.NativeDep PRIVATE ${CMAKE_DL_LIBS})
set_target_properties(TestLibrary.ProfilerProxy.NativeDep PROPERTIES
    PREFIX ""
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/bin")
