using Microsoft.UI.Xaml;
using SpotifyOverlay.Core.OAuth;
using SpotifyOverlay.Core.Spotify;

namespace SpotifyOverlay.App
{
    public partial class App : Application
    {
        private Window m_window;
        private Server.BackendServer _server;

        public OAuthManager OAuthManager { get; private set; }
        public SpotifyClient SpotifyClient { get; private set; }
        public SpotifyStateService SpotifyStateService { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var config = new SpotifyConfig 
            {
                ClientId = "2b802f99de6e43e49ed83d8f55014175"
            };
            OAuthManager = new OAuthManager(config);
            SpotifyClient = new SpotifyClient(OAuthManager);
            
            var serializer = new SpotifyOverlay.Core.Spotify.Protocol.ProtocolSerializer();
            SpotifyStateService = new SpotifyStateService(SpotifyClient, serializer);

            _server = new Server.BackendServer(SpotifyStateService);
            _server.StartAsync().ConfigureAwait(false);

            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}
