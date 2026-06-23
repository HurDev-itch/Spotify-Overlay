#pragma once
#include "IRenderBackend.h"
#include <d3d11.h>
#include <imgui.h>

namespace SpotifyOverlay {

    class D3D11Renderer : public IRenderBackend {
    public:
        D3D11Renderer();
        ~D3D11Renderer() override;

        bool Initialize(HWND hwnd, void* pSwapChain) override;
        void Render(std::function<void()> drawCallback) override;
        void Shutdown() override;
        void OnResize(UINT width, UINT height) override;

    private:
        bool m_isInitialized = false;
        ID3D11Device* m_device = nullptr;
        ID3D11DeviceContext* m_context = nullptr;
        IDXGISwapChain* m_swapChain = nullptr;
        ID3D11RenderTargetView* m_renderTargetView = nullptr;
        HWND m_hwnd = nullptr;

        void CreateRenderTarget();
        void CleanupRenderTarget();
    };

}
