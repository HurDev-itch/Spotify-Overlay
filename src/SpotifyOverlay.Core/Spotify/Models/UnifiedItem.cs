using System.Text.Json.Serialization;

namespace SpotifyOverlay.Core.Spotify.Models
{
    public class UnifiedItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "item";

        [JsonPropertyName("item_type")]
        public string ItemType { get; set; } // "track", "playlist", "queue"

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("subtitle")]
        public string Subtitle { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }
    }
}
