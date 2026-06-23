using System;
using DiscordRPC;
using DiscordRPC.Logging;

namespace SpotifyOverlay.Core.Plugins.Discord
{
    public class DiscordRpcPlugin : ISpotifyModule
    {
        private DiscordRpcClient? _client;
        private readonly string _discordClientId;

        public string Id => "Core.DiscordRPC";
        public string Name => "Discord Rich Presence";
        public string Description => "Broadcasts current Spotify playback to Discord.";
        public string Version => "1.0.0";
        public string Author => "SpotifyOverlay Team";

        public DiscordRpcPlugin(string discordClientId)
        {
            _discordClientId = discordClientId;
        }

        public void OnLoad()
        {
            _client = new DiscordRpcClient(_discordClientId);
            _client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };
            _client.Initialize();
        }

        public void OnUnload()
        {
            if (_client != null)
            {
                _client.ClearPresence();
                _client.Dispose();
                _client = null;
            }
        }

        public void OnTrackChanged(TrackChangedEventArgs args)
        {
            if (_client == null || !_client.IsInitialized) return;

            _client.SetPresence(new RichPresence()
            {
                Details = args.TrackName,
                State = $"by {args.ArtistName}",
                Assets = new Assets()
                {
                    LargeImageKey = "spotify_logo", // You'd set up an asset named this on your Discord Dev portal
                    LargeImageText = "Listening on SpotifyOverlay",
                }
            });
        }

        public void OnPlaybackPaused()
        {
            if (_client == null || !_client.IsInitialized) return;
            
            var presence = _client.CurrentPresence;
            if (presence != null)
            {
                presence.Assets.SmallImageKey = "paused_icon";
                presence.Assets.SmallImageText = "Paused";
                _client.SetPresence(presence);
            }
        }

        public void OnPlaybackResumed()
        {
            if (_client == null || !_client.IsInitialized) return;
            
            var presence = _client.CurrentPresence;
            if (presence != null)
            {
                presence.Assets.SmallImageKey = "playing_icon";
                presence.Assets.SmallImageText = "Playing";
                _client.SetPresence(presence);
            }
        }
    }
}
