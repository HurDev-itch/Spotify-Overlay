let ws = null;
let reconnectInterval = null;
let heartbeatInterval = null;

const statusEl = document.getElementById('status');
const trackNameEl = document.getElementById('track-name');
const artistNameEl = document.getElementById('artist-name');
const artworkEl = document.getElementById('artwork');
const iconPlay = document.getElementById('icon-play');
const iconPause = document.getElementById('icon-pause');

// Make the window draggable
overwolf.windows.getCurrentWindow(result => {
    if (result.status === "success") {
        document.getElementById('drag-region').onmousedown = () => {
            overwolf.windows.dragMove(result.window.id);
        };
    }
});

function init() {
    if (typeof overwolf !== 'undefined') {
        const configPath = overwolf.io.paths.localAppData + "/SpotifyOverlay/connection.json";
        overwolf.io.readTextFile(configPath, { encoding: overwolf.io.enums.eEncoding.UTF8 }, (result) => {
            if (result.status === "success") {
                try {
                    const config = JSON.parse(result.content);
                    connectWebSocket(config.WebSocketPort);
                } catch (e) {
                    statusEl.innerText = "Error parsing config";
                    retryInit();
                }
            } else {
                statusEl.innerText = "Waiting for Backend...";
                retryInit();
            }
        });
    } else if (window.__INJECTED_CONFIG__) {
        // We are running inside native WebView2
        connectWebSocket(window.__INJECTED_CONFIG__.WebSocketPort);
    } else {
        statusEl.innerText = "Waiting for Config...";
        retryInit();
    }
}

function retryInit() {
    setTimeout(init, 3000); // Try to find the backend every 3 seconds
}

function connectWebSocket(port) {
    if (ws !== null) {
        ws.close();
    }

    statusEl.innerText = `Connecting to ws://127.0.0.1:${port}...`;
    ws = new WebSocket(`ws://127.0.0.1:${port}`);

    ws.onopen = () => {
        statusEl.innerText = "Connected";
        statusEl.style.color = "#1DB954";
        diagnosticState.wsStatus = "Connected";
        updateDiagnosticPanel();
        
        if (reconnectInterval) clearInterval(reconnectInterval);
        
        // Setup Heartbeat
        heartbeatInterval = setInterval(() => {
            if (ws.readyState === WebSocket.OPEN) {
                ws.send("ping");
            }
        }, 5000);
    };

    ws.onmessage = (event) => {
        if (event.data === "pong") return; // Ignore heartbeat responses
        
        try {
            const data = JSON.parse(event.data);
            
            if (data.type === "playback_state") {
                trackNameEl.textContent = data.track_name || 'No Track';
                artistNameEl.textContent = data.artist || 'No Artist';
                if (data.artwork_url) {
                    albumArtEl.src = data.artwork_url;
                }
            }
            else if (data.type === "set_state") {
                diagnosticState.overlayState = data.state;
                updateDiagnosticPanel();
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage(event.data);
                }
            }
            else if (data.type === "set_click_through") {
                diagnosticState.clickThrough = data.enabled;
                updateDiagnosticPanel();
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage(event.data);
                }
            }
            else if (data.type === "toggle_diagnostic") {
                if (diagnosticPanel) {
                    diagnosticPanel.style.display = data.enabled ? 'block' : 'none';
                }
            }
            else {
                updateUI(data);
            }
        } catch (e) {
            console.error("Failed to parse WebSocket message", e);
        }
    };

    ws.onclose = () => {
        cleanup();
        statusEl.innerText = "Disconnected. Reconnecting...";
        statusEl.style.color = "#b3b3b3";
        diagnosticState.wsStatus = "Disconnected";
        updateDiagnosticPanel();
        reconnectInterval = setTimeout(() => connectWebSocket(port), 3000);
    };

    ws.onerror = (error) => {
        console.error("WebSocket Error:", error);
        ws.close();
    };
}

function cleanup() {
    if (heartbeatInterval) {
        clearInterval(heartbeatInterval);
        heartbeatInterval = null;
    }
}

function updateUI(state) {
    if (state.track_name) {
        trackNameEl.innerText = state.track_name;
    }
    if (state.artist_name) {
        artistNameEl.innerText = state.artist_name;
    }
    if (state.artwork_path) {
        artworkEl.style.backgroundImage = `url('${state.artwork_path}')`;
    }
    
    if (state.is_playing) {
        iconPlay.style.display = 'none';
        iconPause.style.display = 'block';
    } else {
        iconPlay.style.display = 'block';
        iconPause.style.display = 'none';
    }
}

// Button Listeners
document.getElementById('btn-play').onclick = () => {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({ type: 'PLAY_PAUSE' }));
    }
};

document.getElementById('btn-prev').onclick = () => {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({ type: 'PREV_TRACK' }));
    }
};

document.getElementById('btn-next').onclick = () => {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({ type: 'NEXT_TRACK' }));
    }
};

// Start
init();
