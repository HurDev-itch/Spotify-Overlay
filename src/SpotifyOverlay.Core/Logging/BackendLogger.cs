using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace SpotifyOverlay.Core.Logging
{
    /// <summary>
    /// Thread-safe singleton logger. All logging calls are non-blocking
    /// and guaranteed to never throw exceptions to callers.
    /// </summary>
    public sealed class BackendLogger : IDisposable
    {
        private static readonly Lazy<BackendLogger> _instance = new(() => new BackendLogger());
        public static BackendLogger Instance => _instance.Value;

        private readonly BlockingCollection<string> _queue;
        private readonly Thread _writerThread;
        private readonly string _logPath;
        private bool _disposed;

        private BackendLogger()
        {
            _logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "backend_debug.log");

            _queue = new BlockingCollection<string>(boundedCapacity: 10000);

            _writerThread = new Thread(ProcessQueue)
            {
                IsBackground = true,
                Name = "BackendLogger-Writer"
            };
            _writerThread.Start();
        }

        /// <summary>
        /// Enqueue a log line. Never throws. Never blocks the caller
        /// beyond the time to enqueue (bounded queue will drop if full).
        /// </summary>
        public void Log(string category, string message)
        {
            try
            {
                var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{category}] {message}";
                System.Diagnostics.Debug.WriteLine(line);

                // TryAdd returns false if the queue is full; we silently drop.
                _queue.TryAdd(line);
            }
            catch
            {
                // Logging must NEVER throw.
            }
        }

        private void ProcessQueue()
        {
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(_logPath, append: true)
                {
                    AutoFlush = true
                };

                foreach (var line in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        writer.WriteLine(line);
                    }
                    catch
                    {
                        // Disk full, permission error, etc. — swallow silently.
                    }
                }
            }
            catch
            {
                // If we can't even open the file, degrade gracefully:
                // drain the queue so producers don't block.
                try
                {
                    foreach (var _ in _queue.GetConsumingEnumerable()) { }
                }
                catch { }
            }
            finally
            {
                writer?.Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _queue.CompleteAdding();
            _writerThread.Join(timeout: TimeSpan.FromSeconds(3));
            _queue.Dispose();
        }
    }
}
