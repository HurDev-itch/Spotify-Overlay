using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SpotifyOverlay.Core.Models;

namespace SpotifyOverlay.Core.Services.Lyrics
{
    public class LrclibLyricsProvider : ILyricsProvider
    {
        private readonly HttpClient _httpClient;

        public LrclibLyricsProvider()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SpotifyOverlay (https://github.com/SpotifyOverlay)");
        }

        public async Task<LyricsResult> GetLyricsAsync(string trackName, string artistName)
        {
            var result = new LyricsResult { IsFound = false, Source = "LRCLIB" };
            try
            {
                var uri = $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(trackName)}&artist_name={Uri.EscapeDataString(artistName)}";
                var response = await _httpClient.GetAsync(uri);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("syncedLyrics", out var syncedProp) && syncedProp.ValueKind == JsonValueKind.String)
                    {
                        result.RawLrc = syncedProp.GetString();
                        if (!string.IsNullOrWhiteSpace(result.RawLrc))
                        {
                            result.IsFound = true;
                            result.Lines = LyricsParser.ParseLrc(result.RawLrc);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.BackendLogger.Instance.Log("LRCLIB", $"Error fetching lyrics: {ex.Message}");
            }

            return result;
        }
    }
}
