using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using SpotifyOverlay.IPC;

namespace SpotifyOverlay.Service.IPC
{
    public class IPCServer
    {
        private readonly string _pipeName = "SpotifyOverlayIPC";
        private NamedPipeServerStream? _serverStream;

        // Event for when an overlay sends a command
        public event EventHandler<OverlayCommand>? OnCommandReceived;

        public async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _serverStream = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                    Console.WriteLine("[IPCServer] Waiting for overlay client connection...");
                    await _serverStream.WaitForConnectionAsync(cancellationToken);
                    Console.WriteLine("[IPCServer] Overlay client connected.");

                    // Handle incoming commands
                    _ = Task.Run(() => HandleClientAsync(_serverStream, cancellationToken), cancellationToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[IPCServer] Error: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        private async Task HandleClientAsync(NamedPipeServerStream stream, CancellationToken cancellationToken)
        {
            using (stream)
            {
                byte[] lengthBuffer = new byte[4];
                while (stream.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        int bytesRead = await stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
                        if (bytesRead < 4) break;

                        int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                        byte[] messageBuffer = new byte[messageLength];
                        int totalRead = 0;
                        while (totalRead < messageLength)
                        {
                            int read = await stream.ReadAsync(messageBuffer, totalRead, messageLength - totalRead, cancellationToken);
                            if (read == 0) break;
                            totalRead += read;
                        }

                        if (totalRead == messageLength)
                        {
                            var command = OverlayCommand.Parser.ParseFrom(messageBuffer);
                            OnCommandReceived?.Invoke(this, command);
                        }
                    }
                    catch { break; }
                }
            }
        }

        public void BroadcastEvent(ServiceEvent serviceEvent)
        {
            if (_serverStream == null || !_serverStream.IsConnected) return;

            try
            {
                byte[] messageBytes = serviceEvent.ToByteArray();
                byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

                _serverStream.Write(lengthBytes, 0, 4);
                _serverStream.Write(messageBytes, 0, messageBytes.Length);
                _serverStream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IPCServer] Failed to broadcast event: {ex.Message}");
            }
        }
    }
}
