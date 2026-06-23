using System;

namespace SpotifyOverlay.Core.Plugins
{
    /// <summary>
    /// Interface for plugins that provide visual elements or modules to the overlay.
    /// </summary>
    public interface IOverlayModule : IPlugin
    {
        /// <summary>
        /// Instructs the module to render its ImGui or custom UI elements.
        /// This is intended to be serialized and passed via IPC, or handled directly if the plugin is native C++.
        /// </summary>
        void Render();

        /// <summary>
        /// Toggles the visibility of the module.
        /// </summary>
        bool IsVisible { get; set; }
    }
}
