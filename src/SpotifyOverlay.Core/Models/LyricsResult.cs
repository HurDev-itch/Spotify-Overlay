using System.Collections.Generic;

namespace SpotifyOverlay.Core.Models
{
    public class LyricLine
    {
        public long TimeMs { get; set; }
        public string Text { get; set; }
    }

    public class LyricsResult
    {
        public bool IsFound { get; set; }
        public string Source { get; set; }
        public string RawLrc { get; set; }
        public List<LyricLine> Lines { get; set; } = new List<LyricLine>();
    }
}
