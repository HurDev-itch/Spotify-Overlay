using System;
using System.IO;
using System.Threading.Tasks;
using SpotifyOverlay.Core.Models;
using SpotifyOverlay.Core.Services.Lyrics;

namespace SpotifyOverlay.Core.Services
{
    public class LyricsService
    {
        private static LyricsService _instance;
        public static LyricsService Instance => _instance ??= new LyricsService();

        private readonly ILyricsProvider _provider;
        private readonly string _cacheFolder;

        private LyricsService()
        {
            _provider = new LrclibLyricsProvider();
            
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _cacheFolder = Path.Combine(localAppData, "SpotifyOverlay", "Cache", "Lyrics");
            Directory.CreateDirectory(_cacheFolder);
        }

        public async Task<LyricsResult> GetLyricsAsync(string trackName, string artistName)
        {
            if (string.IsNullOrWhiteSpace(trackName) || string.IsNullOrWhiteSpace(artistName))
                return new LyricsResult { IsFound = false };

            var safeArtist = SanitizeFileName(artistName);
            var safeTrack = SanitizeFileName(trackName);
            var fileName = $"{safeArtist} - {safeTrack}.lrc";
            var filePath = Path.Combine(_cacheFolder, fileName);

            // Check cache
            if (File.Exists(filePath))
            {
                try
                {
                    var cachedLrc = await File.ReadAllTextAsync(filePath);
                    return new LyricsResult 
                    { 
                        IsFound = true, 
                        Source = "LOCAL_CACHE", 
                        RawLrc = cachedLrc, 
                        Lines = LyricsParser.ParseLrc(cachedLrc) 
                    };
                }
                catch (Exception ex)
                {
                    Logging.BackendLogger.Instance.Log("LYRICS", $"Cache read error: {ex.Message}");
                }
            }

            // Fetch from provider
            var result = await _provider.GetLyricsAsync(trackName, artistName);
            
            // Save to cache
            if (result.IsFound && !string.IsNullOrWhiteSpace(result.RawLrc))
            {
                try
                {
                    await File.WriteAllTextAsync(filePath, result.RawLrc);
                }
                catch (Exception ex)
                {
                    Logging.BackendLogger.Instance.Log("LYRICS", $"Cache write error: {ex.Message}");
                }
            }

            return result;
        }

        private string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = name;
            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            return sanitized;
        }
    }
}
