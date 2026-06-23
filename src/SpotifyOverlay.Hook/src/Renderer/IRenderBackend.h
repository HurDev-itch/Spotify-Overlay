#pragma once
#include <windows.h>
#include <functional>

namespace SpotifyOverlay {

    class IRenderBackend {
    public:
        virtual ~IRenderBackend() = default;

        // Initialize the backend using a target window handle or swap chain pointer
        virtual bool Initialize(HWND hwnd, void* pSwapChain) = 0;

        // Render the ImGui frame
        // The callback provides the user a place to issue ImGui:: rendering calls
        virtual void Render(std::function<void()> drawCallback) = 0;

        // Shutdown and release resources
        virtual void Shutdown() = 0;

        // Called when the window or swap chain resizes
        virtual void OnResize(UINT width, UINT height) = 0;
    };

}
