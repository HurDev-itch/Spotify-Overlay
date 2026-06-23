#include "DX11Hook.h"
#include <MinHook.h>
#include "OverlayRenderer.h"
#include <iostream>

using namespace SpotifyOverlay;

DX11Hook& DX11Hook::GetInstance() {
    static DX11Hook instance;
    return instance;
}

bool DX11Hook::Initialize() {
    if (isInitialized) return true;

    // Create a dummy swapchain to get the vtable addresses
    D3D_FEATURE_LEVEL featureLevel = D3D_FEATURE_LEVEL_11_0;
    DXGI_SWAP_CHAIN_DESC sd = {};
    sd.BufferCount = 1;
    sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    sd.OutputWindow = GetForegroundWindow();
    sd.SampleDesc.Count = 1;
    sd.Windowed = TRUE;
    sd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;

    IDXGISwapChain* dummySwapChain = nullptr;
    ID3D11Device* dummyDevice = nullptr;
    ID3D11DeviceContext* dummyContext = nullptr;

    if (FAILED(D3D11CreateDeviceAndSwapChain(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0, &featureLevel, 1, D3D11_SDK_VERSION, &sd, &dummySwapChain, &dummyDevice, nullptr, &dummyContext))) {
        return false;
    }

    void** pVTable = *reinterpret_cast<void***>(dummySwapChain);

    // Present is at index 8, ResizeBuffers is at index 13 in IDXGISwapChain vtable
    void* presentAddress = pVTable[8];
    void* resizeBuffersAddress = pVTable[13];

    if (MH_CreateHook(presentAddress, &HookedPresent, reinterpret_cast<void**>(&OriginalPresent)) != MH_OK) {
        return false;
    }

    if (MH_CreateHook(resizeBuffersAddress, &HookedResizeBuffers, reinterpret_cast<void**>(&OriginalResizeBuffers)) != MH_OK) {
        return false;
    }

    dummySwapChain->Release();
    dummyDevice->Release();
    dummyContext->Release();

    isInitialized = true;
    return true;
}

void DX11Hook::Shutdown() {
    if (!isInitialized) return;
    OverlayRenderer::GetInstance().Shutdown();
    if (pRenderTargetView) { pRenderTargetView->Release(); pRenderTargetView = nullptr; }
    isInitialized = false;
}

HRESULT WINAPI DX11Hook::HookedPresent(IDXGISwapChain* pSwapChain, UINT SyncInterval, UINT Flags) {
    auto& hook = GetInstance();

    if (!hook.pDevice) {
        pSwapChain->GetDevice(__uuidof(ID3D11Device), (void**)&hook.pDevice);
        hook.pDevice->GetImmediateContext(&hook.pContext);
        
        DXGI_SWAP_CHAIN_DESC sd;
        pSwapChain->GetDesc(&sd);
        hook.windowHwnd = sd.OutputWindow;

        OverlayRenderer::GetInstance().Initialize(hook.windowHwnd, pSwapChain);
    }

    OverlayRenderer::GetInstance().Render();

    return hook.OriginalPresent(pSwapChain, SyncInterval, Flags);
}

HRESULT WINAPI DX11Hook::HookedResizeBuffers(IDXGISwapChain* pSwapChain, UINT BufferCount, UINT Width, UINT Height, DXGI_FORMAT NewFormat, UINT SwapChainFlags) {
    auto& hook = GetInstance();
    
    HRESULT hr = hook.OriginalResizeBuffers(pSwapChain, BufferCount, Width, Height, NewFormat, SwapChainFlags);

    OverlayRenderer::GetInstance().OnResize(Width, Height);

    return hr;
}
