using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SpotifyOverlay.Core.Models;

namespace SpotifyOverlay.Core.Services
{
    public class ThemeService
    {
        private static ThemeService _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        private readonly string _settingsFilePath;
        public Settings CurrentSettings { get; private set; }

        private ThemeService()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(localAppData, "SpotifyOverlay");
            Directory.CreateDirectory(appFolder);
            _settingsFilePath = Path.Combine(appFolder, "settings.json");

            LoadSettings();
        }

        public void LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    CurrentSettings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                }
                catch (Exception ex)
                {
                    Logging.BackendLogger.Instance.Log("THEME", $"Failed to load settings: {ex.Message}");
                    CurrentSettings = new Settings();
                }
            }
            else
            {
                CurrentSettings = new Settings();
                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(CurrentSettings, options);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Logging.BackendLogger.Instance.Log("THEME", $"Failed to save settings: {ex.Message}");
            }
        }

        public string GetSettingsJson()
        {
            return JsonSerializer.Serialize(CurrentSettings);
        }
    }
}
