#include "HookManager.h"
#include <MinHook.h>
#include "DX11Hook.h"
#include <iostream>

HookManager& HookManager::GetInstance() {
    static HookManager instance;
    return instance;
}

bool HookManager::Initialize() {
    if (isInitialized) return true;

    if (MH_Initialize() != MH_OK) {
        std::cerr << "[HookManager] Failed to initialize MinHook.\n";
        return false;
    }

    if (!DX11Hook::GetInstance().Initialize()) {
        std::cerr << "[HookManager] Failed to initialize DX11 Hook.\n";
        // Do not fail completely, maybe they are running DX12 or OpenGL.
    }

    if (MH_EnableHook(MH_ALL_HOOKS) != MH_OK) {
        std::cerr << "[HookManager] Failed to enable hooks.\n";
        return false;
    }

    isInitialized = true;
    return true;
}

void HookManager::Shutdown() {
    if (!isInitialized) return;
    
    MH_DisableHook(MH_ALL_HOOKS);
    DX11Hook::GetInstance().Shutdown();
    MH_Uninitialize();
    
    isInitialized = false;
}
