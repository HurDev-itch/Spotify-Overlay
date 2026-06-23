#include "D3D11Renderer.h"
#include <backends/imgui_impl_win32.h>
#include <backends/imgui_impl_dx11.h>
#include "../Logger.h"

namespace SpotifyOverlay {

    D3D11Renderer::D3D11Renderer() {}
    D3D11Renderer::~D3D11Renderer() { Shutdown(); }

    bool D3D11Renderer::Initialize(HWND hwnd, void* pSwapChain) {
        if (m_isInitialized) return true;

        m_hwnd = hwnd;
        m_swapChain = static_cast<IDXGISwapChain*>(pSwapChain);

        if (!m_swapChain) {
            Logger::Error("D3D11Renderer::Initialize failed: Null swap chain");
            return false;
        }

        if (FAILED(m_swapChain->GetDevice(__uuidof(ID3D11Device), (void**)&m_device))) {
            Logger::Error("D3D11Renderer::Initialize failed: Cannot get device");
            return false;
        }
        
        m_device->GetImmediateContext(&m_context);

        CreateRenderTarget();

        IMGUI_CHECKVERSION();
        ImGui::CreateContext();
        
        ImGui::StyleColorsDark();
        ImGuiStyle& style = ImGui::GetStyle();
        style.WindowRounding = 8.0f;
        style.Colors[ImGuiCol_WindowBg].w = 0.85f;

        ImGui_ImplWin32_Init(hwnd);
        ImGui_ImplDX11_Init(m_device, m_context);

        m_isInitialized = true;
        Logger::Info("D3D11Renderer Initialized successfully.");
        return true;
    }

    void D3D11Renderer::CreateRenderTarget() {
        ID3D11Texture2D* pBackBuffer = nullptr;
        m_swapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), (LPVOID*)&pBackBuffer);
        if (pBackBuffer) {
            m_device->CreateRenderTargetView(pBackBuffer, nullptr, &m_renderTargetView);
            pBackBuffer->Release();
        }
    }

    void D3D11Renderer::CleanupRenderTarget() {
        if (m_renderTargetView) {
            m_renderTargetView->Release();
            m_renderTargetView = nullptr;
        }
    }

    void D3D11Renderer::Render(std::function<void()> drawCallback) {
        if (!m_isInitialized) return;

        ImGui_ImplDX11_NewFrame();
        ImGui_ImplWin32_NewFrame();
        ImGui::NewFrame();

        if (drawCallback) {
            drawCallback();
        }

        ImGui::Render();
        m_context->OMSetRenderTargets(1, &m_renderTargetView, nullptr);
        ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());
    }

    void D3D11Renderer::Shutdown() {
        if (!m_isInitialized) return;

        ImGui_ImplDX11_Shutdown();
        ImGui_ImplWin32_Shutdown();
        ImGui::DestroyContext();

        CleanupRenderTarget();

        if (m_context) { m_context->Release(); m_context = nullptr; }
        if (m_device) { m_device->Release(); m_device = nullptr; }

        m_isInitialized = false;
        Logger::Info("D3D11Renderer Shutdown successfully.");
    }

    void D3D11Renderer::OnResize(UINT width, UINT height) {
        CleanupRenderTarget();
        m_swapChain->ResizeBuffers(0, width, height, DXGI_FORMAT_UNKNOWN, 0);
        CreateRenderTarget();
    }
}
