using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyOverlay.IPC
{
    public interface ITransport
    {
        event EventHandler<byte[]> MessageReceived;
        event EventHandler ClientConnected;
        event EventHandler ClientDisconnected;

        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        Task SendAsync(byte[] data, CancellationToken cancellationToken = default);
    }
}
