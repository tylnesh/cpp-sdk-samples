# --------------------
# Environment
# --------------------
set(subProject wav)
project(${subProject})

# --------------------
# Dependencies
# --------------------
ADD_LIB(${subProject} LibSndFile)
set(AffectivaSpeech_DIR ${AFFECTIVA_SDK_DIR})
FIND_AND_ADD_LIB(${subProject} AffectivaSpeech)

ADD_BOOST(${subProject} program_options)

# --------------------
# Build
# --------------------

file(GLOB SRCS RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} *.cpp)

file(GLOB HDRS RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} *.h ../headers/*.h)

add_executable(${subProject} ${SRCS} ${HDRS})
target_include_directories(${subProject} PRIVATE ${${subProject}_INCLUDE_DIRS} ${CMAKE_SOURCE_DIR}/headers)
target_link_libraries(${subProject} ${${subProject}_LIBS})

# Installation steps
install( TARGETS ${subProject}
        RUNTIME DESTINATION ${RUNTIME_INSTALL_DIRECTORY}
        LIBRARY DESTINATION ${LIBRARY_INSTALL_DIRECTORY}
        ARCHIVE DESTINATION ${ARCHIVE_INSTALL_DIRECTORY})
