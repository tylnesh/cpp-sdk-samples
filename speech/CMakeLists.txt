cmake_minimum_required (VERSION 2.6)

# load some useful macros
include(../cmake_modules/Macros.cmake)
set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${CMAKE_CURRENT_SOURCE_DIR}/../cmake_modules")

# --------------------
# Environment
# --------------------
set(rootProject speech_sdk_examples)
project(${rootProject})

option(BUILD_WAV "Build wav project" ON)
option(BUILD_MIC "Build mic project" ON)

# --------------------
# Build
# --------------------
CONFIG_COMPILER_11()
CONFIG_LINKER()
SETUP_INSTALL_DIRS()

if(USE_LISTENER)
    add_definitions(-DUSE_LISTENER)
endif()

if(NOT DEFINED AFFECTIVA_SDK_DIR )
    set(AFFECTIVA_SDK_DIR "" CACHE PATH "Install directory for the Affectiva SDK" )
    message(FATAL_ERROR "Please set AFFECTIVA_SDK_DIR to the directory where the Affectiva SDK is installed")
endif()

if(BUILD_WAV)
    add_subdirectory(wav)
endif()

if(BUILD_MIC)
    add_subdirectory(mic)
endif()
