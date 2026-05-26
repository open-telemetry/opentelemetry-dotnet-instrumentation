# Copyright The OpenTelemetry Authors
# SPDX-License-Identifier: Apache-2.0

cmake_minimum_required (VERSION 3.13..3.20)
cmake_policy(SET CMP0015 NEW)

# ******************************************************
# Environment detection
# ******************************************************

# Detect target operating system early enough to select compilers before project().
if (CMAKE_SYSTEM_NAME MATCHES "Linux" OR (NOT CMAKE_SYSTEM_NAME AND CMAKE_HOST_SYSTEM_NAME MATCHES "Linux"))
    SET(ISLINUX true)
elseif (CMAKE_SYSTEM_NAME MATCHES "Windows" OR (NOT CMAKE_SYSTEM_NAME AND CMAKE_HOST_SYSTEM_NAME MATCHES "Windows"))
    SET(ISWINDOWS true)
elseif (CMAKE_SYSTEM_NAME MATCHES "Darwin" OR (NOT CMAKE_SYSTEM_NAME AND CMAKE_HOST_SYSTEM_NAME MATCHES "Darwin"))
    SET(ISMACOS true)
endif()

# Select Linux compilers before project() so compiler feature checks use the same compiler as the build.
if (ISLINUX)
    find_program(FOUND_CLANG clang)
    if(NOT FOUND_CLANG)
        message(FATAL_ERROR "CLANG is required to build the project")
    endif()

    find_program(FOUND_CLANGPP clang++)
    if(NOT FOUND_CLANGPP)
        message(FATAL_ERROR "CLANG++ is required to build the project")
    endif()

    SET (CMAKE_C_COMPILER   ${FOUND_CLANG})
    SET (CMAKE_CXX_COMPILER ${FOUND_CLANGPP})
endif()

# ******************************************************
# Project definition
# ******************************************************

project("TestApplication.ContinuousProfiler.NativeDep" VERSION 0.0.1)

# ******************************************************
# Environment report
# ******************************************************

# Report operating system
if (ISWINDOWS)
    message(FATAL_ERROR "Windows builds are not supported using CMAKE. Please use Visual Studio")
elseif (ISLINUX)
    message(STATUS "Preparing Linux build")
elseif (ISMACOS)
    message(STATUS "Preparing macOS build")
endif()

# Detect bitness of the build
if (CMAKE_SIZEOF_VOID_P EQUAL 8)
    message(STATUS "Setting compilation for 64bits processor")
    SET(BIT64 true)
endif()

# Detect architecture
if (CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL amd64)
    message(STATUS "Architecture is x64/AMD64")
    SET(ISAMD64 true)
elseif (CMAKE_SYSTEM_PROCESSOR STREQUAL x86 OR CMAKE_SYSTEM_PROCESSOR STREQUAL i686)
    message(STATUS "Architecture is x86")
    SET(ISX86 true)
elseif (CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL arm64)
    message(STATUS "Architecture is ARM64")
    SET(ISARM64 true)
elseif (CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l OR CMAKE_SYSTEM_PROCESSOR STREQUAL arm)
    message(STATUS "Architecture is ARM")
    SET(ISARM true)
endif()

# ******************************************************
# Detect prerequisites
# ******************************************************
find_program(FOUND_GIT git)
if (NOT FOUND_GIT)
    message(FATAL_ERROR "GIT is required to build the project")
else()
    message(STATUS "GIT found: ${FOUND_GIT}")
endif()

find_program(FOUND_GCC gcc)
if(NOT FOUND_GCC)
    message(FATAL_ERROR "GCC is required to build the project's dependencies")
else()
    message(STATUS "GCC found: ${FOUND_GCC}")
endif()

if(NOT FOUND_CLANG)
    find_program(FOUND_CLANG clang)
endif()
if(NOT FOUND_CLANG)
    message(FATAL_ERROR "CLANG is required to build the project")
else()
    message(STATUS "CLANG found: ${FOUND_CLANG}")
endif()

if(NOT FOUND_CLANGPP)
    find_program(FOUND_CLANGPP clang++)
endif()
if(NOT FOUND_CLANGPP)
    message(FATAL_ERROR "CLANG++ is required to build the project")
else()
    message(STATUS "CLANG++ found: ${FOUND_CLANGPP}")
endif()

# ******************************************************
# Output folders
# ******************************************************

# Set output folders
SET(OUTPUT_BIN_DIR ${CMAKE_BINARY_DIR}/bin)
SET(OUTPUT_TMP_DIR ${CMAKE_BINARY_DIR}/tmp.${CMAKE_SYSTEM_NAME}_${CMAKE_SYSTEM_PROCESSOR})
FILE(MAKE_DIRECTORY ${OUTPUT_BIN_DIR})
FILE(MAKE_DIRECTORY ${OUTPUT_TMP_DIR})

SET(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${OUTPUT_BIN_DIR})
SET(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${OUTPUT_BIN_DIR})
SET(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${OUTPUT_BIN_DIR})

# ******************************************************
# Dependencies
# ******************************************************

# ******************************************************
# Compiler options
# ******************************************************

# Sets compiler options
add_compile_options(-std=c++17 -fPIC -fms-extensions)
add_compile_options(-DPAL_STDCPP_COMPAT -DPLATFORM_UNIX -DUNICODE)
add_compile_options(-Wno-invalid-noreturn -Wno-macro-redefined)
if (ISMACOS)
    add_compile_options(-stdlib=libc++ -DMACOS -Wno-pragma-pack)
elseif(ISLINUX)
    add_compile_options(-stdlib=libstdc++ -DLINUX -Wno-pragmas)
endif()
if (BIT64)
    add_compile_options(-DBIT64 -DHOST_64BIT)
endif()
if (ISAMD64)
    add_compile_options(-DAMD64)
elseif (ISX86)
    add_compile_options(-DBX86 -DHOST_X86)
elseif (ISARM64)
    add_compile_options(-DARM64)
elseif (ISARM)
    add_compile_options(-DARM)
endif()

# ******************************************************
# Suppress Warning on MacOS
# ******************************************************

# Only necessary with cmake 3.19.x on macos
# See https://stackoverflow.com/questions/4929255/building-static-libraries-on-mac-using-cmake-and-gcc#answer-4953904

if (ISMACOS)
    SET(CMAKE_C_ARCHIVE_CREATE   "<CMAKE_AR> Scr <TARGET> <LINK_FLAGS> <OBJECTS>")
    SET(CMAKE_CXX_ARCHIVE_CREATE "<CMAKE_AR> Scr <TARGET> <LINK_FLAGS> <OBJECTS>")
    SET(CMAKE_C_ARCHIVE_FINISH   "<CMAKE_RANLIB> -no_warning_for_no_symbols -c <TARGET>")
    SET(CMAKE_CXX_ARCHIVE_FINISH "<CMAKE_RANLIB> -no_warning_for_no_symbols -c <TARGET>")
endif()


# ******************************************************
# Define shared target
# ******************************************************
SET(TARGET_NAME "TestApplication.ContinuousProfiler.NativeDep")

add_library(${TARGET_NAME} SHARED
    callbacktest.cpp
)

set_target_properties(${TARGET_NAME} PROPERTIES PREFIX "")
