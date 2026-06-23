cmake_minimum_required(VERSION 3.20)
project(SpotifyOverlayHook VERSION 1.0.0 LANGUAGES CXX C)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include(FetchContent)

# Fetch MinHook
FetchContent_Declare(
    minhook
    GIT_REPOSITORY https://github.com/TsudaKageyu/minhook.git
    GIT_TAG        v1.3.3
)
FetchContent_GetProperties(minhook)
if(NOT minhook_POPULATED)
    FetchContent_Populate(minhook)
endif()

# Setup MinHook Sources
set(MINHOOK_SOURCES
    ${minhook_SOURCE_DIR}/src/buffer.c
    ${minhook_SOURCE_DIR}/src/hook.c
    ${minhook_SOURCE_DIR}/src/trampoline.c
    ${minhook_SOURCE_DIR}/src/hde/hde32.c
    ${minhook_SOURCE_DIR}/src/hde/hde64.c
)

# Fetch Dear ImGui
FetchContent_Declare(
    imgui
    GIT_REPOSITORY https://github.com/ocornut/imgui.git
    GIT_TAG        v1.90.4
)
FetchContent_GetProperties(imgui)
if(NOT imgui_POPULATED)
    FetchContent_Populate(imgui)
endif()

# Setup ImGui Sources
set(IMGUI_SOURCES
    ${imgui_SOURCE_DIR}/imgui.cpp
    ${imgui_SOURCE_DIR}/imgui_draw.cpp
    ${imgui_SOURCE_DIR}/imgui_tables.cpp
    ${imgui_SOURCE_DIR}/imgui_widgets.cpp
    ${imgui_SOURCE_DIR}/backends/imgui_impl_win32.cpp
    ${imgui_SOURCE_DIR}/backends/imgui_impl_dx11.cpp
)

# Source files
set(SOURCES
    src/dllmain.cpp
    src/HookManager.cpp
    src/DX11Hook.cpp
    src/OverlayRenderer.cpp
    src/IPCClient.cpp
    src/Logger.cpp
    src/Renderer/D3D11Renderer.cpp
)

# Shared library
add_library(SpotifyOverlayHook SHARED ${SOURCES} ${IMGUI_SOURCES} ${MINHOOK_SOURCES})

# Includes
target_include_directories(SpotifyOverlayHook PRIVATE 
    src
    ${imgui_SOURCE_DIR}
    ${imgui_SOURCE_DIR}/backends
    ${minhook_SOURCE_DIR}/include
)

# Link against standard windows libs
target_link_libraries(SpotifyOverlayHook PRIVATE 
    d3d11 
    d3dcompiler 
    dxgi
)
