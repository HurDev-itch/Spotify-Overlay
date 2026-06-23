#include <windows.h>
#include "HookManager.h"
#include "Logger.h"
#include <thread>

void SafeInitialize() {
    __try {
        HookManager::GetInstance().Initialize();
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        OutputDebugStringA("SEH Exception caught during HookManager::Initialize()!\n");
    }
}

void SafeShutdown() {
    __try {
        HookManager::GetInstance().Shutdown();
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        OutputDebugStringA("SEH Exception caught during HookManager::Shutdown()!\n");
    }
}

extern "C" {
    __declspec(dllexport) void InitializeOverlay()
    {
        SpotifyOverlay::Logger::Initialize();
        SpotifyOverlay::Logger::Info("InitializeOverlay called.");
        SafeInitialize();
    }

    __declspec(dllexport) void ShutdownOverlay()
    {
        SpotifyOverlay::Logger::Info("ShutdownOverlay called.");
        SafeShutdown();
    }
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        // Do NOT auto-initialize here to avoid loader lock issues and respect user roadmap
        break;
    case DLL_PROCESS_DETACH:
        // Optional: safety catch if ShutdownOverlay was not called
        break;
    }
    return TRUE;
}
