# SpotifyOverlay

A modern Spotify overlay for Xbox Game Bar that allows you to control Spotify directly inside your games without DLL injection, game hooks, or anti-cheat risks.

SpotifyOverlay uses a split architecture:

* **SpotifyOverlay.App** (.NET 8 Backend)
* **SpotifyOverlay.GameBar** (Xbox Game Bar Widget)
* **WebSocket Communication Layer**
* **Spotify Web API Integration**

The goal is to provide a lightweight, responsive, and fullscreen-compatible Spotify experience while gaming.

---

# Features

## Current Features

### Playback Controls

* Play / Pause
* Next Track
* Previous Track
* Volume Control

### Now Playing

* Track Name
* Artist Name
* Album Artwork
* Playback Progress

### Xbox Game Bar Integration

* Native Xbox Game Bar Widget
* Works over games supported by Game Bar
* No DLL Injection
* No DirectX Hooking
* No Anti-Cheat Risks

### Backend Services

* Spotify OAuth Authentication
* Automatic Token Refresh
* Local Artwork Cache
* WebSocket Communication

---

# Planned Features

## Version 1.1

* Search Tracks
* Search Artists
* Search Albums
* Playlist Browser
* Queue Viewer
* Improved Playback Synchronization

## Version 1.2

* Lyrics Support (Soon)
* Compact / Expanded Modes (Soon)
* Theme Customization (Soon)
* Improved Notifications (Soon)

## Future Versions

* Device Switching
* Friends Activity
* Plugin System
* Multiple Overlay Layouts
* Advanced Media Controls

---

# Architecture

```text
┌─────────────────────────────┐
│ Xbox Game Bar Widget        │
│ SpotifyOverlay.GameBar      │
└─────────────┬───────────────┘
              │
         WebSocket
              │
┌─────────────▼───────────────┐
│ SpotifyOverlay.App          │
│ .NET 8 Backend             │
└─────────────┬───────────────┘
              │
              ▼
      Spotify Web API
```

---

# Project Structure

```text
SpotifyOverlay/
│
├── src/
│   ├── SpotifyOverlay.App/
│   ├── SpotifyOverlay.Core/
│   ├── SpotifyOverlay.IPC/
│   └── SpotifyOverlay.GameBar/
│
├── docs/
│
└── README.md
```

---

# Requirements

## Runtime

* Windows 10 19041+
* Windows 11
* Xbox Game Bar Installed
* Spotify Premium Account

## Development

* Visual Studio 2022
* .NET 8 SDK
* UWP Development Workload
* Xbox Game Bar SDK

---

# Installation

## Clone Repository

```bash
git clone https://github.com/yourname/SpotifyOverlay.git
```

---

## Open Solution

Open:

```text
SpotifyOverlay.sln
```

using Visual Studio 2022.

---

## Configure Spotify API

Create a Spotify Application in the Spotify Developer Dashboard.

Configure:

```text
Client ID
Client Secret
Redirect URI
```

inside the backend configuration.

---

## Build

Build:

```text
SpotifyOverlay.App
SpotifyOverlay.GameBar
```

using Release x64.

---

# Spotify Permissions

SpotifyOverlay requires the following scopes:

```text
user-read-playback-state
user-modify-playback-state
user-read-currently-playing

playlist-read-private
playlist-read-collaborative

user-library-read
```

These permissions are required for:

* Playback Control
* Queue Management
* Playlist Access
* Search Features

---

# Security

SpotifyOverlay never stores Spotify credentials.

Authentication uses:

* OAuth 2.0 Authorization Code Flow
* Refresh Tokens
* Local Secure Storage

The Game Bar Widget never communicates directly with Spotify.

All Spotify communication is handled by the backend.

---

# Performance Goals

Target resource usage:

```text
CPU: < 1%
RAM: < 150 MB
GPU: Minimal
```

The backend caches:

* Album Artwork
* Playlist Metadata
* Search Results

to reduce Spotify API requests.

---

# Roadmap

## Stabilization

* Fix Search
* Fix Playlists
* Fix Queue
* Fix Playback Synchronization

## Version 1.1

* Search
* Playlists
* Queue

## Version 1.2

* Lyrics (Soon)
* Themes (Soon)
* Notifications (Soon)

## Version 2.0

* Plugin System
* Multiple Widgets
* Device Management

---

# Contributing

Pull requests are welcome.

Before submitting a PR:

* Follow existing code style
* Test on Windows 10 and Windows 11
* Verify Game Bar compatibility
* Document major changes

---

# License

This project is currently under development.

License will be defined before the first stable public release.

---

# Disclaimer

SpotifyOverlay is an independent project and is not affiliated with, endorsed by, or sponsored by Spotify AB or Microsoft Corporation.

Spotify is a trademark of Spotify AB.

Xbox and Xbox Game Bar are trademarks of Microsoft Corporation.
