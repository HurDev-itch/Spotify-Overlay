using System;

namespace SpotifyOverlay.Core.Plugins
{
    /// <summary>
    /// Base interface for all Spotify Overlay plugins.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// The unique identifier for the plugin.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The display name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A brief description of what the plugin does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The version of the plugin.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// The author of the plugin.
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Called when the plugin is enabled/loaded.
        /// </summary>
        void OnLoad();

        /// <summary>
        /// Called when the plugin is disabled/unloaded.
        /// </summary>
        void OnUnload();
    }
}
