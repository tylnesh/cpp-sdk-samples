cmake_minimum_required (VERSION 2.6)

# load some useful macros
include(../cmake_modules/Macros.cmake)

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

if(BUILD_WAV)
    add_subdirectory(wav)
endif()

if(BUILD_MIC)
    add_subdirectory(mic)
endif()
