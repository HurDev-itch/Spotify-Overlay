#pragma once
#include <string>

namespace SpotifyOverlay {

    class Logger {
    public:
        static void Initialize();
        static void Info(const std::string& message);
        static void Error(const std::string& message);

    private:
        static void Log(const std::string& level, const std::string& message);
        static std::string GetLogFilePath();
        static std::string GetTimestamp();
    };

}
