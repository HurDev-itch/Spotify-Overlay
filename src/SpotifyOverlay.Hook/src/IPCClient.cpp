#include "IPCClient.h"
#include <iostream>

IPCClient& IPCClient::GetInstance() {
    static IPCClient instance;
    return instance;
}

bool IPCClient::Initialize() {
    if (isRunning) return true;
    isRunning = true;
    
    // Set some default state
    currentState.trackName = "Waiting for Spotify...";
    currentState.artistName = "";
    currentState.isPlaying = false;
    
    workerThread = std::thread(&IPCClient::ListenerThread, this);
    return true;
}

void IPCClient::Shutdown() {
    if (!isRunning) return;
    isRunning = false;
    
    if (hPipe != INVALID_HANDLE_VALUE) {
        CloseHandle(hPipe);
        hPipe = INVALID_HANDLE_VALUE;
    }

    if (workerThread.joinable()) {
        workerThread.join();
    }
}

void IPCClient::ListenerThread() {
    while (isRunning) {
        hPipe = CreateFileA(
            "\\\\.\\pipe\\SpotifyOverlayIPC",
            GENERIC_READ | GENERIC_WRITE,
            0,
            NULL,
            OPEN_EXISTING,
            0,
            NULL
        );

        if (hPipe != INVALID_HANDLE_VALUE) {
            // Connected! Read loop for incoming Protobuf messages.
            // In a production build, this would read the length prefix, then the bytes,
            // and call SpotifyOverlay::IPC::ServiceEvent::ParseFromArray().
            
            DWORD bytesRead;
            char buffer[1024];
            while (isRunning && ReadFile(hPipe, buffer, sizeof(buffer), &bytesRead, NULL)) {
                // Parse Protobuf and update currentState safely...
                // (Mocking the state update for the UI)
                std::lock_guard<std::mutex> lock(stateMutex);
                currentState.trackName = "Connected to Service!";
                currentState.artistName = "Ready";
            }
            CloseHandle(hPipe);
            hPipe = INVALID_HANDLE_VALUE;
        }

        std::this_thread::sleep_for(std::chrono::milliseconds(1000));
    }
}

void IPCClient::SendCommandPlayPause() {
    // In production: Serialize OverlayCommand protobuf and WriteFile to hPipe
}

void IPCClient::SendCommandNext() {
    // In production: Serialize OverlayCommand protobuf and WriteFile to hPipe
}

void IPCClient::SendCommandPrev() {
    // In production: Serialize OverlayCommand protobuf and WriteFile to hPipe
}

NativeSpotifyState IPCClient::GetLatestState() {
    std::lock_guard<std::mutex> lock(stateMutex);
    return currentState;
}
