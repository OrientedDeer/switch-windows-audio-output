cmake_minimum_required(VERSION 3.20)
project(SwapAPO LANGUAGES CXX)

add_library(SwapAPO SHARED
    SwapAPO.cpp
    SwapAPO.h
    SwapAPO.def
)

target_compile_definitions(SwapAPO PRIVATE
    WIN32_LEAN_AND_MEAN
    UNICODE
    _UNICODE
)

target_link_libraries(SwapAPO PRIVATE
    ole32
    oleaut32
    uuid
    audiobaseprocessingobject
    audiomediatypecrt
)

set_target_properties(SwapAPO PROPERTIES
    CXX_STANDARD 17
    PREFIX ""
    OUTPUT_NAME "SwapAPO"
)
