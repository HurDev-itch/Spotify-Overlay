using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using SpotifyOverlay.GameBar.Models;

namespace SpotifyOverlay.GameBar
{
    public enum ViewState
    {
        Idle,
        Loading,
        Success,
        Empty,
        Error
    }

    public sealed partial class SpotifyOverlayWidget : Page
    {
        private XboxGameBarWidget widget = null;
        private ConnectionManager _connectionManager;
        private DispatcherTimer _searchDebounceTimer;
        private readonly IUiModelMapper _mapper;

        // --- Tab state isolation ---
        private string _activeTab = "Player";
        private int _requestGeneration = 0; // Incremented on every tab switch to invalidate stale responses

        // --- Player state (always updated regardless of active tab) ---
        private bool _isPlaying = true;
        private bool _updatingVolumeFromBackend = false;

        public ObservableCollection<TrackUIModel> SearchResults { get; set; } = new ObservableCollection<TrackUIModel>();
        public ObservableCollection<PlaylistUIModel> Playlists { get; set; } = new ObservableCollection<PlaylistUIModel>();
        public ObservableCollection<TrackUIModel> Queue { get; set; } = new ObservableCollection<TrackUIModel>();

        public SpotifyOverlayWidget()
        {
            this.InitializeComponent();

            _mapper = new UiModelMapper();

            SearchResultsList.ItemsSource = SearchResults;
            PlaylistsList.ItemsSource = Playlists;
            QueueList.ItemsSource = Queue;

            _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;

            _connectionManager = new ConnectionManager();
            _connectionManager.OnMessageReceived += ConnectionManager_OnMessageReceived;
            _ = _connectionManager.StartAsync();

            NavView.SelectedItem = NavView.MenuItems[0];
        }

        // ========== UI State Management ==========

        private void SetViewState(ViewState state, string errorMessage = null)
        {
            // Always runs on UI thread (callers must ensure this)
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
            // Increment generation to invalidate any in-flight responses
            _requestGeneration++;

            // Clear all collections
            SearchResults.Clear();
            Playlists.Clear();
            Queue.Clear();

            // Reset UI state
            SetViewState(ViewState.Idle);
        }

        // ========== Tab Navigation ==========

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
            if (tag == null) return;

            // 1. Reset all state from previous tab
            ResetTabState();

            // 2. Hide all panels
            PlayerPanel.Visibility = Visibility.Collapsed;
            SearchPanel.Visibility = Visibility.Collapsed;
            PlaylistsPanel.Visibility = Visibility.Collapsed;
            QueuePanel.Visibility = Visibility.Collapsed;

            // 3. Track the new active tab
            _activeTab = tag;

            // 4. Show the correct panel and load data
            switch (tag)
            {
                case "Player":
                    PlayerPanel.Visibility = Visibility.Visible;
                    // Player gets its state from continuous polling (playback_state) — no explicit load needed
                    break;
                case "Search":
                    SearchPanel.Visibility = Visibility.Visible;
                    // Search loads on user input, not on tab entry
                    break;
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
            }
        }

        // ========== WebSocket Message Handler ==========

        private async void ConnectionManager_OnMessageReceived(string json)
        {
            // Capture the generation at message receive time
            int genAtReceive = _requestGeneration;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    if (!JsonObject.TryParse(json, out var obj)) return;
                    if (!obj.ContainsKey("type") || obj["type"].ValueType != JsonValueType.String) return;

                    var type = obj["type"].GetString();

                    // --- Error handling (always process) ---
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

                    // --- Playback state (always process regardless of tab) ---
                    if (type == "playback_state" && obj.ContainsKey("data") && obj["data"].ValueType == JsonValueType.Object)
                    {
                        HandlePlaybackState(obj["data"].GetObject());
                        return;
                    }

                    // --- Tab-specific data: check staleness ---
                    if (genAtReceive != _requestGeneration)
                    {
                        Debug.WriteLine($"[UI] Discarding stale '{type}' response (gen {genAtReceive} != current {_requestGeneration})");
                        return;
                    }

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
                                SetViewState(Playlists.Count > 0 ? ViewState.Success : ViewState.Empty);
                            }
                            break;

                        case "queue":
                            if (_activeTab != "Queue" && _activeTab != "Player") return;
                            if (obj["data"].ValueType == JsonValueType.Object)
                            {
                                var dataObj = obj["data"].GetObject();
                                Queue.Clear();

                                if (dataObj.ContainsKey("current") && dataObj["current"].ValueType == JsonValueType.Object)
                                {
                                    var currentTrack = _mapper.MapTrack(dataObj["current"].GetObject());
                                    UpdatePlayerUI(currentTrack);
                                }

                                if (dataObj.ContainsKey("up_next") && dataObj["up_next"].ValueType == JsonValueType.Array)
                                {
                                    foreach (var itemVal in dataObj["up_next"].GetArray())
                                        Queue.Add(_mapper.MapTrack(itemVal.GetObject()));
                                }
                                SetViewState(Queue.Count > 0 ? ViewState.Success : ViewState.Empty);
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

        // ========== Playback State Handler (always active) ==========

        private void HandlePlaybackState(JsonObject dataObj)
        {
            // Play/Pause
            if (dataObj.ContainsKey("is_playing") && dataObj["is_playing"].ValueType == JsonValueType.Boolean)
            {
                _isPlaying = dataObj["is_playing"].GetBoolean();
                PlayPauseButton.Content = _isPlaying ? "\uE769" : "\uE768";
            }

            // Volume
            if (dataObj.ContainsKey("volume") && dataObj["volume"].ValueType == JsonValueType.Number)
            {
                _updatingVolumeFromBackend = true;
                VolumeSlider.Value = dataObj["volume"].GetNumber();
                _updatingVolumeFromBackend = false;
            }

            // Progress
            if (dataObj.ContainsKey("progress_ms") && dataObj["progress_ms"].ValueType == JsonValueType.Number
                && dataObj.ContainsKey("duration_ms") && dataObj["duration_ms"].ValueType == JsonValueType.Number)
            {
                long progressMs = (long)dataObj["progress_ms"].GetNumber();
                long durationMs = (long)dataObj["duration_ms"].GetNumber();
                ProgressCurrentText.Text = FormatMs(progressMs);
                ProgressDurationText.Text = FormatMs(durationMs);
                ProgressBar.Maximum = durationMs > 0 ? durationMs : 1;
                ProgressBar.Value = progressMs;
            }

            // Current track
            if (dataObj.ContainsKey("current_track") && dataObj["current_track"].ValueType == JsonValueType.Object)
            {
                var currentTrack = _mapper.MapTrack(dataObj["current_track"].GetObject());
                UpdatePlayerUI(currentTrack);
            }
        }

        // ========== Player UI ==========

        private string _currentPlayerUri;

        private void UpdatePlayerUI(TrackUIModel track)
        {
            if (track == null) return;
            TrackTitleText.Text = track.Name;
            ArtistNameText.Text = track.Artist;

            // Prevent image flicker by only changing the source if it's a new track
            if (_currentPlayerUri != track.Uri)
            {
                _currentPlayerUri = track.Uri;
                PlayerAlbumArt.Source = track.ImageSource;
            }
        }

        // ========== Search ==========

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
                var escapedQuery = query.Replace("\"", "\\\"");
                SendCommand($"{{\"command\": \"search\", \"query\": \"{escapedQuery}\"}}");
            }
            else
            {
                SearchResults.Clear();
                SetViewState(ViewState.Idle);
            }
        }

        // ========== Item Click Handlers ==========

        private void TrackItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TrackUIModel item)
            {
                UpdatePlayerUI(item);
                var escapedUri = item.Uri.Replace("\"", "\\\"");
                SendCommand($"{{\"command\": \"play\", \"uri\": \"{escapedUri}\"}}");
            }
        }

        private void PlaylistItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PlaylistUIModel item)
            {
                var escapedUri = item.Uri.Replace("\"", "\\\"");
                SendCommand($"{{\"command\": \"play\", \"uri\": \"{escapedUri}\"}}");
            }
        }

        // ========== Playback Controls ==========

        private void SendCommand(string json)
        {
            _ = _connectionManager.SendMessageAsync(json);
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            _isPlaying = !_isPlaying;
            PlayPauseButton.Content = _isPlaying ? "\uE769" : "\uE768";
            var command = _isPlaying ? "resume" : "pause";
            SendCommand($"{{\"command\": \"{command}\"}}");
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            SendCommand("{\"command\": \"previous\"}");
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            SendCommand("{\"command\": \"next\"}");
        }

        private void VolumeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_updatingVolumeFromBackend) return;
            var volume = e.NewValue;
            SendCommand($"{{\"command\": \"set_volume\", \"volume\": {volume.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
        }

        // ========== Utilities ==========

        private static string FormatMs(long ms)
        {
            var ts = TimeSpan.FromMilliseconds(ms);
            return ts.TotalHours >= 1
                ? ts.ToString(@"h\:mm\:ss")
                : ts.ToString(@"m\:ss");
        }

        // ========== Navigation Lifecycle ==========

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            widget = e.Parameter as XboxGameBarWidget;
            if (widget != null)
            {
                widget.GameBarDisplayModeChanged += Widget_GameBarDisplayModeChanged;
            }
        }

        private void Widget_GameBarDisplayModeChanged(XboxGameBarWidget sender, object args)
        {
        }
    }
}
