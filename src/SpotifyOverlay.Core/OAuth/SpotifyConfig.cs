using System;

namespace SpotifyOverlay.Core.OAuth
{
    public class SpotifyConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty; // Not strictly needed for PKCE, but good to have
        public string RedirectUri { get; set; } = "http://127.0.0.1:5000/callback";
        public string[] Scopes { get; set; } = new[] 
        { 
            "user-read-playback-state", 
            "user-modify-playback-state", 
            "user-read-currently-playing",
            "playlist-read-private",
            "playlist-read-collaborative"
        };
    }
}
