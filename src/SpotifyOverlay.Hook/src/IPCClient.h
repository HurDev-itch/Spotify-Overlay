#pragma once
#include <windows.h>
#include <string>
#include <thread>
#include <atomic>
#include <mutex>

// Mock of the Protobuf State for the renderer
struct NativeSpotifyState {
    std::string trackName;
    std::string artistName;
    bool isPlaying;
    int progressMs;
    int durationMs;
};

class IPCClient {
public:
    static IPCClient& GetInstance();

    bool Initialize();
    void Shutdown();

    // Sends a command to the C# Service
    void SendCommandPlayPause();
    void SendCommandNext();
    void SendCommandPrev();

    // Read the latest state
    NativeSpotifyState GetLatestState();

private:
    IPCClient() = default;
    ~IPCClient() = default;

    void ListenerThread();

    std::atomic<bool> isRunning{ false };
    std::thread workerThread;
    HANDLE hPipe = INVALID_HANDLE_VALUE;

    std::mutex stateMutex;
    NativeSpotifyState currentState;
};
