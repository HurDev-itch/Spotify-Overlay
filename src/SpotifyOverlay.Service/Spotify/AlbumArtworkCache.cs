using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyOverlay.Service.Spotify
{
    public class AlbumArtworkCache
    {
        private readonly HttpClient _httpClient;
        private readonly string _cacheDirectory;

        public AlbumArtworkCache()
        {
            _httpClient = new HttpClient();
            _cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpotifyOverlay", "ArtworkCache");
            
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        public async Task<string> GetOrDownloadArtworkAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;

            var fileName = ComputeHash(url) + ".jpg";
            var filePath = Path.Combine(_cacheDirectory, fileName);

            if (File.Exists(filePath))
            {
                return filePath; // Return cached local path
            }

            try
            {
                var imageBytes = await _httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(filePath, imageBytes);
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AlbumArtworkCache] Failed to download artwork: {ex.Message}");
                return string.Empty;
            }
        }

        private string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
