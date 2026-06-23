using Windows.Data.Json;
using System.Collections.Generic;

namespace SpotifyOverlay.GameBar.Models
{
    public interface IUiModelMapper
    {
        TrackUIModel MapTrack(JsonObject json);
        PlaylistUIModel MapPlaylist(JsonObject json);
    }

    public class UiModelMapper : IUiModelMapper
    {
        public TrackUIModel MapTrack(JsonObject json)
        {
            return new TrackUIModel
            {
                Id = json.ContainsKey("id") && json["id"].ValueType == JsonValueType.String ? json["id"].GetString() : "",
                Name = json.ContainsKey("name") && json["name"].ValueType == JsonValueType.String ? json["name"].GetString() : "Unknown Track",
                Artist = json.ContainsKey("artist") && json["artist"].ValueType == JsonValueType.String ? json["artist"].GetString() : "Unknown Artist",
                ImageUrl = json.ContainsKey("image") && json["image"].ValueType == JsonValueType.String ? json["image"].GetString() : "",
                Uri = json.ContainsKey("uri") && json["uri"].ValueType == JsonValueType.String ? json["uri"].GetString() : ""
            };
        }

        public PlaylistUIModel MapPlaylist(JsonObject json)
        {
            return new PlaylistUIModel
            {
                Id = json.ContainsKey("id") && json["id"].ValueType == JsonValueType.String ? json["id"].GetString() : "",
                Name = json.ContainsKey("name") && json["name"].ValueType == JsonValueType.String ? json["name"].GetString() : "Unknown Playlist",
                TrackCount = json.ContainsKey("track_count") && json["track_count"].ValueType == JsonValueType.Number ? (int)json["track_count"].GetNumber() : 0,
                ImageUrl = json.ContainsKey("image") && json["image"].ValueType == JsonValueType.String ? json["image"].GetString() : "",
                Uri = json.ContainsKey("uri") && json["uri"].ValueType == JsonValueType.String ? json["uri"].GetString() : ""
            };
        }
    }
}
