#pragma once
#include <memory>
#include <windows.h>
#include "Renderer/IRenderBackend.h"

namespace SpotifyOverlay {

    class OverlayRenderer {
    public:
        static OverlayRenderer& GetInstance();

        void Initialize(HWND hwnd, void* pSwapChain);
        void Render();
        void Shutdown();
        void OnResize(UINT width, UINT height);

    private:
        OverlayRenderer();
        ~OverlayRenderer();

        std::unique_ptr<IRenderBackend> m_backend;
        bool isInitialized = false;

        // Performance Metrics
        float m_fps = 0.0f;
        float m_frameTimeMs = 0.0f;
        float m_renderCostMs = 0.0f;
        
        void DrawImGui();
    };

}
