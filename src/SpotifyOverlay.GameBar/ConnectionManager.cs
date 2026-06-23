using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace SpotifyOverlay.GameBar
{
    public class ConnectionManager
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private readonly HttpClient _httpClient = new HttpClient();

        public event Action<string> OnMessageReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    Debug.WriteLine("Attempting to discover backend...");
                    
                    // 1. HTTP Discovery
                    var response = await _httpClient.GetAsync("http://localhost:8999/discovery", _cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        if (JsonObject.TryParse(json, out var doc))
                        {
                            var wsPort = (int)doc.GetNamedNumber("ws_port");
                            Debug.WriteLine($"Found backend on WebSocket port {wsPort}. Connecting...");

                            // 2. Connect WebSocket
                            await ConnectWebSocketAsync(wsPort, _cts.Token);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Connection failed: {ex.Message}. Retrying in 5 seconds...");
                }

                // Exponential backoff or simple delay
                await Task.Delay(5000, _cts.Token);
            }
        }

        private async Task ConnectWebSocketAsync(int port, CancellationToken token)
        {
            _webSocket = new ClientWebSocket();
            try
            {
                await _webSocket.ConnectAsync(new Uri($"ws://localhost:{port}/ws"), token);
                OnConnected?.Invoke();

                Debug.WriteLine("WebSocket connected.");

                // Initial handshake
                var handshake = "{\"type\": \"hello\", \"client\": \"GameBar\"}";
                var handshakeBytes = Encoding.UTF8.GetBytes(handshake);
                await _webSocket.SendAsync(new ArraySegment<byte>(handshakeBytes), WebSocketMessageType.Text, true, token);

                // Receive loop
                var buffer = new byte[1024 * 4];
                while (_webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed", token);
                                break;
                            }
                            
                            ms.Write(buffer, 0, result.Count);
                        }
                        while (!result.EndOfMessage && !token.IsCancellationRequested && _webSocket.State == WebSocketState.Open);

                        if (result.MessageType == WebSocketMessageType.Close) break;

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(ms.ToArray());
                            OnMessageReceived?.Invoke(message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebSocket error: {ex.Message}");
            }
            finally
            {
                if (_webSocket.State != WebSocketState.Closed && _webSocket.State != WebSocketState.Aborted)
                {
                    _webSocket.Dispose();
                }
                OnDisconnected?.Invoke();
                Debug.WriteLine("WebSocket disconnected.");
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _webSocket?.Dispose();
        }
    }
}
