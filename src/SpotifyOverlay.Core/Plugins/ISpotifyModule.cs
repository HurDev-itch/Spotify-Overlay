using System;

namespace SpotifyOverlay.Core.Plugins
{
    /// <summary>
    /// Event arguments for when the track changes.
    /// </summary>
    public class TrackChangedEventArgs : EventArgs
    {
        public string TrackId { get; set; } = string.Empty;
        public string TrackName { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface for plugins that interact with or respond to Spotify events.
    /// </summary>
    public interface ISpotifyModule : IPlugin
    {
        /// <summary>
        /// Called when the currently playing track changes.
        /// </summary>
        void OnTrackChanged(TrackChangedEventArgs args);

        /// <summary>
        /// Called when playback is paused.
        /// </summary>
        void OnPlaybackPaused();

        /// <summary>
        /// Called when playback is resumed.
        /// </summary>
        void OnPlaybackResumed();
    }
}
