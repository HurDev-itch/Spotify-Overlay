using System.Text.Json;
using SpotifyOverlay.Core.Spotify.Models;

namespace SpotifyOverlay.Core.Spotify.Protocol
{
    public interface IProtocolSerializer
    {
        string SerializeSearch(System.Collections.Generic.List<Track> tracks);
        string SerializePlaylists(System.Collections.Generic.List<Playlist> playlists);
        string SerializeQueue(QueueItem queue);
        string SerializePlaybackState(PlaybackStateData state);
        string SerializeError(string code, string message);
    }

    public class ProtocolSerializer : IProtocolSerializer
    {
        public string SerializeSearch(System.Collections.Generic.List<Track> tracks)
        {
            var response = new SearchResponse { Data = tracks };
            return JsonSerializer.Serialize(response);
        }

        public string SerializePlaylists(System.Collections.Generic.List<Playlist> playlists)
        {
            var response = new PlaylistsResponse { Data = playlists };
            return JsonSerializer.Serialize(response);
        }

        public string SerializeQueue(QueueItem queue)
        {
            var response = new QueueResponse { Data = queue };
            return JsonSerializer.Serialize(response);
        }

        public string SerializePlaybackState(PlaybackStateData state)
        {
            var response = new PlaybackStateResponse { Data = state };
            return JsonSerializer.Serialize(response);
        }

        public string SerializeError(string code, string message)
        {
            var response = new ErrorResponse
            {
                Error = new ErrorDetails { Code = code, Message = message }
            };
            return JsonSerializer.Serialize(response);
        }
    }
}
