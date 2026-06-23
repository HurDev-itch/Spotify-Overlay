using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;

namespace SpotifyOverlay.GameBar.Models
{
    public class LyricLineUIModel : INotifyPropertyChanged
    {
        public long TimeMs { get; set; }
        public string Text { get; set; }

        private SolidColorBrush _foreground;
        public SolidColorBrush Foreground
        {
            get => _foreground;
            set
            {
                _foreground = value;
                OnPropertyChanged();
            }
        }

        private Windows.UI.Text.FontWeight _fontWeight;
        public Windows.UI.Text.FontWeight FontWeight
        {
            get => _fontWeight;
            set
            {
                _fontWeight = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
