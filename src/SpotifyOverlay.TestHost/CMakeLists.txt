cmake_minimum_required(VERSION 3.15)
project(SpotifyOverlayTestHost)

set(CMAKE_CXX_STANDARD 17)

add_executable(SpotifyOverlay.TestHost WIN32 main.cpp)

target_link_libraries(SpotifyOverlay.TestHost d3d11 d3dcompiler dxgi)

# Copy the Hook DLL to the TestHost output directory for easy loading
add_custom_command(TARGET SpotifyOverlay.TestHost POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
    "${CMAKE_CURRENT_SOURCE_DIR}/../SpotifyOverlay.Hook/build/Release/SpotifyOverlayHook.dll"
    $<TARGET_FILE_DIR:SpotifyOverlay.TestHost>
)
