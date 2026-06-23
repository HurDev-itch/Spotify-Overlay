using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SpotifyOverlay.Core.Spotify.Models
{
    // Domain Models (Backend internal)
    public class Track
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("artist")]
        public string Artist { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("item_type")]
        public string ItemType { get; set; } = "track";
    }

    public class ArtistDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("followers")]
        public int Followers { get; set; }

        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new List<string>();

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }
    }

    public class Playlist
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("track_count")]
        public int TrackCount { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }
    }

    public class QueueItem
    {
        [JsonPropertyName("current")]
        public Track Current { get; set; }

        [JsonPropertyName("up_next")]
        public List<Track> UpNext { get; set; } = new List<Track>();
    }

    public class PlaylistTracksData
    {
        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public List<Track> Items { get; set; } = new List<Track>();
    }

    // Wire Protocol DTOs
    public class WireMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class ErrorResponse : WireMessage
    {
        public ErrorResponse() { Type = "error"; }

        [JsonPropertyName("error")]
        public ErrorDetails Error { get; set; }
    }

    public class ErrorDetails
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class SearchResponse : WireMessage
    {
        public SearchResponse() { Type = "search_results"; }

        [JsonPropertyName("data")]
        public List<Track> Data { get; set; } = new List<Track>();
    }

    public class PlaylistsResponse : WireMessage
    {
        public PlaylistsResponse() { Type = "playlists"; }

        [JsonPropertyName("data")]
        public List<Playlist> Data { get; set; } = new List<Playlist>();
    }

    public class PlaylistTracksResponse : WireMessage
    {
        public PlaylistTracksResponse() { Type = "playlist_tracks"; }

        [JsonPropertyName("playlist_id")]
        public string PlaylistId { get; set; }

        [JsonPropertyName("data")]
        public PlaylistTracksData Data { get; set; }
    }

    public class QueueResponse : WireMessage
    {
        public QueueResponse() { Type = "queue"; }

        [JsonPropertyName("data")]
        public QueueItem Data { get; set; }
    }

    public class PlaybackStateResponse : WireMessage
    {
        public PlaybackStateResponse() { Type = "playback_state"; }

        [JsonPropertyName("data")]
        public PlaybackStateData Data { get; set; }
    }

    public class PlaybackStateData
    {
        [JsonPropertyName("is_playing")]
        public bool IsPlaying { get; set; }

        [JsonPropertyName("volume")]
        public double Volume { get; set; }

        [JsonPropertyName("progress_ms")]
        public long ProgressMs { get; set; }

        [JsonPropertyName("duration_ms")]
        public long DurationMs { get; set; }

        [JsonPropertyName("current_track")]
        public Track CurrentTrack { get; set; }
    }

    public class ArtistDetailsResponse : WireMessage
    {
        public ArtistDetailsResponse() { Type = "artist_details"; }

        [JsonPropertyName("data")]
        public ArtistDetailsData Data { get; set; }
    }

    public class ArtistDetailsData
    {
        [JsonPropertyName("artist")]
        public ArtistDto Artist { get; set; }

        [JsonPropertyName("tracks")]
        public List<Track> Tracks { get; set; } = new List<Track>();
    }
}
