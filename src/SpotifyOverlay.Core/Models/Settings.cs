using System.Text.Json.Serialization;

namespace SpotifyOverlay.Core.Models
{
    public class Settings
    {
        [JsonPropertyName("overlay_mode")]
        public string OverlayMode { get; set; } = "compact";

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "SpotifyOverlay Default";

        [JsonPropertyName("accent_color")]
        public string AccentColor { get; set; } = "#1DB954";

        [JsonPropertyName("background_opacity")]
        public double BackgroundOpacity { get; set; } = 0.9;

        [JsonPropertyName("corner_radius")]
        public double CornerRadius { get; set; } = 8.0;

        [JsonPropertyName("artwork_blur_strength")]
        public double ArtworkBlurStrength { get; set; } = 20.0;

        [JsonPropertyName("font_size")]
        public double FontSize { get; set; } = 14.0;

        [JsonPropertyName("notify_track_change")]
        public bool NotifyTrackChange { get; set; } = true;

        [JsonPropertyName("notify_queue")]
        public bool NotifyQueue { get; set; } = true;

        [JsonPropertyName("notify_device")]
        public bool NotifyDevice { get; set; } = true;
    }
}
