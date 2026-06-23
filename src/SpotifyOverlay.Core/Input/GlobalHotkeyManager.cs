using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpotifyOverlay.Core.Input
{
    public class GlobalHotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int WM_HOTKEY = 0x0312;

        public enum Modifiers : uint
        {
            None = 0x0000,
            Alt = 0x0001,
            Ctrl = 0x0002,
            Shift = 0x0004,
            Win = 0x0008
        }

        private readonly IntPtr _hwnd;
        private int _currentId;
        private readonly Dictionary<int, Action> _registeredCallbacks = new();

        public GlobalHotkeyManager(IntPtr hwnd)
        {
            _hwnd = hwnd;
            _currentId = 0;
        }

        public int Register(Modifiers modifiers, uint key, Action callback)
        {
            _currentId++;
            if (RegisterHotKey(_hwnd, _currentId, (uint)modifiers, key))
            {
                _registeredCallbacks[_currentId] = callback;
                return _currentId;
            }
            throw new InvalidOperationException("Couldn't register the global hotkey.");
        }

        public void Unregister(int id)
        {
            if (_registeredCallbacks.ContainsKey(id))
            {
                UnregisterHotKey(_hwnd, id);
                _registeredCallbacks.Remove(id);
            }
        }

        public void HandleWindowMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_registeredCallbacks.TryGetValue(id, out var callback))
                {
                    callback.Invoke();
                }
            }
        }

        public void Dispose()
        {
            foreach (var id in _registeredCallbacks.Keys)
            {
                UnregisterHotKey(_hwnd, id);
            }
            _registeredCallbacks.Clear();
        }
    }
}
