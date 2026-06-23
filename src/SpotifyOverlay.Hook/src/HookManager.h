#pragma once
#include <windows.h>

class HookManager {
public:
    static HookManager& GetInstance();
    
    bool Initialize();
    void Shutdown();

private:
    HookManager() = default;
    ~HookManager() = default;

    bool isInitialized = false;
};
