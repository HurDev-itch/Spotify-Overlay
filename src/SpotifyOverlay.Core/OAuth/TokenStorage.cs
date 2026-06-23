using System;
using System.IO;
using System.Text.Json;
using SpotifyOverlay.Core.Logging;

namespace SpotifyOverlay.Core.OAuth
{
    public static class TokenStorage
    {
        private static readonly string TokenFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpotifyOverlay", "token.json");

        public static void SaveRefreshToken(string refreshToken)
        {
            try
            {
                var directory = Path.GetDirectoryName(TokenFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var data = new { RefreshToken = refreshToken };
                var json = JsonSerializer.Serialize(data);
                File.WriteAllText(TokenFilePath, json);
                Logger.Info(LogComponent.App, "Refresh token saved to disk.");
            }
            catch (Exception ex)
            {
                Logger.Error(LogComponent.App, $"Failed to save refresh token: {ex.Message}");
            }
        }

        public static string? LoadRefreshToken()
        {
            try
            {
                if (File.Exists(TokenFilePath))
                {
                    var json = File.ReadAllText(TokenFilePath);
                    var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("RefreshToken", out var tokenProp))
                    {
                        return tokenProp.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogComponent.App, $"Failed to load refresh token: {ex.Message}");
            }
            return null;
        }

        public static void ClearToken()
        {
            try
            {
                if (File.Exists(TokenFilePath))
                {
                    File.Delete(TokenFilePath);
                    Logger.Info(LogComponent.App, "Refresh token cleared.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogComponent.App, $"Failed to clear refresh token: {ex.Message}");
            }
        }
    }
}
