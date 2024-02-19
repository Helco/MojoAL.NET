cmake_minimum_required(VERSION 3.16)
if (NOT "${CMAKE_TOOLCHAIN_FILE}" STREQUAL "")
    set(VCPKG_CHAINLOAD_TOOLCHAIN_FILE "${CMAKE_TOOLCHAIN_FILE}")
endif()
if ("$ENV{VCPKG_INSTALLATION_ROOT}" STREQUAL "")
    message(FATAL_ERROR "VCPKG_INSTALLATION_ROOT is not set")
    return()
endif()
set(CMAKE_TOOLCHAIN_FILE "$ENV{VCPKG_INSTALLATION_ROOT}/scripts/buildsystems/vcpkg.cmake")

project(MojoAL C)

# By default, we build in Release mode.
if(NOT CMAKE_BUILD_TYPE)
    set(
        CMAKE_BUILD_TYPE "Release" CACHE
        STRING "Choose the type of build." FORCE
    )
endif()

# Build options
# =============

option(MOJOAL_BUILD_SHARED_LIBS "Build MojoAL shared library" OFF)
option(MOJOAL_BUILD_STATIC_LIBS "Build MojoAL static library" OFF)

message(STATUS "Build shared libraries: ${MOJOAL_BUILD_SHARED_LIBS}")
message(STATUS "Build static libraries: ${MOJOAL_BUILD_STATIC_LIBS}")

# Targets and link
# ================

include(CheckCXXCompilerFlag)

macro(mojoal_create_target target_name linkage output_name)
    string(TOUPPER "${linkage}" linkage_upper)

    if(NOT ${linkage_upper} MATCHES "^(STATIC|SHARED)$")
        message(FATAL_ERROR "Invalid linkage: ${linkage}")
    endif()

    add_library(
        ${target_name} ${linkage_upper}
        MojoAL/mojoal.c
    )

    target_include_directories(
        ${target_name}
        PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/MojoAL/>
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/MojoAL/AL/>
    )

    if(${linkage_upper} STREQUAL "STATIC")
        target_compile_definitions(${target_name} PUBLIC AL_LIBTYPE_STATIC)
    endif()

    find_package(SDL2 CONFIG REQUIRED)
    target_link_libraries(${target_name} PRIVATE SDL2::SDL2)

endmacro()

set(mojoal_targets "")

if(MOJOAL_BUILD_SHARED_LIBS)
    mojoal_create_target(mojoal SHARED mojoal)
    list(APPEND mojoal_targets mojoal)

    #add_executable(testpos MojoAL/tests/testposition.c)
    #target_link_libraries(testpos PRIVATE SDL2::SDL2 SDL2::SDL2main mojoal)
endif()

if(MOJOAL_BUILD_STATIC_LIBS)
    # On Windows, a static library should use a different output name
    # to avoid name conflicts with the shared library.
    if(WIN32)
        mojoal_create_target(mojoal-static STATIC mojoal_static)
    else()
        mojoal_create_target(mojoal-static STATIC mojoal)
    endif()

    list(APPEND mojoal_targets mojoal-static)
endif()

# Tests
# =====

# TODO: implement a test suite.
