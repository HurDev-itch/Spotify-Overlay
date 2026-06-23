using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using SpotifyOverlay.Core.OAuth;
using SpotifyOverlay.Core.Spotify.Models;

namespace SpotifyOverlay.Core.Spotify
{
    public class SpotifyClient
    {
        private readonly HttpClient _httpClient;
        private readonly OAuthManager _oauthManager;

        public SpotifyClient(OAuthManager oauthManager)
        {
            _oauthManager = oauthManager;
            _httpClient = new HttpClient();
        }

        private void Log(string message)
        {
            Logging.BackendLogger.Instance.Log("Spotify API", message);
        }

        private async Task EnsureAuthorizedAsync()
        {
            if (string.IsNullOrEmpty(_oauthManager.AccessToken))
            {
                await _oauthManager.TryLoadAndRefreshAsync();
            }
        }
        private async Task<HttpResponseMessage> GetWithLogAsync(string uri)
        {
            await EnsureAuthorizedAsync();
            
            // Build full absolute URL to avoid HttpClient BaseAddress resolution quirks
            string fullUrl;
            if (uri.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                fullUrl = uri;
            }
            else
            {
                fullUrl = "https://api.spotify.com/v1/" + uri;
            }
            
            Log($"GET {fullUrl}");
            var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _oauthManager.AccessToken);
            var response = await _httpClient.SendAsync(request);
            Log($"GET {fullUrl} - {(int)response.StatusCode} {response.StatusCode}");
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Log($"Error: {errorBody}");
            }
            return response;
        }

        private async Task<HttpResponseMessage> PostWithLogAsync(string uri, HttpContent content)
        {
            await EnsureAuthorizedAsync();
            string fullUrl = uri.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? uri
                : "https://api.spotify.com/v1/" + uri;
            Log($"POST {fullUrl}");
            var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _oauthManager.AccessToken);
            if (content != null) request.Content = content;
            var response = await _httpClient.SendAsync(request);
            Log($"POST {fullUrl} - {(int)response.StatusCode} {response.StatusCode}");
            if (!response.IsSuccessStatusCode) Log($"Error: {await response.Content.ReadAsStringAsync()}");
            return response;
        }

        private async Task<HttpResponseMessage> PutWithLogAsync(string uri, HttpContent content)
        {
            await EnsureAuthorizedAsync();
            string fullUrl = uri.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? uri
                : "https://api.spotify.com/v1/" + uri;
            Log($"PUT {fullUrl}");
            var request = new HttpRequestMessage(HttpMethod.Put, fullUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _oauthManager.AccessToken);
            if (content != null) request.Content = content;
            var response = await _httpClient.SendAsync(request);
            Log($"PUT {fullUrl} - {(int)response.StatusCode} {response.StatusCode}");
            if (!response.IsSuccessStatusCode) Log($"Error: {await response.Content.ReadAsStringAsync()}");
            return response;
        }

        public async Task<List<Track>> SearchAsync(string query, string type = "track,artist,album", int limit = 10)
        {
            var items = new List<Track>();

            if (string.IsNullOrWhiteSpace(query)) return items;

            Log($"[SEARCH] Query received: {query}");
            Log($"[SEARCH] Calling Spotify API...");
            var response = await GetWithLogAsync($"search?q={Uri.EscapeDataString(query)}&type={type}&limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Parse tracks
                if (root.TryGetProperty("tracks", out var tracksProp) && tracksProp.TryGetProperty("items", out var trackItems))
                {
                    foreach (var item in trackItems.EnumerateArray())
                    {
                        items.Add(ParseTrack(item));
                    }
                }

                // Parse artists (map to Track model for unified display)
                if (root.TryGetProperty("artists", out var artistsProp) && artistsProp.TryGetProperty("items", out var artistItems))
                {
                    foreach (var item in artistItems.EnumerateArray())
                    {
                        var id = item.TryGetProperty("id", out var idP) ? idP.GetString() : "";
                        var name = item.TryGetProperty("name", out var nameP) ? nameP.GetString() : "Unknown Artist";
                        var uri = item.TryGetProperty("uri", out var uriP) ? uriP.GetString() : "";
                        var imageUrl = "";
                        if (item.TryGetProperty("images", out var imgP) && imgP.GetArrayLength() > 0)
                        {
                            imageUrl = imgP[0].GetProperty("url").GetString();
                        }
                        items.Add(new Track { Id = id, Name = name, Artist = "Artist", Image = imageUrl, Uri = uri, ItemType = "artist" });
                    }
                }

                // Parse albums (map to Track model for unified display)
                if (root.TryGetProperty("albums", out var albumsProp) && albumsProp.TryGetProperty("items", out var albumItems))
                {
                    foreach (var item in albumItems.EnumerateArray())
                    {
                        var id = item.TryGetProperty("id", out var idP) ? idP.GetString() : "";
                        var name = item.TryGetProperty("name", out var nameP) ? nameP.GetString() : "Unknown Album";
                        var uri = item.TryGetProperty("uri", out var uriP) ? uriP.GetString() : "";
                        string artistName = "Unknown Artist";
                        if (item.TryGetProperty("artists", out var artP) && artP.GetArrayLength() > 0)
                        {
                            artistName = artP[0].GetProperty("name").GetString();
                        }
                        var imageUrl = "";
                        if (item.TryGetProperty("images", out var imgP) && imgP.GetArrayLength() > 0)
                        {
                            imageUrl = imgP[0].GetProperty("url").GetString();
                        }
                        items.Add(new Track { Id = id, Name = name, Artist = artistName + " (Album)", Image = imageUrl, Uri = uri, ItemType = "album" });
                    }
                }

                Log($"[SEARCH] Results returned: {items.Count}");
            }
            else
            {
                Log($"[SEARCH] API call failed");
            }

            Log($"[SEARCH] Sending results to widget");
            return items;
        }

        public async Task<ArtistDto> GetArtistAsync(string artistId)
        {
            var response = await GetWithLogAsync($"artists/{artistId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Log($"[ARTIST DEBUG] {json}");
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var id = root.TryGetProperty("id", out var idP) ? idP.GetString() : "";
                var name = root.TryGetProperty("name", out var nameP) ? nameP.GetString() : "Unknown Artist";
                var uri = root.TryGetProperty("uri", out var uriP) ? uriP.GetString() : "";
                var popularity = root.TryGetProperty("popularity", out var popP) ? popP.GetInt32() : 0;
                
                var followers = 0;
                if (root.TryGetProperty("followers", out var follP) && follP.TryGetProperty("total", out var follTotalP))
                {
                    followers = follTotalP.GetInt32();
                }

                var genres = new List<string>();
                if (root.TryGetProperty("genres", out var genP))
                {
                    foreach (var genre in genP.EnumerateArray())
                    {
                        genres.Add(genre.GetString());
                    }
                }

                var imageUrl = "";
                if (root.TryGetProperty("images", out var imgP) && imgP.GetArrayLength() > 0)
                {
                    imageUrl = imgP[0].GetProperty("url").GetString();
                }

                return new ArtistDto
                {
                    Id = id,
                    Name = name,
                    Uri = uri,
                    Popularity = popularity,
                    Followers = followers,
                    Genres = genres,
                    Image = imageUrl
                };
            }
            return null;
        }

        public async Task<List<Track>> GetArtistTopTracksAsync(string artistName)
        {
            var items = new List<Track>();
            var query = Uri.EscapeDataString($"artist:\"{artistName}\"");
            var response = await GetWithLogAsync($"search?q={query}&type=track&limit=10");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Log($"[TOP TRACKS DEBUG] {json}");
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("tracks", out var tracksProp) && tracksProp.TryGetProperty("items", out var trackItems))
                {
                    foreach (var item in trackItems.EnumerateArray())
                    {
                        items.Add(ParseTrack(item));
                    }
                }
            }
            return items;
        }

        public async Task<List<Playlist>> GetUserPlaylistsAsync(int limit = 50)
        {
            var items = new List<Playlist>();
            string nextUrl = $"me/playlists?limit={limit}";
            int pageNum = 0;

            Log($"[PLAYLISTS] Requesting playlists...");

            while (!string.IsNullOrEmpty(nextUrl))
            {
                pageNum++;
                var response = await GetWithLogAsync(nextUrl);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Log($"[PLAYLISTS] 403 Forbidden - token lacks playlist scopes. User must re-authenticate.");
                    // Clear cached token so next startup forces re-auth
                    OAuth.TokenStorage.ClearToken();
                    break;
                }
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    int pageCount = 0;
                    if (doc.RootElement.TryGetProperty("items", out var playlistItems))
                    {
                        foreach (var item in playlistItems.EnumerateArray())
                        {
                            if (item.ValueKind != JsonValueKind.Null)
                            {
                                items.Add(ParsePlaylist(item));
                                pageCount++;
                            }
                        }
                    }
                    Log($"[PLAYLISTS] Page {pageNum}: {pageCount} playlists");

                    if (doc.RootElement.TryGetProperty("next", out var nextProp) && nextProp.ValueKind == JsonValueKind.String)
                    {
                        var fullNextUrl = nextProp.GetString();
                        if (!string.IsNullOrEmpty(fullNextUrl))
                        {
                            // Pass the full absolute URL directly — GetWithLogAsync handles it
                            nextUrl = fullNextUrl;
                        }
                        else
                        {
                            nextUrl = null;
                        }
                    }
                    else
                    {
                        nextUrl = null;
                    }
                }
                else
                {
                    Log($"[PLAYLISTS] Failed on page {pageNum}");
                    break;
                }
            }

            Log($"[PLAYLISTS] Total playlists loaded: {items.Count}");
            return items;
        }

        public async Task<PlaylistTracksData> GetPlaylistTracksAsync(string playlistId, int offset = 0, int limit = 50)
        {
            var data = new PlaylistTracksData { Offset = offset, Limit = limit, Items = new List<Track>() };
            Log($"[PLAYLISTS] Requesting tracks for {playlistId} at offset {offset}");

            var response = await GetWithLogAsync($"playlists/{playlistId}/items?offset={offset}&limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("total", out var totalProp) && totalProp.ValueKind == JsonValueKind.Number)
                {
                    data.Total = totalProp.GetInt32();
                }

                if (root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in itemsProp.EnumerateArray())
                    {
                        if (item.TryGetProperty("track", out var trackProp) && trackProp.ValueKind != JsonValueKind.Null)
                        {
                            data.Items.Add(ParseTrack(trackProp));
                        }
                        else if (item.TryGetProperty("item", out var itemProp2) && itemProp2.ValueKind != JsonValueKind.Null)
                        {
                            data.Items.Add(ParseTrack(itemProp2));
                        }
                    }
                }
            }
            return data;
        }

        public async Task<QueueItem> GetQueueAsync()
        {
            var queue = new QueueItem();

            var response = await GetWithLogAsync("me/player/queue");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("currently_playing", out var currentItem) && currentItem.ValueKind != JsonValueKind.Null)
                {
                    queue.Current = ParseTrack(currentItem);
                }

                if (doc.RootElement.TryGetProperty("queue", out var queueItems))
                {
                    foreach (var item in queueItems.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Null)
                        {
                            queue.UpNext.Add(ParseTrack(item));
                        }
                    }
                }
            }

            return queue;
        }

        public async Task<bool> PlayContextAsync(string contextUri)
        {
            var content = new StringContent($"{{\"context_uri\":\"{contextUri}\"}}", System.Text.Encoding.UTF8, "application/json");
            var response = await PutWithLogAsync("me/player/play", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PlayContextOffsetAsync(string contextUri, string trackUri)
        {
            var content = new StringContent($"{{\"context_uri\":\"{contextUri}\",\"offset\":{{\"uri\":\"{trackUri}\"}}}}", System.Text.Encoding.UTF8, "application/json");
            var response = await PutWithLogAsync("me/player/play", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PlayTrackAsync(string trackUri)
        {
            var content = new StringContent($"{{\"uris\":[\"{trackUri}\"]}}", System.Text.Encoding.UTF8, "application/json");
            var response = await PutWithLogAsync("me/player/play", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PauseAsync()
        {
            var response = await PutWithLogAsync("me/player/pause", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ResumeAsync()
        {
            var response = await PutWithLogAsync("me/player/play", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> NextAsync()
        {
            var response = await PostWithLogAsync("me/player/next", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PreviousAsync()
        {
            var response = await PostWithLogAsync("me/player/previous", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddToQueueAsync(string trackUri)
        {
            var response = await PostWithLogAsync($"me/player/queue?uri={Uri.EscapeDataString(trackUri)}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SetVolumeAsync(int volumePercent)
        {
            var response = await PutWithLogAsync($"me/player/volume?volume_percent={volumePercent}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<PlaybackStateData> GetCurrentPlaybackAsync()
        {
            var response = await GetWithLogAsync("me/player");
            if (response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                bool isPlaying = root.TryGetProperty("is_playing", out var isPlayingProp) && isPlayingProp.GetBoolean();
                
                long progressMs = 0;
                if (root.TryGetProperty("progress_ms", out var progressProp) && progressProp.ValueKind == JsonValueKind.Number)
                {
                    progressMs = progressProp.GetInt64();
                }

                long durationMs = 0;
                Track track = null;
                if (root.TryGetProperty("item", out var itemProp) && itemProp.ValueKind != JsonValueKind.Null)
                {
                    track = ParseTrack(itemProp);
                    if (itemProp.TryGetProperty("duration_ms", out var durProp) && durProp.ValueKind == JsonValueKind.Number)
                    {
                        durationMs = durProp.GetInt64();
                    }
                }

                double volume = 0;
                if (root.TryGetProperty("device", out var deviceProp) && deviceProp.ValueKind != JsonValueKind.Null)
                {
                    if (deviceProp.TryGetProperty("volume_percent", out var volProp) && volProp.ValueKind != JsonValueKind.Null)
                    {
                        volume = volProp.GetInt32() / 100.0;
                    }
                }

                return new PlaybackStateData
                {
                    IsPlaying = isPlaying,
                    Volume = volume,
                    ProgressMs = progressMs,
                    DurationMs = durationMs,
                    CurrentTrack = track
                };
            }
            return null;
        }

        private Track ParseTrack(JsonElement item)
        {
            var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : "";
            var title = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Unknown Track";
            var uri = item.TryGetProperty("uri", out var uriProp) ? uriProp.GetString() : "";
            
            string artistName = "Unknown Artist";
            if (item.TryGetProperty("artists", out var artistsProp) && artistsProp.GetArrayLength() > 0)
            {
                artistName = artistsProp[0].GetProperty("name").GetString();
            }

            string imageUrl = "";
            if (item.TryGetProperty("album", out var albumProp) && albumProp.TryGetProperty("images", out var imagesProp) && imagesProp.GetArrayLength() > 0)
            {
                imageUrl = imagesProp[0].GetProperty("url").GetString();
            }

            return new Track
            {
                Id = id,
                Name = title,
                Artist = artistName,
                Image = imageUrl,
                Uri = uri
            };
        }

        private Playlist ParsePlaylist(JsonElement item)
        {
            var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : "";
            var title = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Unknown Playlist";
            var uri = item.TryGetProperty("uri", out var uriProp) ? uriProp.GetString() : "";

            int trackCount = 0;
            if (item.TryGetProperty("tracks", out var tracksProp) && tracksProp.TryGetProperty("total", out var totalProp))
            {
                trackCount = totalProp.GetInt32();
            }
            else if (item.TryGetProperty("items", out var itemsProp) && itemsProp.TryGetProperty("total", out totalProp))
            {
                trackCount = totalProp.GetInt32();
            }

            string imageUrl = "";
            if (item.TryGetProperty("images", out var imagesProp) && imagesProp.ValueKind != JsonValueKind.Null && imagesProp.GetArrayLength() > 0)
            {
                imageUrl = imagesProp[0].GetProperty("url").GetString();
            }

            return new Playlist
            {
                Id = id,
                Name = title,
                TrackCount = trackCount,
                Image = imageUrl,
                Uri = uri
            };
        }
    }
}
