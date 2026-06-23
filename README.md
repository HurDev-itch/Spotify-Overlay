# SpotifyOverlay

A modern Spotify overlay for Xbox Game Bar that allows you to control Spotify directly inside your games without DLL injection, game hooks, or anti-cheat risks.

SpotifyOverlay uses a split architecture:

* **SpotifyOverlay.App** (.NET 8 Backend)
* **SpotifyOverlay.GameBar** (Xbox Game Bar Widget)
* **WebSocket Communication Layer**
* **Spotify Web API Integration**

The goal is to provide a lightweight, responsive, and fullscreen-compatible Spotify experience while gaming.

## Features

### Current Features (Version 1.3)

* **Playback Controls:** Play / Pause, Next Track, Previous Track, Volume Control
* **Now Playing:** Track Name, Artist Name, Album Artwork, Playback Progress
* **Library Access:** Search for Tracks/Artists/Albums, and browse your Playlists!
* **Xbox Game Bar Integration:** Native widget, works over any game supported by Game Bar, zero anti-cheat risks.
* **Backend Services:** Spotify OAuth Authentication, automatic token refresh, local artwork caching, and real-time WebSocket communication.

### Planned Features

**Version 1.5**
* Lyrics Support (Soon)
* Compact / Expanded Modes (Soon)
* Theme Customization (Soon)
* Improved Notifications (Soon)

**Future Versions (2.0+)**
* Device Switching
* Friends Activity
* Plugin System
* Multiple Overlay Layouts
* Advanced Media Controls

## Architecture

```text
┌─────────────────────────────┐
│ Xbox Game Bar Widget        │
│ SpotifyOverlay.GameBar      │
└─────────────┬───────────────┘
              │ WebSocket
┌─────────────▼───────────────┐
│ SpotifyOverlay.App          │
│ .NET 8 Backend              │
└─────────────┬───────────────┘
              │ HTTPS
              ▼
       Spotify Web API
```

## Requirements

**Runtime**
* Windows 10 (19041+) or Windows 11
* Xbox Game Bar Installed
* Spotify Premium Account

**Development**
* Visual Studio 2022 or newer
* .NET 8 SDK
* UWP Development Workload
* Xbox Game Bar SDK

## Installation

### 1. Clone Repository
```bash
git clone https://github.com/HurDev-itch/Spotify-Overlay.git
```

### 2. Configure Spotify API
You will need to create a Spotify Application in the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/).
Configure the following inside the backend configuration:
* Client ID
* Client Secret
* Redirect URI

### 3. Build & Run
Open `SpotifyOverlay.slnx` using Visual Studio. Build the following projects in `Release x64`:
* `SpotifyOverlay.App`
* `SpotifyOverlay.GameBar`

## Spotify Permissions

SpotifyOverlay requires the following scopes to function properly:
* `user-read-playback-state`
* `user-modify-playback-state`
* `user-read-currently-playing`
* `playlist-read-private`
* `playlist-read-collaborative`
* `user-library-read`

## Security & Performance

**Security:** SpotifyOverlay never stores your raw Spotify credentials. Authentication uses the OAuth 2.0 Authorization Code Flow. Only secure refresh tokens are stored locally. The Game Bar Widget never communicates directly with Spotify; all communication is safely handled by the backend server.

**Performance Goals:** 
* CPU: < 1%
* RAM: < 150 MB
* GPU: Minimal

The backend caches album artwork, playlist metadata, and search results to reduce Spotify API requests and keep the UI lightning-fast.

## Contributing

Pull requests are welcome! Before submitting a PR:
* Follow the existing code style
* Test your changes on both Windows 10 and Windows 11
* Verify Game Bar compatibility
* Document any major changes

## License

This project is licensed under the **MIT License**. See the `LICENSE` file for details.

---

**Disclaimer:** SpotifyOverlay is an independent project and is not affiliated with, endorsed by, or sponsored by Spotify AB or Microsoft Corporation. Spotify is a trademark of Spotify AB. Xbox and Xbox Game Bar are trademarks of Microsoft Corporation.
