#include "Logger.h"
#include <windows.h>
#include <fstream>
#include <iostream>
#include <chrono>
#include <iomanip>
#include <sstream>
#include <shlobj.h>

namespace SpotifyOverlay {

    void Logger::Initialize() {
        std::string logPath = GetLogFilePath();
        // Create directory if it doesn't exist
        char dirPath[MAX_PATH];
        strcpy_s(dirPath, logPath.c_str());
        char* lastSlash = strrchr(dirPath, '\\');
        if (lastSlash) {
            *lastSlash = '\0';
            // Simple create directory, won't handle recursive but sufficient here as C# app usually creates it
            CreateDirectoryA(dirPath, NULL);
        }
    }

    void Logger::Info(const std::string& message) {
        Log("INFO", message);
    }

    void Logger::Error(const std::string& message) {
        Log("ERROR", message);
    }

    void Logger::Log(const std::string& level, const std::string& message) {
        std::string logPath = GetLogFilePath();
        std::ofstream file(logPath, std::ios::app);
        if (file.is_open()) {
            file << "[" << GetTimestamp() << "] [" << level << "] " << message << std::endl;
        }
        
        // Also print to debug console
        std::string debugMsg = "[Overlay] [" + level + "] " + message + "\n";
        OutputDebugStringA(debugMsg.c_str());
    }

    std::string Logger::GetLogFilePath() {
        char path[MAX_PATH];
        if (SUCCEEDED(SHGetFolderPathA(NULL, CSIDL_APPDATA, NULL, 0, path))) {
            return std::string(path) + "\\SpotifyOverlay\\logs\\overlay.log";
        }
        return "overlay.log";
    }

    std::string Logger::GetTimestamp() {
        auto now = std::chrono::system_clock::now();
        auto in_time_t = std::chrono::system_clock::to_time_t(now);
        auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()) % 1000;

        std::stringstream ss;
        struct tm buf;
        localtime_s(&buf, &in_time_t);
        ss << std::put_time(&buf, "%Y-%m-%d %H:%M:%S") << '.' << std::setfill('0') << std::setw(3) << ms.count();
        return ss.str();
    }
}
