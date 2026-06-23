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
                            var json = _serializer.SerializePlaybackState(playback);
                            BroadcastJson(json);
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

                    case "pause":
                        await _client.PauseAsync();
                        _ = TriggerPlayerStateUpdateAsync();
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
