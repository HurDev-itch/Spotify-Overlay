using System;
using System.ComponentModel;
using Windows.Data.Json;
using Windows.UI.Xaml.Media.Imaging;

namespace SpotifyOverlay.GameBar.Models
{
    public class UnifiedItem : INotifyPropertyChanged
    {
        public string ItemType { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
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

        public static UnifiedItem FromJsonObject(JsonObject json)
        {
            return new UnifiedItem
            {
                ItemType = json.ContainsKey("item_type") && json["item_type"].ValueType == JsonValueType.String ? json["item_type"].GetString() : "",
                Title = json.ContainsKey("title") && json["title"].ValueType == JsonValueType.String ? json["title"].GetString() : "",
                Subtitle = json.ContainsKey("subtitle") && json["subtitle"].ValueType == JsonValueType.String ? json["subtitle"].GetString() : "",
                ImageUrl = json.ContainsKey("image") && json["image"].ValueType == JsonValueType.String ? json["image"].GetString() : "",
                Uri = json.ContainsKey("uri") && json["uri"].ValueType == JsonValueType.String ? json["uri"].GetString() : ""
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
