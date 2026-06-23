using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using SpotifyOverlay.GameBar.Models;

namespace SpotifyOverlay.GameBar
{
    public enum ViewState
    {
        Idle, Loading, Success, Empty, Error
    }

    public sealed partial class SpotifyOverlayWidget : Page
    {
        private XboxGameBarWidget widget = null;
        private ConnectionManager _connectionManager;
        private DispatcherTimer _searchDebounceTimer;
        private readonly IUiModelMapper _mapper;

        private string _activeTab = "Player";
        private int _requestGeneration = 0;
        private bool _isPlaying = true;
        private bool _updatingVolumeFromBackend = false;
        private bool _isCompactMode = false;
        private bool _isSettingsLoaded = false;

        public ObservableCollection<TrackUIModel> SearchResults { get; set; } = new ObservableCollection<TrackUIModel>();
        public ObservableCollection<PlaylistUIModel> Playlists { get; set; } = new ObservableCollection<PlaylistUIModel>();
        public ObservableCollection<TrackUIModel> Queue { get; set; } = new ObservableCollection<TrackUIModel>();
        public ObservableCollection<TrackUIModel> CurrentPlaylistTracks { get; set; } = new ObservableCollection<TrackUIModel>();
        public ObservableCollection<LyricLineUIModel> Lyrics { get; set; } = new ObservableCollection<LyricLineUIModel>();
        public ObservableCollection<TrackUIModel> CurrentArtistTracks { get; set; } = new ObservableCollection<TrackUIModel>();
        
        public PlaylistUIModel CurrentPlaylist { get; set; }
        public ArtistUIModel CurrentArtist { get; set; }
        private int _playlistTracksTotal = 0;
        private int _nextOffset = 0;
        private bool _isLoadingMoreTracks = false;

        public SpotifyOverlayWidget()
        {
            this.InitializeComponent();
            _mapper = new UiModelMapper();

            SearchResultsList.ItemsSource = SearchResults;
            PlaylistsList.ItemsSource = Playlists;
            QueueList.ItemsSource = Queue;
            PlaylistTracksList.ItemsSource = CurrentPlaylistTracks;
            ArtistTracksList.ItemsSource = CurrentArtistTracks;
            LyricsList.ItemsSource = Lyrics;

            _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;

            _connectionManager = new ConnectionManager();
            _connectionManager.OnMessageReceived += ConnectionManager_OnMessageReceived;
            _ = _connectionManager.StartAsync();

            NavView.SelectedItem = NavView.MenuItems[0];
            
            // Request settings on load
            SendCommand("{\"command\": \"get_settings\"}");
        }

        private void SetViewState(ViewState state, string errorMessage = null)
        {
            LoadingSpinner.IsActive = state == ViewState.Loading;
            EmptyStateText.Visibility = state == ViewState.Empty ? Visibility.Visible : Visibility.Collapsed;
            if (state == ViewState.Error && errorMessage != null)
            {
                ErrorStatePanel.Visibility = Visibility.Visible;
                ErrorStateText.Text = errorMessage;
            }
            else
            {
                ErrorStatePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ResetTabState()
        {
            _requestGeneration++;
            SearchResults.Clear();
            Playlists.Clear();
            Queue.Clear();
            CurrentPlaylistTracks.Clear();
            Lyrics.Clear();
            LyricsUnavailableText.Visibility = Visibility.Collapsed;
            CurrentPlaylist = null;
            CurrentArtist = null;
            CurrentArtistTracks.Clear();
            _playlistTracksTotal = 0;
            _nextOffset = 0;
            _isLoadingMoreTracks = false;
            SetViewState(ViewState.Idle);
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
            if (tag == null) return;

            ResetTabState();

            PlayerPanel.Visibility = Visibility.Collapsed;
            SearchPanel.Visibility = Visibility.Collapsed;
            PlaylistsPanel.Visibility = Visibility.Collapsed;
            QueuePanel.Visibility = Visibility.Collapsed;
            PlaylistDetailPanel.Visibility = Visibility.Collapsed;
            ArtistDetailPanel.Visibility = Visibility.Collapsed;
            LyricsPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;

            _activeTab = tag;

            switch (tag)
            {
                case "Player": PlayerPanel.Visibility = Visibility.Visible; break;
                case "Search": SearchPanel.Visibility = Visibility.Visible; break;
                case "Playlists":
                    PlaylistsPanel.Visibility = Visibility.Visible;
                    SetViewState(ViewState.Loading);
                    SendCommand("{\"command\": \"get_playlists\"}");
                    break;
                case "Queue":
                    QueuePanel.Visibility = Visibility.Visible;
                    SetViewState(ViewState.Loading);
                    SendCommand("{\"command\": \"get_queue\"}");
                    break;
                case "Lyrics":
                    LyricsPanel.Visibility = Visibility.Visible;
                    SetViewState(ViewState.Loading);
                    // Lyrics are pushed by backend automatically on track change, but we can display the current cache
                    break;
                case "Settings":
                    SettingsPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private async void ConnectionManager_OnMessageReceived(string json)
        {
            int genAtReceive = _requestGeneration;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    if (!JsonObject.TryParse(json, out var obj)) return;
                    if (!obj.ContainsKey("type") || obj["type"].ValueType != JsonValueType.String) return;

                    var type = obj["type"].GetString();

                    if (type == "error")
                    {
                        if (obj.ContainsKey("error") && obj["error"].ValueType == JsonValueType.Object)
                        {
                            var err = obj["error"].GetObject();
                            var msg = err.ContainsKey("message") ? err["message"].GetString() : "Unknown Error";
                            SetViewState(ViewState.Error, msg);
                        }
                        return;
                    }

                    if (type == "settings")
                    {
                        if (obj.ContainsKey("data") && obj["data"].ValueType == JsonValueType.Object)
                        {
                            ApplySettings(obj["data"].GetObject());
                        }
                        return;
                    }

                    if (type == "playback_state" && obj.ContainsKey("data") && obj["data"].ValueType == JsonValueType.Object)
                    {
                        HandlePlaybackState(obj["data"].GetObject());
                        return;
                    }

                    if (type == "lyrics" && obj.ContainsKey("data") && obj["data"].ValueType == JsonValueType.Object)
                    {
                        HandleLyrics(obj["data"].GetObject());
                        return;
                    }

                    if (genAtReceive != _requestGeneration) return;

                    if (!obj.ContainsKey("data")) return;

                    switch (type)
                    {
                        case "search_results":
                            if (_activeTab != "Search") return;
                            if (obj["data"].ValueType == JsonValueType.Array)
                            {
                                SearchResults.Clear();
                                foreach (var itemVal in obj["data"].GetArray())
                                    SearchResults.Add(_mapper.MapTrack(itemVal.GetObject()));
                                SetViewState(SearchResults.Count > 0 ? ViewState.Success : ViewState.Empty);
                            }
                            break;

                        case "playlists":
                            if (_activeTab != "Playlists") return;
                            if (obj["data"].ValueType == JsonValueType.Array)
                            {
                                Playlists.Clear();
                                foreach (var itemVal in obj["data"].GetArray())
                                    Playlists.Add(_mapper.MapPlaylist(itemVal.GetObject()));
                                if (PlaylistDetailPanel.Visibility != Visibility.Visible)
                                    SetViewState(Playlists.Count > 0 ? ViewState.Success : ViewState.Empty);
                            }
                            break;

                        case "playlist_tracks":
                            if (_activeTab != "Playlists" || CurrentPlaylist == null) return;
                            if (obj["data"].ValueType == JsonValueType.Object)
                            {
                                var dataObj = obj["data"].GetObject();
                                if (obj.ContainsKey("playlist_id") && obj["playlist_id"].GetString() != CurrentPlaylist.Id) return;
                                if (dataObj.ContainsKey("total")) _playlistTracksTotal = (int)dataObj["total"].GetNumber();
                                
                                int offset = dataObj.ContainsKey("offset") ? (int)dataObj["offset"].GetNumber() : 0;
                                int limit = dataObj.ContainsKey("limit") ? (int)dataObj["limit"].GetNumber() : 50;
                                
                                if (offset == 0) CurrentPlaylistTracks.Clear();

                                if (dataObj.ContainsKey("items") && dataObj["items"].ValueType == JsonValueType.Array)
                                {
                                    foreach (var itemVal in dataObj["items"].GetArray())
                                        CurrentPlaylistTracks.Add(_mapper.MapTrack(itemVal.GetObject()));
                                }
                                
                                _nextOffset = offset + limit;
                                _isLoadingMoreTracks = false;
                                SetViewState(CurrentPlaylistTracks.Count > 0 ? ViewState.Success : ViewState.Empty);
                            }
                            break;

                        case "queue":
                            if (_activeTab != "Queue" && _activeTab != "Player") return;
                            if (obj["data"].ValueType == JsonValueType.Object)
                            {
                                var dataObj = obj["data"].GetObject();
                                Queue.Clear();

                                if (dataObj.ContainsKey("current") && dataObj["current"].ValueType == JsonValueType.Object)
                                    UpdatePlayerUI(_mapper.MapTrack(dataObj["current"].GetObject()));

                                if (dataObj.ContainsKey("up_next") && dataObj["up_next"].ValueType == JsonValueType.Array)
                                {
                                    foreach (var itemVal in dataObj["up_next"].GetArray())
                                        Queue.Add(_mapper.MapTrack(itemVal.GetObject()));
                                }
                                SetViewState(Queue.Count > 0 ? ViewState.Success : ViewState.Empty);
                            }
                            break;

                        case "artist_details":
                            if (_activeTab != "Search" || ArtistDetailPanel.Visibility != Visibility.Visible) return;
                            if (obj["data"].ValueType == JsonValueType.Object)
                            {
                                var dataObj = obj["data"].GetObject();
                                if (dataObj.ContainsKey("artist") && dataObj["artist"].ValueType == JsonValueType.Object)
                                {
                                    CurrentArtist = _mapper.MapArtist(dataObj["artist"].GetObject());
                                    ArtistDetailImage.Source = CurrentArtist.ImageSource;
                                    ArtistDetailName.Text = CurrentArtist.Name;
                                    ArtistDetailFollowers.Text = CurrentArtist.FollowersText;
                                    ArtistDetailGenres.Text = CurrentArtist.GenresText;
                                    ArtistDetailPopularity.Text = CurrentArtist.PopularityText;
                                }

                                if (dataObj.ContainsKey("tracks") && dataObj["tracks"].ValueType == JsonValueType.Array)
                                {
                                    foreach (var itemVal in dataObj["tracks"].GetArray())
                                    {
                                        CurrentArtistTracks.Add(_mapper.MapTrack(itemVal.GetObject()));
                                    }
                                }

                                SetViewState(ViewState.Success);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[UI] Error parsing JSON: {ex.Message}");
                }
            });
        }

        private void HandlePlaybackState(JsonObject dataObj)
        {
            if (dataObj.ContainsKey("is_playing") && dataObj["is_playing"].ValueType == JsonValueType.Boolean)
            {
                _isPlaying = dataObj["is_playing"].GetBoolean();
                PlayPauseButton.Content = _isPlaying ? "\uE769" : "\uE768";
            }

            if (dataObj.ContainsKey("volume") && dataObj["volume"].ValueType == JsonValueType.Number)
            {
                _updatingVolumeFromBackend = true;
                VolumeSlider.Value = dataObj["volume"].GetNumber();
                _updatingVolumeFromBackend = false;
            }

            long progressMs = 0;
            if (dataObj.ContainsKey("progress_ms") && dataObj["progress_ms"].ValueType == JsonValueType.Number
                && dataObj.ContainsKey("duration_ms") && dataObj["duration_ms"].ValueType == JsonValueType.Number)
            {
                progressMs = (long)dataObj["progress_ms"].GetNumber();
                long durationMs = (long)dataObj["duration_ms"].GetNumber();
                ProgressCurrentText.Text = FormatMs(progressMs);
                ProgressDurationText.Text = FormatMs(durationMs);
                ProgressBar.Maximum = durationMs > 0 ? durationMs : 1;
                ProgressBar.Value = progressMs;
            }

            if (dataObj.ContainsKey("current_track") && dataObj["current_track"].ValueType == JsonValueType.Object)
            {
                UpdatePlayerUI(_mapper.MapTrack(dataObj["current_track"].GetObject()));
            }

            SyncLyrics(progressMs);
        }

        private void HandleLyrics(JsonObject dataObj)
        {
            Lyrics.Clear();
            SetViewState(ViewState.Idle);

            if (dataObj.ContainsKey("IsFound") && dataObj["IsFound"].ValueType == JsonValueType.Boolean && dataObj["IsFound"].GetBoolean())
            {
                LyricsUnavailableText.Visibility = Visibility.Collapsed;
                if (dataObj.ContainsKey("Lines") && dataObj["Lines"].ValueType == JsonValueType.Array)
                {
                    var activeBrush = (SolidColorBrush)Resources["TextBrush"];
                    foreach (var lineVal in dataObj["Lines"].GetArray())
                    {
                        var lineObj = lineVal.GetObject();
                        Lyrics.Add(new LyricLineUIModel 
                        { 
                            TimeMs = (long)lineObj["TimeMs"].GetNumber(), 
                            Text = lineObj["Text"].GetString(),
                            Foreground = activeBrush,
                            FontWeight = Windows.UI.Text.FontWeights.Normal
                        });
                    }
                }
            }
            else
            {
                LyricsUnavailableText.Visibility = Visibility.Visible;
            }
        }

        private void SyncLyrics(long progressMs)
        {
            if (Lyrics.Count == 0) return;

            int activeIndex = -1;
            for (int i = 0; i < Lyrics.Count; i++)
            {
                if (progressMs >= Lyrics[i].TimeMs)
                {
                    activeIndex = i;
                }
                else break;
            }

            var textBrush = (SolidColorBrush)Resources["TextBrush"];
            var accentBrush = (SolidColorBrush)Resources["AccentBrush"];

            for (int i = 0; i < Lyrics.Count; i++)
            {
                bool isActive = i == activeIndex;
                Lyrics[i].Foreground = isActive ? accentBrush : textBrush;
                Lyrics[i].FontWeight = isActive ? Windows.UI.Text.FontWeights.Bold : Windows.UI.Text.FontWeights.Normal;
            }

            if (activeIndex >= 0 && LyricsList.Items.Count > activeIndex)
            {
                LyricsList.ScrollIntoView(LyricsList.Items[activeIndex], ScrollIntoViewAlignment.Default);
            }
        }

        private void ApplySettings(JsonObject settings)
        {
            _isSettingsLoaded = true;
            try
            {
                string theme = settings.ContainsKey("theme") ? settings["theme"].GetString() : "SpotifyOverlay Default";
                ThemeComboBox.SelectedItem = theme;

                if (settings.ContainsKey("overlay_mode"))
                {
                    var mode = settings["overlay_mode"].GetString();
                    if (mode == "compact" && !_isCompactMode) ToggleMode();
                    else if (mode == "expanded" && _isCompactMode) ToggleMode();
                }

                if (settings.ContainsKey("notify_track_change")) NotifyTrackToggle.IsOn = settings["notify_track_change"].GetBoolean();
                if (settings.ContainsKey("notify_queue")) NotifyQueueToggle.IsOn = settings["notify_queue"].GetBoolean();
                if (settings.ContainsKey("notify_device")) NotifyDeviceToggle.IsOn = settings["notify_device"].GetBoolean();

                // Apply Theme Colors
                if (theme == "Spotify Dark")
                {
                    ((SolidColorBrush)Resources["BackgroundBrush"]).Color = Color.FromArgb(255, 18, 18, 18);
                    ((SolidColorBrush)Resources["AccentBrush"]).Color = Color.FromArgb(255, 29, 185, 84);
                }
                else if (theme == "Xbox Dark")
                {
                    ((SolidColorBrush)Resources["BackgroundBrush"]).Color = Color.FromArgb(255, 16, 16, 16);
                    ((SolidColorBrush)Resources["AccentBrush"]).Color = Color.FromArgb(255, 16, 124, 16);
                }
                else if (theme == "Fluent Light")
                {
                    ((SolidColorBrush)Resources["BackgroundBrush"]).Color = Color.FromArgb(255, 243, 243, 243);
                    ((SolidColorBrush)Resources["AccentBrush"]).Color = Color.FromArgb(255, 0, 90, 158);
                    ((SolidColorBrush)Resources["TextBrush"]).Color = Color.FromArgb(255, 0, 0, 0);
                    ((SolidColorBrush)Resources["PanelBrush"]).Color = Color.FromArgb(255, 255, 255, 255);
                }
                else if (theme == "AMOLED")
                {
                    ((SolidColorBrush)Resources["BackgroundBrush"]).Color = Color.FromArgb(255, 0, 0, 0);
                    ((SolidColorBrush)Resources["AccentBrush"]).Color = Color.FromArgb(255, 29, 185, 84);
                }
                else // SpotifyOverlay Default
                {
                    ((SolidColorBrush)Resources["BackgroundBrush"]).Color = Color.FromArgb(255, 18, 18, 18);
                    ((SolidColorBrush)Resources["AccentBrush"]).Color = Color.FromArgb(255, 29, 185, 84);
                    ((SolidColorBrush)Resources["TextBrush"]).Color = Color.FromArgb(255, 255, 255, 255);
                    ((SolidColorBrush)Resources["PanelBrush"]).Color = Color.FromArgb(255, 40, 40, 40);
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            if (!_isSettingsLoaded) return;

            string themeStr = ThemeComboBox.SelectedItem?.ToString() ?? "SpotifyOverlay Default";
            string modeStr = _isCompactMode ? "compact" : "expanded";
            
            string json = $"{{\"command\":\"save_settings\",\"data\":{{\"theme\":\"{themeStr}\",\"overlay_mode\":\"{modeStr}\",\"notify_track_change\":{NotifyTrackToggle.IsOn.ToString().ToLower()},\"notify_queue\":{NotifyQueueToggle.IsOn.ToString().ToLower()},\"notify_device\":{NotifyDeviceToggle.IsOn.ToString().ToLower()}}}}}";
            SendCommand(json);
        }

        private string _currentPlayerUri;
        private void UpdatePlayerUI(TrackUIModel track)
        {
            if (track == null) return;
            TrackTitleText.Text = track.Name;
            ArtistNameText.Text = track.Artist;
            if (_currentPlayerUri != track.Uri)
            {
                _currentPlayerUri = track.Uri;
                PlayerAlbumArt.Source = track.ImageSource;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void SearchDebounceTimer_Tick(object sender, object e)
        {
            _searchDebounceTimer.Stop();
            var query = SearchBox.Text.Trim();
            if (!string.IsNullOrEmpty(query))
            {
                SetViewState(ViewState.Loading);
                SendCommand($"{{\"command\": \"search\", \"query\": \"{query.Replace("\"", "\\\"")}\"}}");
            }
            else
            {
                SearchResults.Clear();
                SetViewState(ViewState.Idle);
            }
        }

        private void TrackItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TrackUIModel item)
            {
                if (item.ItemType == "artist" || item.Uri.Contains("artist"))
                {
                    CurrentArtist = null;
                    CurrentArtistTracks.Clear();
                    ArtistDetailImage.Source = item.ImageSource;
                    ArtistDetailName.Text = item.Name;
                    ArtistDetailFollowers.Text = "Loading...";
                    ArtistDetailGenres.Text = "";
                    ArtistDetailPopularity.Text = "";
                    
                    SearchPanel.Visibility = Visibility.Collapsed;
                    ArtistDetailPanel.Visibility = Visibility.Visible;
                    SetViewState(ViewState.Loading);
                    SendCommand($"{{\"command\": \"get_artist_details\", \"artist_id\": \"{item.Id}\"}}");
                    return;
                }

                UpdatePlayerUI(item);
                SendCommand($"{{\"command\": \"play\", \"uri\": \"{item.Uri.Replace("\"", "\\\"")}\"}}");
            }
        }

        private void PlaylistItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PlaylistUIModel item)
            {
                CurrentPlaylist = item;
                CurrentPlaylistTracks.Clear();
                _playlistTracksTotal = 0;
                PlaylistDetailCover.Source = item.ImageSource;
                PlaylistDetailName.Text = item.Name;
                PlaylistDetailCount.Text = item.Subtitle;
                PlaylistsPanel.Visibility = Visibility.Collapsed;
                PlaylistDetailPanel.Visibility = Visibility.Visible;
                SetViewState(ViewState.Loading);
                _isLoadingMoreTracks = true;
                _nextOffset = 0;
                SendCommand($"{{\"command\": \"get_playlist_tracks\", \"playlist_id\": \"{item.Id}\", \"offset\": 0}}");
            }
        }

        private void PlaylistDetailBackButton_Click(object sender, RoutedEventArgs e)
        {
            PlaylistDetailPanel.Visibility = Visibility.Collapsed;
            PlaylistsPanel.Visibility = Visibility.Visible;
            CurrentPlaylist = null;
            SetViewState(Playlists.Count > 0 ? ViewState.Success : ViewState.Empty);
        }

        private void PlaylistTrackItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TrackUIModel item && CurrentPlaylist != null)
            {
                UpdatePlayerUI(item);
                SendCommand($"{{\"command\": \"play_context\", \"context_uri\": \"{CurrentPlaylist.Uri.Replace("\"", "\\\"")}\", \"offset_uri\": \"{item.Uri.Replace("\"", "\\\"")}\"}}");
            }
        }

        private void PlaylistTracksList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!_isLoadingMoreTracks && _nextOffset < _playlistTracksTotal)
            {
                if (args.ItemIndex >= CurrentPlaylistTracks.Count - 5)
                {
                    _isLoadingMoreTracks = true;
                    SendCommand($"{{\"command\": \"get_playlist_tracks\", \"playlist_id\": \"{CurrentPlaylist.Id}\", \"offset\": {_nextOffset}}}");
                }
            }
        }

        private void ArtistDetailBackButton_Click(object sender, RoutedEventArgs e)
        {
            ArtistDetailPanel.Visibility = Visibility.Collapsed;
            SearchPanel.Visibility = Visibility.Visible;
            CurrentArtist = null;
            CurrentArtistTracks.Clear();
            SetViewState(SearchResults.Count > 0 ? ViewState.Success : ViewState.Empty);
        }

        private void ArtistDetailPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentArtist != null)
            {
                SendCommand($"{{\"command\": \"play_artist\", \"artist_uri\": \"{CurrentArtist.Uri.Replace("\"", "\\\"")}\"}}");
            }
        }

        private void ArtistTrackItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TrackUIModel item)
            {
                UpdatePlayerUI(item);
                // Top tracks aren't a proper context usually, so we'll just play the specific track.
                SendCommand($"{{\"command\": \"play\", \"uri\": \"{item.Uri.Replace("\"", "\\\"")}\"}}");
            }
        }

        private void SendCommand(string json) => _ = _connectionManager.SendMessageAsync(json);

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            _isPlaying = !_isPlaying;
            PlayPauseButton.Content = _isPlaying ? "\uE769" : "\uE768";
            SendCommand($"{{\"command\": \"{(_isPlaying ? "resume" : "pause")}\"}}");
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e) => SendCommand("{\"command\": \"previous\"}");
        private void NextButton_Click(object sender, RoutedEventArgs e) => SendCommand("{\"command\": \"next\"}");

        private void VolumeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_updatingVolumeFromBackend) return;
            SendCommand($"{{\"command\": \"set_volume\", \"volume\": {e.NewValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
        }

        private static string FormatMs(long ms)
        {
            var ts = TimeSpan.FromMilliseconds(ms);
            return ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            widget = e.Parameter as XboxGameBarWidget;
            if (widget != null) widget.GameBarDisplayModeChanged += Widget_GameBarDisplayModeChanged;
        }

        private void Widget_GameBarDisplayModeChanged(XboxGameBarWidget sender, object args) { }

        private async void ToggleMode()
        {
            _isCompactMode = !_isCompactMode;
            if (_isCompactMode)
            {
                NavView.Visibility = Visibility.Collapsed;
                ModeToggleButton.Content = "\uE73F"; // Collapse icon
                if (widget != null) await widget.TryResizeWindowAsync(new Windows.Foundation.Size(400, 120));
                
                PlayerAlbumArtBorder.Visibility = Visibility.Collapsed;
                VolumeControlGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                NavView.Visibility = Visibility.Visible;
                ModeToggleButton.Content = "\uE740"; // Expand icon
                if (widget != null) await widget.TryResizeWindowAsync(new Windows.Foundation.Size(400, 600));
                
                PlayerAlbumArtBorder.Visibility = Visibility.Visible;
                VolumeControlGrid.Visibility = Visibility.Visible;
            }
            SaveSettings();
        }

        private void Grid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => ToggleMode();
        private void ModeToggleButton_Click(object sender, RoutedEventArgs e) => ToggleMode();
        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => SaveSettings();
        private void SettingToggle_Toggled(object sender, RoutedEventArgs e) => SaveSettings();
    }
}
