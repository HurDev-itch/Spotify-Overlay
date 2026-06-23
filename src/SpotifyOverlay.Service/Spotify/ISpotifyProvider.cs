using System;
using System.Threading;
using System.Threading.Tasks;
using SpotifyOverlay.IPC;

namespace SpotifyOverlay.Service.Spotify
{
    public interface ISpotifyProvider
    {
        event EventHandler<SpotifyState>? TrackChanged;
        event EventHandler<SpotifyState>? PlaybackPaused;
        event EventHandler<SpotifyState>? PlaybackResumed;
        event EventHandler<SpotifyState>? VolumeChanged;

        Task StartPollingAsync(CancellationToken cancellationToken);
    }
}
