using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifyOverlay.IPC;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpotifyOverlay.Core.Spotify;

namespace SpotifyOverlay.App.Server
{
    public class BackendServer : ITransport
    {
        private WebApplication _app;
        private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
        private readonly SpotifyStateService _spotifyState;

        public event EventHandler<byte[]> MessageReceived;
        public event EventHandler ClientConnected;
        public event EventHandler ClientDisconnected;

        public BackendServer(SpotifyStateService spotifyState)
        {
            _spotifyState = spotifyState;
            _spotifyState.SetBroadcastAction(json =>
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                _ = SendAsync(bytes);
            });
            MessageReceived += BackendServer_MessageReceived;
        }

        private async void BackendServer_MessageReceived(object sender, byte[] e)
        {
            var json = Encoding.UTF8.GetString(e);
            await _spotifyState.HandleCommandAsync(json);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting BackendServer...");
                var builder = WebApplication.CreateBuilder();
                
                // Configure Kestrel to listen on port 8999
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenLocalhost(8999);
                });

                _app = builder.Build();
                _app.UseWebSockets();

                // Discovery Endpoint
                _app.MapGet("/discovery", () =>
                {
                    return Results.Json(new
                    {
                        ws_port = 8999,
                        session_id = Guid.NewGuid().ToString()
                    });
                });

                // WebSocket Endpoint
                _app.Map("/ws", async context =>
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await HandleWebSocketConnection(webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    }
                });

                await _app.StartAsync(cancellationToken);
                System.Diagnostics.Debug.WriteLine("BackendServer started successfully on port 8999.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BackendServer failed to start: {ex}");
                SpotifyOverlay.Core.Logging.BackendLogger.Instance.Log("SERVER", $"FATAL: {ex}");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_app != null)
            {
                await _app.StopAsync(cancellationToken);
            }
        }

        public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            var messageString = Encoding.UTF8.GetString(data);
            Log($"[WS OUT] {messageString}");

            foreach (var kvp in _clients)
            {
                var socket = kvp.Value;
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(
                        new ArraySegment<byte>(data),
                        WebSocketMessageType.Text,
                        true,
                        cancellationToken);
                }
            }
        }

        private void Log(string message)
        {
            SpotifyOverlay.Core.Logging.BackendLogger.Instance.Log("WS", message);
        }

        private async Task HandleWebSocketConnection(WebSocket webSocket)
        {
            var clientId = Guid.NewGuid();
            _clients.TryAdd(clientId, webSocket);
            ClientConnected?.Invoke(this, EventArgs.Empty);
            Log($"[WS CONNECT] Client connected: {clientId}");

            var buffer = new byte[1024 * 4];
            try
            {
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!receiveResult.CloseStatus.HasValue)
                {
                    var data = new byte[receiveResult.Count];
                    Array.Copy(buffer, data, receiveResult.Count);
                    
                    var messageString = Encoding.UTF8.GetString(data);
                    Log($"[WS IN] {messageString}");

                    MessageReceived?.Invoke(this, data);

                    receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(
                    receiveResult.CloseStatus.Value,
                    receiveResult.CloseStatusDescription,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log($"[WS ERROR] {ex.Message}");
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
                ClientDisconnected?.Invoke(this, EventArgs.Empty);
                Log($"[WS DISCONNECT] Client disconnected: {clientId}");
            }
        }
    }
}
