using System;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpotifyOverlay.Core.OAuth
{
    public class OAuthManager
    {
        private readonly SpotifyConfig _config;
        private readonly HttpClient _httpClient;
        
        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }

        public OAuthManager(SpotifyConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }

        public string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        public string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            return Base64UrlEncode(hash);
        }

        public string GetAuthorizationUrl(string codeChallenge, string state)
        {
            var scopes = string.Join(" ", _config.Scopes);
            return $"https://accounts.spotify.com/authorize?client_id={_config.ClientId}&response_type=code&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}&code_challenge_method=S256&code_challenge={codeChallenge}&state={state}&scope={Uri.EscapeDataString(scopes)}";
        }

        public async Task<bool> ExchangeCodeForTokenAsync(string code, string codeVerifier)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            var content = new StringContent($"client_id={_config.ClientId}&grant_type=authorization_code&code={code}&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}&code_verifier={codeVerifier}", Encoding.UTF8, "application/x-www-form-urlencoded");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                AccessToken = doc.RootElement.GetProperty("access_token").GetString();
                if (doc.RootElement.TryGetProperty("refresh_token", out var refreshProp))
                {
                    RefreshToken = refreshProp.GetString();
                    TokenStorage.SaveRefreshToken(RefreshToken);
                }
                return true;
            }
            return false;
        }

        public async Task<string?> WaitForCallbackAsync()
        {
            using var listener = new HttpListener();
            var prefix = _config.RedirectUri.EndsWith("/") ? _config.RedirectUri : _config.RedirectUri + "/";
            listener.Prefixes.Add(prefix);
            listener.Start();

            try
            {
                var context = await listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                string? code = request.QueryString["code"];
                string responseString = string.IsNullOrEmpty(code)
                    ? "<html><head><style>body{font-family:sans-serif;text-align:center;padding:50px;background:#121212;color:white;}</style></head><body><h1>Authentication Failed!</h1><p>Please close this tab and try again.</p></body></html>"
                    : "<html><head><style>body{font-family:sans-serif;text-align:center;padding:50px;background:#121212;color:white;}</style></head><body><h1 style='color:#1DB954;'>Authentication Successful!</h1><p>You can close this tab and return to Spotify Overlay.</p><script>setTimeout(function(){window.close();}, 3000);</script></body></html>";

                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                return code;
            }
            catch
            {
                return null;
            }
            finally
            {
                listener.Stop();
            }
        }

        public async Task<bool> TryLoadAndRefreshAsync()
        {
            RefreshToken = TokenStorage.LoadRefreshToken();
            if (!string.IsNullOrEmpty(RefreshToken))
            {
                return await RefreshTokenAsync();
            }
            return false;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(RefreshToken)) return false;

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            var content = new StringContent($"client_id={_config.ClientId}&grant_type=refresh_token&refresh_token={RefreshToken}", Encoding.UTF8, "application/x-www-form-urlencoded");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                AccessToken = doc.RootElement.GetProperty("access_token").GetString();
                if (doc.RootElement.TryGetProperty("refresh_token", out var refreshProp))
                {
                    RefreshToken = refreshProp.GetString();
                    TokenStorage.SaveRefreshToken(RefreshToken);
                }
                return true;
            }
            else
            {
                // Token might be invalid/revoked
                TokenStorage.ClearToken();
            }
            return false;
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
