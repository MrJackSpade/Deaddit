cmake_minimum_required(VERSION 3.22.1)
project(deadditbridge C)

# Reddit.Api compiled natively; built by native/build-android.sh into jniLibs.
add_library(redditapi SHARED IMPORTED)
set_target_properties(redditapi PROPERTIES IMPORTED_LOCATION
    ${CMAKE_SOURCE_DIR}/../jniLibs/${ANDROID_ABI}/libredditapi.so)

add_library(deadditbridge SHARED bridge.c)
target_link_libraries(deadditbridge redditapi log)
