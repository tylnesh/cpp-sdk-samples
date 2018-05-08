# --------------------
# Environment
# --------------------
set(subProject mic)
project(${subProject})

# --------------------
# Dependencies
# --------------------
ADD_LIB(${subProject} affectiva-speech)
ADD_LIB(${subProject} PortAudio)
ADD_BOOST(${subProject} program_options)

# Apple-specific
IF(APPLE)
	set(FRAMEWORKS "-framework AudioToolbox -framework CoreAudio -framework Foundation -framework Cocoa")
endif()

# --------------------
# Build
# --------------------

file(GLOB SRCS RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} *.cpp)

file(GLOB HDRS RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} *.h ../headers/*.h)


add_executable(${subProject} ${SRCS} ${HDRS})
target_include_directories(${subProject} PRIVATE ${${subProject}_INCLUDE_DIRS} ${CMAKE_SOURCE_DIR}/headers)
target_link_libraries(${subProject} ${${subProject}_LIBS} ${FRAMEWORKS})

# Installation steps
install( TARGETS ${subProject}
        RUNTIME DESTINATION ${RUNTIME_INSTALL_DIRECTORY}
        LIBRARY DESTINATION ${LIBRARY_INSTALL_DIRECTORY}
        ARCHIVE DESTINATION ${ARCHIVE_INSTALL_DIRECTORY})
