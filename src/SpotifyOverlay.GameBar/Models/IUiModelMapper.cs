using Windows.Data.Json;
using System.Collections.Generic;

namespace SpotifyOverlay.GameBar.Models
{
    public interface IUiModelMapper
    {
        TrackUIModel MapTrack(JsonObject json);
        PlaylistUIModel MapPlaylist(JsonObject json);
        ArtistUIModel MapArtist(JsonObject json);
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
                Uri = json.ContainsKey("uri") && json["uri"].ValueType == JsonValueType.String ? json["uri"].GetString() : "",
                ItemType = json.ContainsKey("item_type") && json["item_type"].ValueType == JsonValueType.String ? json["item_type"].GetString() : "track"
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

        public ArtistUIModel MapArtist(JsonObject json)
        {
            var model = new ArtistUIModel
            {
                Id = json.ContainsKey("id") && json["id"].ValueType == JsonValueType.String ? json["id"].GetString() : "",
                Name = json.ContainsKey("name") && json["name"].ValueType == JsonValueType.String ? json["name"].GetString() : "Unknown Artist",
                ImageUrl = json.ContainsKey("image") && json["image"].ValueType == JsonValueType.String ? json["image"].GetString() : "",
                Uri = json.ContainsKey("uri") && json["uri"].ValueType == JsonValueType.String ? json["uri"].GetString() : "",
                Followers = json.ContainsKey("followers") && json["followers"].ValueType == JsonValueType.Number ? (int)json["followers"].GetNumber() : 0,
                Popularity = json.ContainsKey("popularity") && json["popularity"].ValueType == JsonValueType.Number ? (int)json["popularity"].GetNumber() : 0
            };

            if (json.ContainsKey("genres") && json["genres"].ValueType == JsonValueType.Array)
            {
                foreach (var g in json["genres"].GetArray())
                {
                    if (g.ValueType == JsonValueType.String)
                    {
                        model.Genres.Add(g.GetString());
                    }
                }
            }
            return model;
        }
    }
}
