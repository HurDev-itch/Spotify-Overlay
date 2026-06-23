#include "OverlayRenderer.h"
#include <imgui.h>
#include "IPCClient.h"
#include "Renderer/D3D11Renderer.h"
#include "Logger.h"
#include <chrono>

namespace SpotifyOverlay {

    OverlayRenderer& OverlayRenderer::GetInstance() {
        static OverlayRenderer instance;
        return instance;
    }

    OverlayRenderer::OverlayRenderer() {
        m_backend = std::make_unique<D3D11Renderer>();
    }

    OverlayRenderer::~OverlayRenderer() {
        Shutdown();
    }

    void OverlayRenderer::Initialize(HWND hwnd, void* pSwapChain) {
        if (isInitialized) return;

        if (m_backend->Initialize(hwnd, pSwapChain)) {
            IPCClient::GetInstance().Initialize();
            isInitialized = true;
            Logger::Info("OverlayRenderer Initialized.");
        } else {
            Logger::Error("Failed to initialize Render Backend.");
        }
    }

    void OverlayRenderer::Render() {
        if (!isInitialized) return;

        auto startRender = std::chrono::high_resolution_clock::now();

        m_backend->Render([this]() { DrawImGui(); });

        auto endRender = std::chrono::high_resolution_clock::now();
        m_renderCostMs = std::chrono::duration<float, std::milli>(endRender - startRender).count();
    }

    void OverlayRenderer::DrawImGui() {
        NativeSpotifyState state = IPCClient::GetInstance().GetLatestState();

        // Calculate global FPS and FrameTime from ImGui
        m_fps = ImGui::GetIO().Framerate;
        m_frameTimeMs = 1000.0f / m_fps;

        ImGui::SetNextWindowSize(ImVec2(350, 150), ImGuiCond_FirstUseEver);
        if (ImGui::Begin("Spotify Overlay", nullptr, ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoResize)) {
            ImGui::TextColored(ImVec4(0.11f, 0.72f, 0.32f, 1.0f), "Spotify Mini Player");
            ImGui::Separator();
            
            ImGui::Text("Track: %s", state.trackName.c_str());
            ImGui::Text("Artist: %s", state.artistName.c_str());
            
            ImGui::Separator();

            if (ImGui::Button("Prev")) IPCClient::GetInstance().SendCommandPrev();
            ImGui::SameLine();
            if (ImGui::Button(state.isPlaying ? "Pause" : "Play")) IPCClient::GetInstance().SendCommandPlayPause();
            ImGui::SameLine();
            if (ImGui::Button("Next")) IPCClient::GetInstance().SendCommandNext();

            ImGui::Separator();
            // Performance Diagnostics Panel
            ImGui::TextColored(ImVec4(0.5f, 0.5f, 0.5f, 1.0f), "Diagnostics:");
            ImGui::TextColored(ImVec4(0.7f, 0.7f, 0.7f, 1.0f), "FPS: %.1f (%.2f ms)", m_fps, m_frameTimeMs);
            ImGui::TextColored(ImVec4(0.7f, 0.7f, 0.7f, 1.0f), "Render Cost: %.3f ms", m_renderCostMs);
        }
        ImGui::End();
    }

    void OverlayRenderer::Shutdown() {
        if (!isInitialized) return;
        IPCClient::GetInstance().Shutdown();
        m_backend->Shutdown();
        isInitialized = false;
        Logger::Info("OverlayRenderer Shutdown.");
    }

    void OverlayRenderer::OnResize(UINT width, UINT height) {
        if (isInitialized) m_backend->OnResize(width, height);
    }
}
