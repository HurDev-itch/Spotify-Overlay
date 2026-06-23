using System.Threading.Tasks;
using SpotifyOverlay.Core.Models;

namespace SpotifyOverlay.Core.Services.Lyrics
{
    public interface ILyricsProvider
    {
        Task<LyricsResult> GetLyricsAsync(string trackName, string artistName);
    }
}
