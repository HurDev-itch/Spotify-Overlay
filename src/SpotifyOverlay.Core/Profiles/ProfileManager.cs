using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SpotifyOverlay.Core.Profiles
{
    public class OverlayProfile
    {
        public string ProcessName { get; set; } = "default";
        public double Opacity { get; set; } = 1.0;
        public float Scale { get; set; } = 1.0f;
        public int PositionX { get; set; } = 10;
        public int PositionY { get; set; } = 10;
        public bool ShowOnTrackChange { get; set; } = true;
    }

    public class ProfileManager
    {
        private readonly string _profilesDirectory;
        private readonly Dictionary<string, OverlayProfile> _profilesCache = new(StringComparer.OrdinalIgnoreCase);

        public ProfileManager()
        {
            _profilesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpotifyOverlay", "Profiles");
            if (!Directory.Exists(_profilesDirectory))
            {
                Directory.CreateDirectory(_profilesDirectory);
            }
        }

        public OverlayProfile GetProfileForProcess(string processName)
        {
            if (_profilesCache.TryGetValue(processName, out var profile))
            {
                return profile;
            }

            var filePath = Path.Combine(_profilesDirectory, $"{processName}.json");
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var loadedProfile = JsonSerializer.Deserialize<OverlayProfile>(json);
                    if (loadedProfile != null)
                    {
                        _profilesCache[processName] = loadedProfile;
                        return loadedProfile;
                    }
                }
                catch { /* fallback to default */ }
            }

            var defaultProfile = new OverlayProfile { ProcessName = processName };
            _profilesCache[processName] = defaultProfile;
            return defaultProfile;
        }

        public void SaveProfile(OverlayProfile profile)
        {
            _profilesCache[profile.ProcessName] = profile;
            var filePath = Path.Combine(_profilesDirectory, $"{profile.ProcessName}.json");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
