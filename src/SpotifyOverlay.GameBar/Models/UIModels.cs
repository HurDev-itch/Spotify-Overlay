using System;
using System.ComponentModel;
using Windows.UI.Xaml.Media.Imaging;

namespace SpotifyOverlay.GameBar.Models
{
    public class TrackUIModel : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string ImageUrl { get; set; }
        public string Uri { get; set; }
        public string ItemType { get; set; }

        public string ItemTypeBadge => string.IsNullOrEmpty(ItemType) ? "" : ItemType.ToUpper();

        private BitmapImage _imageSource;
        public BitmapImage ImageSource
        {
            get
            {
                if (_imageSource == null && !string.IsNullOrEmpty(ImageUrl))
                {
                    _imageSource = new BitmapImage(new System.Uri(ImageUrl));
                }
                return _imageSource;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class PlaylistUIModel : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int TrackCount { get; set; }
        public string ImageUrl { get; set; }
        public string Uri { get; set; }

        private BitmapImage _imageSource;
        public BitmapImage ImageSource
        {
            get
            {
                if (_imageSource == null && !string.IsNullOrEmpty(ImageUrl))
                {
                    _imageSource = new BitmapImage(new System.Uri(ImageUrl));
                }
                return _imageSource;
            }
        }

        public string Subtitle => $"{TrackCount} tracks";

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class QueueUIModel
    {
        public TrackUIModel Current { get; set; }
        public System.Collections.ObjectModel.ObservableCollection<TrackUIModel> UpNext { get; set; } = new System.Collections.ObjectModel.ObservableCollection<TrackUIModel>();
    }

    public class PlaybackStateUIModel : INotifyPropertyChanged
    {
        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
            }
        }

        private double _volume;
        public double Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Volume)));
            }
        }

        private TrackUIModel _currentTrack;
        public TrackUIModel CurrentTrack
        {
            get => _currentTrack;
            set
            {
                _currentTrack = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTrack)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ArtistUIModel : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int Followers { get; set; }
        public System.Collections.Generic.List<string> Genres { get; set; } = new System.Collections.Generic.List<string>();
        public int Popularity { get; set; }
        public string Uri { get; set; }

        private BitmapImage _imageSource;
        public BitmapImage ImageSource
        {
            get
            {
                if (_imageSource == null && !string.IsNullOrEmpty(ImageUrl))
                {
                    _imageSource = new BitmapImage(new System.Uri(ImageUrl));
                }
                return _imageSource;
            }
        }

        public string FollowersText => $"{Followers:N0} Followers";
        public string GenresText => Genres.Count > 0 ? string.Join(", ", Genres) : "No genres specified";
        public string PopularityText => $"Popularity: {Popularity}/100";

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
