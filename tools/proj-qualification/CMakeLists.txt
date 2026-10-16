cmake_minimum_required(VERSION 3.22)
project(GeoXProjQualification LANGUAGES C CXX)
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(CMAKE_CXX_VISIBILITY_PRESET hidden)
set(CMAKE_C_VISIBILITY_PRESET hidden)
set(CMAKE_VISIBILITY_INLINES_HIDDEN ON)

# A bounded offline qualification build, not a production Unity dependency.
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
set(BUILD_TESTING OFF CACHE BOOL "" FORCE)
set(BUILD_APPS OFF CACHE BOOL "" FORCE)
set(ENABLE_CURL OFF CACHE BOOL "" FORCE)
set(ENABLE_TIFF OFF CACHE BOOL "" FORCE)
set(EMBED_PROJ_DATA_PATH OFF CACHE BOOL "" FORCE)
set(EMBED_RESOURCE_FILES OFF CACHE BOOL "" FORCE)
set(TESTING_USE_NETWORK OFF CACHE BOOL "" FORCE)
add_subdirectory("${PROJ_SOURCE_DIR}" proj)

add_library(geox_proj_probe SHARED probe.cpp)
target_link_libraries(geox_proj_probe PRIVATE PROJ::proj)
target_include_directories(geox_proj_probe PRIVATE "${PROJ_SOURCE_DIR}/src")
if(ANDROID)
  # NDK r27 requires explicit flexible-page support; tested with readelf below.
  target_link_options(geox_proj_probe PRIVATE "-Wl,-z,max-page-size=16384"
    "-Wl,--version-script=${CMAKE_CURRENT_SOURCE_DIR}/exports.map")
endif()
