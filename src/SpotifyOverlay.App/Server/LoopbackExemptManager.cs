using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SpotifyOverlay.App.Server
{
    public static class LoopbackExemptManager
    {
        private const string PackageFamilyName = "SpotifyOverlay.GameBar_r0fzewy8edw34";

        public static bool IsExempt()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "CheckNetIsolation.exe",
                Arguments = "LoopbackExempt -s",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output.Contains(PackageFamilyName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool AddExemption()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "CheckNetIsolation.exe",
                Arguments = $"LoopbackExempt -a -n={PackageFamilyName}",
                UseShellExecute = true,
                Verb = "runas" // Request UAC
            };

            try
            {
                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch (Exception)
            {
                // User cancelled UAC prompt or it failed
                return false;
            }
        }
    }
}
