using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SpotifyOverlay.Core.Spotify.Models;
using SpotifyOverlay.Core.Spotify.Protocol;

namespace SpotifyOverlay.Core.Spotify
{
    public class SpotifyStateService
    {
        private readonly SpotifyClient _client;
        private readonly IProtocolSerializer _serializer;
        private Action<string> _broadcastAction;
        
        private string _currentTrackIdForLyrics;
        
        // Simple cache
        private List<Playlist> _cachedPlaylists;
        private DateTime _playlistsCacheTime;

        private CancellationTokenSource _pollingCts;

        public SpotifyStateService(SpotifyClient client, IProtocolSerializer serializer)
        {
            _client = client;
            _serializer = serializer;
            StartPolling();
        }

        private void StartPolling()
        {
            _pollingCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                while (!_pollingCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var playback = await _client.GetCurrentPlaybackAsync();
                        if (playback != null)
                        {
                            SpotifyOverlay.Core.Services.NotificationService.Instance.HandlePlaybackStateChange(playback);
                            var json = _serializer.SerializePlaybackState(playback);
                            BroadcastJson(json);
                            
                            // Check for lyrics if track changed
                            if (playback.CurrentTrack != null && playback.CurrentTrack.Id != _currentTrackIdForLyrics)
                            {
                                _currentTrackIdForLyrics = playback.CurrentTrack.Id;
                                _ = FetchAndBroadcastLyricsAsync(playback.CurrentTrack.Name, playback.CurrentTrack.Artist);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error polling playback: {ex.Message}");
                    }
                    await Task.Delay(1000, _pollingCts.Token);
                }
            });
        }

        public void SetBroadcastAction(Action<string> broadcastAction)
        {
            _broadcastAction = broadcastAction;
        }

        private async Task FetchAndBroadcastLyricsAsync(string trackName, string artistName)
        {
            var result = await SpotifyOverlay.Core.Services.LyricsService.Instance.GetLyricsAsync(trackName, artistName);
            var obj = new { type = "lyrics", data = result };
            var json = JsonSerializer.Serialize(obj);
            BroadcastJson(json);
        }

        private void BroadcastJson(string json)
        {
            _broadcastAction?.Invoke(json);
        }

        private void SendError(string code, string message)
        {
            var json = _serializer.SerializeError(code, message);
            BroadcastJson(json);
        }

        public async Task HandleCommandAsync(string jsonCommand)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonCommand);
                var root = doc.RootElement;
                if (!root.TryGetProperty("command", out var cmdProp)) return;

                var command = cmdProp.GetString();

                switch (command)
                {
                    case "search":
                        if (root.TryGetProperty("query", out var queryProp))
                        {
                            var query = queryProp.GetString();
                            var results = await _client.SearchAsync(query);
                            BroadcastJson(_serializer.SerializeSearch(results));
                        }
                        break;

                    case "get_playlists":
                        // Cache for 5 minutes
                        if (_cachedPlaylists == null || (DateTime.UtcNow - _playlistsCacheTime).TotalMinutes > 5)
                        {
                            _cachedPlaylists = await _client.GetUserPlaylistsAsync();
                            _playlistsCacheTime = DateTime.UtcNow;
                        }
                        BroadcastJson(_serializer.SerializePlaylists(_cachedPlaylists));
                        break;

                    case "get_playlist_tracks":
                        if (root.TryGetProperty("playlist_id", out var playlistIdProp))
                        {
                            var playlistId = playlistIdProp.GetString();
                            int offset = 0;
                            if (root.TryGetProperty("offset", out var offsetProp) && offsetProp.ValueKind == JsonValueKind.Number)
                            {
                                offset = offsetProp.GetInt32();
                            }
                            var data = await _client.GetPlaylistTracksAsync(playlistId, offset, 50);
                            BroadcastJson(_serializer.SerializePlaylistTracks(playlistId, data));
                        }
                        break;

                    case "get_artist_details":
                        if (root.TryGetProperty("artist_id", out var artistIdProp))
                        {
                            var artistId = artistIdProp.GetString();
                            var data = await SpotifyOverlay.Core.Services.ArtistService.Instance.GetArtistDetailsAsync(_client, artistId);
                            if (data != null)
                            {
                                var resp = new { type = "artist_details", data = data };
                                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                                BroadcastJson(JsonSerializer.Serialize(resp, options));
                            }
                            else
                            {
                                SendError("ARTIST_NOT_FOUND", "Unable to load artist information.");
                            }
                        }
                        break;

                    case "get_queue":
                        var queue = await _client.GetQueueAsync();
                        BroadcastJson(_serializer.SerializeQueue(queue));
                        break;

                    case "play":
                        if (root.TryGetProperty("uri", out var uriProp))
                        {
                            var uri = uriProp.GetString();
                            if (uri.Contains(":playlist:") || uri.Contains(":album:"))
                            {
                                await _client.PlayContextAsync(uri);
                            }
                            else
                            {
                                await _client.PlayTrackAsync(uri);
                            }
                            _ = TriggerPlayerStateUpdateAsync();
                        }
                        break;

                    case "play_artist":
                        if (root.TryGetProperty("artist_uri", out var artistUriProp))
                        {
                            var artistUri = artistUriProp.GetString();
                            await _client.PlayContextAsync(artistUri);
                            _ = TriggerPlayerStateUpdateAsync();
                        }
                        break;

                    case "play_context":
                        if (root.TryGetProperty("context_uri", out var ctxUriProp) && root.TryGetProperty("offset_uri", out var offUriProp))
                        {
                            var ctxUri = ctxUriProp.GetString();
                            var offUri = offUriProp.GetString();
                            await _client.PlayContextOffsetAsync(ctxUri, offUri);
                            _ = TriggerPlayerStateUpdateAsync();
                        }
                        break;

                    case "pause":
                        await _client.PauseAsync();
                        _ = TriggerPlayerStateUpdateAsync();
                        break;

                    case "get_settings":
                        var settingsJson = SpotifyOverlay.Core.Services.ThemeService.Instance.GetSettingsJson();
                        BroadcastJson($"{{\"type\":\"settings\", \"data\":{settingsJson}}}");
                        break;

                    case "save_settings":
                        if (root.TryGetProperty("data", out var dataProp))
                        {
                            var newSettings = JsonSerializer.Deserialize<SpotifyOverlay.Core.Models.Settings>(dataProp.GetRawText());
                            if (newSettings != null)
                            {
                                SpotifyOverlay.Core.Services.ThemeService.Instance.CurrentSettings.OverlayMode = newSettings.OverlayMode;
                                SpotifyOverlay.Core.Services.ThemeService.Instance.CurrentSettings.Theme = newSettings.Theme;
                                SpotifyOverlay.Core.Services.ThemeService.Instance.CurrentSettings.NotifyTrackChange = newSettings.NotifyTrackChange;
                                SpotifyOverlay.Core.Services.ThemeService.Instance.CurrentSettings.NotifyQueue = newSettings.NotifyQueue;
                                SpotifyOverlay.Core.Services.ThemeService.Instance.CurrentSettings.NotifyDevice = newSettings.NotifyDevice;
                                SpotifyOverlay.Core.Services.ThemeService.Instance.SaveSettings();
                                BroadcastJson($"{{\"type\":\"settings\", \"data\":{SpotifyOverlay.Core.Services.ThemeService.Instance.GetSettingsJson()}}}");
                            }
                        }
                        break;

                    case "resume":
                        await _client.ResumeAsync();
                        _ = TriggerPlayerStateUpdateAsync();
                        break;

                    case "next":
                        await _client.NextAsync();
                        _ = TriggerPlayerStateUpdateAsync();
                        break;

                    case "previous":
                        await _client.PreviousAsync();
                        _ = TriggerPlayerStateUpdateAsync();
                        break;

                    case "add_to_queue":
                        if (root.TryGetProperty("uri", out var addUriProp))
                        {
                            var uri = addUriProp.GetString();
                            await _client.AddToQueueAsync(uri);
                            SpotifyOverlay.Core.Services.NotificationService.Instance.ShowQueueNotification("Track added to queue");
                            
                            // Re-fetch queue and broadcast
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(500);
                                var newQueue = await _client.GetQueueAsync();
                                BroadcastJson(_serializer.SerializeQueue(newQueue));
                            });
                        }
                        break;

                    case "set_volume":
                        if (root.TryGetProperty("volume", out var volProp))
                        {
                            double volume = volProp.GetDouble();
                            int volPercent = (int)Math.Round(volume * 100);
                            volPercent = Math.Clamp(volPercent, 0, 100);
                            await _client.SetVolumeAsync(volPercent);
                            _ = TriggerPlayerStateUpdateAsync();
                        }
                        break;

                    default:
                        SendError("UNKNOWN_COMMAND", $"The command '{command}' is not supported.");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling command: {ex.Message}");
                Logging.BackendLogger.Instance.Log("ROUTE", $"ERROR: {ex}");
                SendError("INTERNAL_ERROR", ex.Message);
            }
        }

        public async Task TriggerPlayerStateUpdateAsync()
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(500); // Give Spotify API a moment
                var playback = await _client.GetCurrentPlaybackAsync();
                if (playback != null)
                {
                    BroadcastJson(_serializer.SerializePlaybackState(playback));
                }
            });
        }
    }
}
