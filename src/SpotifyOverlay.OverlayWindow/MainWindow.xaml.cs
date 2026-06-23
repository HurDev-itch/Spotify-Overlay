using System;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace SpotifyOverlay.OverlayWindow
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync();
        }

        async void InitializeAsync()
        {
            var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpotifyOverlay", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await webView.EnsureCoreWebView2Async(environment);

            // We can map a virtual host to the Overwolf HTML folder to bypass CORS / File:// restrictions
            var appDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "SpotifyOverlay.Overwolf"));
            
            // Allow clicking through if background is transparent by setting Window hooks, 
            // but for now, we just want to load the UI.

            if (Directory.Exists(appDir))
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpotifyOverlay", "connection.json");
                if (File.Exists(configPath))
                {
                    var configJson = File.ReadAllText(configPath);
                    await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($"window.__INJECTED_CONFIG__ = {configJson};");
                }

                webView.WebMessageReceived += WebView_WebMessageReceived;
                webView.NavigationCompleted += (s, e) =>
                {
                    if (!e.IsSuccess)
                    {
                        MessageBox.Show($"WebView2 Error: {e.WebErrorStatus}", "Navigation Failed");
                    }
                    else
                    {
                        // Apply initial click-through state
                        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                        Win32Interop.MakeWindowUnfocusable(hwnd);
                        Win32Interop.SetClickThrough(hwnd, true);
                        Win32Interop.SetTopMostNoActivate(hwnd);
                        StartTopMostWatchdog();
                    }
                };

                webView.CoreWebView2.SetVirtualHostNameToFolderMapping("spotify-overlay.local", appDir, CoreWebView2HostResourceAccessKind.Allow);
                webView.CoreWebView2.Navigate("http://spotify-overlay.local/index.html");
            }
            else
            {
                MessageBox.Show($"Could not find HTML path: {appDir}\nMake sure SpotifyOverlay.Overwolf exists.");
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        public const uint GW_HWNDPREV = 3;

        private System.Windows.Threading.DispatcherTimer _watchdogTimer;
        private int _recoveryCount = 0;

        private void StartTopMostWatchdog()
        {
            _watchdogTimer = new System.Windows.Threading.DispatcherTimer();
            _watchdogTimer.Interval = TimeSpan.FromSeconds(2);
            _watchdogTimer.Tick += (s, e) =>
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                
                // Detect if there is a window above us
                IntPtr prevWindow = GetWindow(hwnd, GW_HWNDPREV);
                
                if (prevWindow != IntPtr.Zero)
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    Win32Interop.SetTopMostNoActivate(hwnd);
                    sw.Stop();
                    
                    _recoveryCount++;
                    string log = $"[{DateTime.Now:O}] Watchdog Recovery #{_recoveryCount} - Duration: {sw.ElapsedMilliseconds}ms\n";
                    System.IO.File.AppendAllText("visibility.log", log);

                    // Send diagnostic update to frontend
                    try
                    {
                        var json = $"{{\"type\":\"update_metrics\", \"recoveryCount\":{_recoveryCount}}}";
                        if (webView.CoreWebView2 != null)
                        {
                            webView.CoreWebView2.PostWebMessageAsJson(json);
                        }
                    }
                    catch { }
                }
            };
            _watchdogTimer.Start();
        }

        private void WebView_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                var root = System.Text.Json.JsonDocument.Parse(message).RootElement;
                if (root.TryGetProperty("type", out var typeElement))
                {
                    string type = typeElement.GetString();
                    if (type == "set_click_through")
                    {
                        bool enabled = root.GetProperty("enabled").GetBoolean();
                        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                        Win32Interop.SetClickThrough(hwnd, enabled);
                    }
                    else if (type == "set_state")
                    {
                        string state = root.GetProperty("state").GetString();
                        if (state == "expanded")
                        {
                            this.Height = 600;
                        }
                        else
                        {
                            this.Height = 120;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("crash.log", "WebMessage Error: " + ex.ToString() + "\n");
            }
        }
    }
}