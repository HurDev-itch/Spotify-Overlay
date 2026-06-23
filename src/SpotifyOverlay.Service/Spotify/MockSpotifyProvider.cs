using System;
using System.Threading;
using System.Threading.Tasks;
using SpotifyOverlay.IPC;

namespace SpotifyOverlay.Service.Spotify
{
    public class MockSpotifyProvider : ISpotifyProvider
    {
        public event EventHandler<SpotifyState>? TrackChanged;
        public event EventHandler<SpotifyState>? PlaybackPaused;
        public event EventHandler<SpotifyState>? PlaybackResumed;
        public event EventHandler<SpotifyState>? VolumeChanged;

        private SpotifyState _currentState;

        public MockSpotifyProvider()
        {
            _currentState = new SpotifyState
            {
                TrackId = "mock_track_123",
                TrackName = "TestHost Diagnostic Track",
                ArtistName = "System Test",
                AlbumName = "Phase 3A Album",
                DurationMs = 180000,
                ProgressMs = 0,
                IsPlaying = true,
                Volume = 80,
                IsShuffle = false,
                ArtworkPath = string.Empty // Could point to a local test image
            };
        }

        public async Task StartPollingAsync(CancellationToken cancellationToken)
        {
            // Simulate initial track load
            TrackChanged?.Invoke(this, _currentState);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_currentState.IsPlaying)
                {
                    _currentState.ProgressMs += 1000;
                    if (_currentState.ProgressMs >= _currentState.DurationMs)
                    {
                        _currentState.ProgressMs = 0;
                        _currentState.TrackName = "Next Mock Track";
                        TrackChanged?.Invoke(this, _currentState);
                    }
                }
                
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
