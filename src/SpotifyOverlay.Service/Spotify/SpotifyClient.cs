using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SpotifyOverlay.Core.OAuth;
using SpotifyOverlay.IPC;

using SpotifyOverlay.Core.Logging;

namespace SpotifyOverlay.Service.Spotify
{
    public class SpotifyClient : ISpotifyProvider
    {
        private readonly OAuthManager _oauthManager;
        private readonly AlbumArtworkCache _artworkCache;
        private readonly HttpClient _httpClient;
        private SpotifyState? _lastState;

        // Events for the Event-Driven architecture
        public event EventHandler<SpotifyState>? TrackChanged;
        public event EventHandler<SpotifyState>? PlaybackPaused;
        public event EventHandler<SpotifyState>? PlaybackResumed;
        public event EventHandler<SpotifyState>? VolumeChanged;

        public SpotifyClient(OAuthManager oauthManager, AlbumArtworkCache artworkCache)
        {
            _oauthManager = oauthManager;
            _artworkCache = artworkCache;
            _httpClient = new HttpClient();
        }

        public async Task StartPollingAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!string.IsNullOrEmpty(_oauthManager.AccessToken))
                {
                    await PollPlaybackStateAsync();
                }
                
                // Poll every 2 seconds. Will be adjusted dynamically in a production scenario.
                await Task.Delay(2000, cancellationToken);
            }
        }

        private async Task PollPlaybackStateAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _oauthManager.AccessToken);

            var response = await _httpClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await _oauthManager.RefreshTokenAsync();
                return;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                // Nothing is playing
                return;
            }

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("item", out var item) || item.ValueKind == JsonValueKind.Null) return;

                var isPlaying = root.GetProperty("is_playing").GetBoolean();
                var progressMs = root.GetProperty("progress_ms").GetInt32();
                var trackId = item.GetProperty("id").GetString() ?? string.Empty;
                var trackName = item.GetProperty("name").GetString() ?? string.Empty;
                var durationMs = item.GetProperty("duration_ms").GetInt32();
                
                var artistName = item.GetProperty("artists")[0].GetProperty("name").GetString() ?? string.Empty;
                var albumName = item.GetProperty("album").GetProperty("name").GetString() ?? string.Empty;
                
                var images = item.GetProperty("album").GetProperty("images");
                var artworkUrl = string.Empty;
                if (images.GetArrayLength() > 0)
                {
                    artworkUrl = images[0].GetProperty("url").GetString() ?? string.Empty;
                }

                var device = root.GetProperty("device");
                var volume = device.TryGetProperty("volume_percent", out var volProp) && volProp.ValueKind != JsonValueKind.Null ? volProp.GetInt32() : 100;
                
                var isShuffle = root.GetProperty("shuffle_state").GetBoolean();

                var artworkPath = await _artworkCache.GetOrDownloadArtworkAsync(artworkUrl);

                var newState = new SpotifyState
                {
                    TrackId = trackId,
                    TrackName = trackName,
                    ArtistName = artistName,
                    AlbumName = albumName,
                    DurationMs = durationMs,
                    ProgressMs = progressMs,
                    IsPlaying = isPlaying,
                    Volume = volume,
                    IsShuffle = isShuffle,
                    ArtworkPath = artworkPath
                };

                DetectAndFireEvents(newState);
                _lastState = newState;
            }
        }

        private void DetectAndFireEvents(SpotifyState newState)
        {
            if (_lastState == null)
            {
                TrackChanged?.Invoke(this, newState);
                return;
            }

            if (_lastState.TrackId != newState.TrackId)
            {
                TrackChanged?.Invoke(this, newState);
            }

            if (_lastState.IsPlaying != newState.IsPlaying)
            {
                if (newState.IsPlaying)
                    PlaybackResumed?.Invoke(this, newState);
                else
                    PlaybackPaused?.Invoke(this, newState);
            }

            if (_lastState.Volume != newState.Volume)
            {
                VolumeChanged?.Invoke(this, newState);
            }
        }
    }
}
