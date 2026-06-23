using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpotifyOverlay.Core.Input;
using System;

namespace SpotifyOverlay.App
{
    public sealed partial class MainWindow : Window
    {
        private GlobalHotkeyManager _hotkeyManager;

        private SpotifyOverlay.Core.OAuth.OAuthManager _oauthManager;
        public MainWindow()
        {
            this.InitializeComponent();

            _oauthManager = ((App)Application.Current).OAuthManager;

            // Setup Global Hotkey (Using current window handle)
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            _hotkeyManager = new GlobalHotkeyManager(hwnd);
        }

        private async void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // Auto-login if we have a refresh token saved
            bool reconnected = await _oauthManager.TryLoadAndRefreshAsync();
            UpdateSpotifyUI(reconnected);
            NavView.SelectedItem = NavView.MenuItems[0];

            CheckLoopbackStatus();
        }

        private void CheckLoopbackStatus()
        {
            if (!SpotifyOverlay.App.Server.LoopbackExemptManager.IsExempt())
            {
                LoopbackInfoBar.IsOpen = true;
            }
            else
            {
                LoopbackInfoBar.IsOpen = false;
            }
        }

        private void GrantLoopback_Click(object sender, RoutedEventArgs e)
        {
            if (SpotifyOverlay.App.Server.LoopbackExemptManager.AddExemption())
            {
                LoopbackInfoBar.IsOpen = false;
                // Optionally show a success info bar
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ShowPanel(SettingsPanel);
                return;
            }

            var item = args.SelectedItem as NavigationViewItem;
            if (item != null)
            {
                switch (item.Tag?.ToString())
                {
                    case "Dashboard": ShowPanel(DashboardPanel); break;
                    case "Profiles": ShowPanel(ProfilesPanel); break;
                    case "Plugins": ShowPanel(PluginsPanel); break;
                }
            }
        }

        private void ShowPanel(StackPanel targetPanel)
        {
            DashboardPanel.Visibility = Visibility.Collapsed;
            ProfilesPanel.Visibility = Visibility.Collapsed;
            PluginsPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;

            targetPanel.Visibility = Visibility.Visible;
        }

        private void UpdateSpotifyUI(bool isLinked)
        {
            if (isLinked)
            {
                SpotifyStatusInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
                SpotifyStatusInfoBar.Title = "Spotify Linked";
                SpotifyStatusInfoBar.Message = "Your overlay is ready and listening to Spotify.";
                LinkSpotifyBtn.Visibility = Visibility.Collapsed;
                UnlinkSpotifyBtn.Visibility = Visibility.Visible;
            }
            else
            {
                SpotifyStatusInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
                SpotifyStatusInfoBar.Title = "Spotify Not Linked";
                SpotifyStatusInfoBar.Message = "You need to link your Spotify account to enable the overlay.";
                LinkSpotifyBtn.Visibility = Visibility.Visible;
                UnlinkSpotifyBtn.Visibility = Visibility.Collapsed;
            }
        }

        private async void LinkSpotify_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SpotifyStatusInfoBar.Title = "Waiting for browser...";
                SpotifyStatusInfoBar.Message = "Please complete the login in your browser.";
                
                string verifier = _oauthManager.GenerateCodeVerifier();
                string challenge = _oauthManager.GenerateCodeChallenge(verifier);
                string state = Guid.NewGuid().ToString("N");
                
                string authUrl = _oauthManager.GetAuthorizationUrl(challenge, state);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                ContentDialog dialog = new ContentDialog
                {
                    Title = "Spotify Login",
                    Content = "Browser opened! Please authenticate with Spotify and return here.",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.Content.XamlRoot
                };
                _ = dialog.ShowAsync();

                string? code = await _oauthManager.WaitForCallbackAsync();
                dialog.Hide();

                if (!string.IsNullOrEmpty(code))
                {
                    bool success = await _oauthManager.ExchangeCodeForTokenAsync(code, verifier);
                    UpdateSpotifyUI(success);
                }
                else
                {
                    UpdateSpotifyUI(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auth Error: {ex.Message}");
                UpdateSpotifyUI(false);
            }
        }

        private void UnlinkSpotify_Click(object sender, RoutedEventArgs e)
        {
            SpotifyOverlay.Core.OAuth.TokenStorage.ClearToken();
            // In a real scenario, we might also want to reset the OAuthManager state here
            UpdateSpotifyUI(false);
        }

        // We must implement IDisposable or finalizer to clean up the hotkey manager, 
        // or hook into Window.Closed event.
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            _hotkeyManager?.Dispose();
        }
    }
}
