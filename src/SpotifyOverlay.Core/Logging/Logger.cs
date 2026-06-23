using System;
using System.IO;

namespace SpotifyOverlay.Core.Logging
{
    public enum LogComponent
    {
        App,
        Service,
        Overlay,
        Focus,
        Visibility,
        WebSocket
    }

    public static class Logger
    {
        private static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpotifyOverlay", "logs");

        private static readonly object _lock = new object();

        static Logger()
        {
            if (!Directory.Exists(LogDir))
            {
                Directory.CreateDirectory(LogDir);
            }
        }

        public static void Log(LogComponent component, string message, bool isError = false)
        {
            string fileName = component switch
            {
                LogComponent.App => "app.log",
                LogComponent.Service => "service.log",
                LogComponent.Overlay => "overlay.log",
                LogComponent.Focus => "focus.log",
                LogComponent.Visibility => "visibility.log",
                LogComponent.WebSocket => "websocket.log",
                _ => "app.log"
            };

            string path = Path.Combine(LogDir, fileName);
            string level = isError ? "ERROR" : "INFO";
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

            System.Diagnostics.Debug.WriteLine($"[{component}] {logLine}");

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(path, logLine + Environment.NewLine);
                }
                catch
                {
                    // Ignore logging errors to prevent crash loops
                }
            }
        }

        public static void Info(LogComponent component, string message) => Log(component, message, false);
        public static void Error(LogComponent component, string message) => Log(component, message, true);
        public static void Error(LogComponent component, Exception ex) => Log(component, ex.ToString(), true);
    }
}
