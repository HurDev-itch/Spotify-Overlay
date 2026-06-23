#pragma once
#include <d3d11.h>
#include <dxgi.h>

class DX11Hook {
public:
    static DX11Hook& GetInstance();

    bool Initialize();
    void Shutdown();

private:
    DX11Hook() = default;
    ~DX11Hook() = default;

    static HRESULT WINAPI HookedPresent(IDXGISwapChain* pSwapChain, UINT SyncInterval, UINT Flags);
    static HRESULT WINAPI HookedResizeBuffers(IDXGISwapChain* pSwapChain, UINT BufferCount, UINT Width, UINT Height, DXGI_FORMAT NewFormat, UINT SwapChainFlags);

    typedef HRESULT(WINAPI* Present_t)(IDXGISwapChain*, UINT, UINT);
    typedef HRESULT(WINAPI* ResizeBuffers_t)(IDXGISwapChain*, UINT, UINT, UINT, DXGI_FORMAT, UINT);

    Present_t OriginalPresent = nullptr;
    ResizeBuffers_t OriginalResizeBuffers = nullptr;

    bool isInitialized = false;
    HWND windowHwnd = nullptr;
    ID3D11Device* pDevice = nullptr;
    ID3D11DeviceContext* pContext = nullptr;
    ID3D11RenderTargetView* pRenderTargetView = nullptr;
};
