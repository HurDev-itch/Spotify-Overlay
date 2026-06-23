using System;
using System.IO;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using SpotifyOverlay.Core.Models;
using SpotifyOverlay.Core.Spotify.Models;

namespace SpotifyOverlay.Core.Services
{
    public class NotificationService
    {
        private static NotificationService _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        private string _lastTrackId;

        private NotificationService() { }

        public void HandlePlaybackStateChange(PlaybackStateData state)
        {
            if (state?.CurrentTrack == null) return;

            var currentTrackId = state.CurrentTrack.Id;
            
            // Only show notification if the track actually changed
            if (currentTrackId != _lastTrackId)
            {
                _lastTrackId = currentTrackId;

                if (ThemeService.Instance.CurrentSettings.NotifyTrackChange)
                {
                    ShowTrackNotification(state.CurrentTrack);
                }
            }
        }

        public void ShowQueueNotification(string trackName)
        {
            if (!ThemeService.Instance.CurrentSettings.NotifyQueue) return;

            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            var textNodes = toastXml.GetElementsByTagName("text");
            textNodes[0].AppendChild(toastXml.CreateTextNode("Added to Queue"));
            textNodes[1].AppendChild(toastXml.CreateTextNode(trackName));

            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier("SpotifyOverlay").Show(toast);
        }

        private void ShowTrackNotification(Track track)
        {
            try
            {
                // Create a toast with an image (artwork) and text (track name, artist)
                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
                
                var textNodes = toastXml.GetElementsByTagName("text");
                textNodes[0].AppendChild(toastXml.CreateTextNode(track.Name));
                textNodes[1].AppendChild(toastXml.CreateTextNode(track.Artist));

                if (!string.IsNullOrEmpty(track.Image))
                {
                    var imageNodes = toastXml.GetElementsByTagName("image");
                    ((XmlElement)imageNodes[0]).SetAttribute("src", track.Image);
                }

                var toast = new ToastNotification(toastXml);
                
                // Set expiration to 5 seconds
                toast.ExpirationTime = DateTimeOffset.Now.AddSeconds(5);
                
                ToastNotificationManager.CreateToastNotifier("SpotifyOverlay").Show(toast);
            }
            catch (Exception ex)
            {
                Logging.BackendLogger.Instance.Log("NOTIFY", $"Failed to show toast: {ex.Message}");
            }
        }
    }
}
