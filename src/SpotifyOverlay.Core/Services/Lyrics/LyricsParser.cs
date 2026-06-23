using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SpotifyOverlay.Core.Models;

namespace SpotifyOverlay.Core.Services.Lyrics
{
    public static class LyricsParser
    {
        private static readonly Regex LrcRegex = new Regex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\](.*)", RegexOptions.Compiled);

        public static List<LyricLine> ParseLrc(string lrc)
        {
            var lines = new List<LyricLine>();
            if (string.IsNullOrWhiteSpace(lrc)) return lines;

            var strLines = lrc.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in strLines)
            {
                var match = LrcRegex.Match(line);
                if (match.Success)
                {
                    int minutes = int.Parse(match.Groups[1].Value);
                    int seconds = int.Parse(match.Groups[2].Value);
                    string msStr = match.Groups[3].Value;
                    int milliseconds = int.Parse(msStr.PadRight(3, '0'));

                    long timeMs = minutes * 60000 + seconds * 1000 + milliseconds;
                    string text = match.Groups[4].Value.Trim();

                    lines.Add(new LyricLine { TimeMs = timeMs, Text = text });
                }
            }
            return lines;
        }
    }
}
