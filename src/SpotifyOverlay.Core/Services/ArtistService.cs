using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SpotifyOverlay.Core.Spotify;
using SpotifyOverlay.Core.Spotify.Models;

namespace SpotifyOverlay.Core.Services
{
    public class ArtistService
    {
        private static readonly Lazy<ArtistService> _instance = new(() => new ArtistService());
        public static ArtistService Instance => _instance.Value;

        private readonly string _cacheDirectory;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);

        private ArtistService()
        {
            _cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SpotifyOverlay",
                "Cache",
                "Artists"
            );

            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        public async Task<ArtistDetailsData> GetArtistDetailsAsync(SpotifyClient client, string artistId)
        {
            var cacheFile = Path.Combine(_cacheDirectory, $"artist_{artistId}.json");

            // Check cache
            if (File.Exists(cacheFile))
            {
                var fileInfo = new FileInfo(cacheFile);
                if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc < _cacheExpiration)
                {
                    try
                    {
                        var cachedJson = await File.ReadAllTextAsync(cacheFile);
                        var cachedData = JsonSerializer.Deserialize<ArtistDetailsData>(cachedJson);
                        if (cachedData != null && cachedData.Artist != null && cachedData.Tracks != null)
                        {
                            return cachedData;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.BackendLogger.Instance.Log("ArtistService", $"Failed to read cache for artist {artistId}: {ex.Message}");
                    }
                }
            }

            // Fetch from Spotify
            Logging.BackendLogger.Instance.Log("ArtistService", $"Fetching artist {artistId} from Spotify API");
            var artistDto = await client.GetArtistAsync(artistId);
            if (artistDto == null)
            {
                return null;
            }

            var tracks = await client.GetArtistTopTracksAsync(artistDto.Name);

            var data = new ArtistDetailsData
            {
                Artist = artistDto,
                Tracks = tracks ?? new List<Track>()
            };

            // Save to cache
            try
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(cacheFile, json);
            }
            catch (Exception ex)
            {
                Logging.BackendLogger.Instance.Log("ArtistService", $"Failed to write cache for artist {artistId}: {ex.Message}");
            }

            return data;
        }
    }
}
