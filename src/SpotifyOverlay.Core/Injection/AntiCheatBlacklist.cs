using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SpotifyOverlay.Core.Injection
{
    public static class AntiCheatBlacklist
    {
        // A list of common anti-cheat protected executables or parent processes.
        // Injecting into these can cause permanent bans for the user.
        private static readonly HashSet<string> _blacklistedProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            // Easy Anti-Cheat
            "EasyAntiCheat.exe",
            "EasyAntiCheat_Setup.exe",
            "eac_launcher.exe",
            "rustclient.exe",
            "r5apex.exe", // Apex Legends
            "fortniteclient-win64-shipping.exe",
            
            // BattlEye
            "BEService.exe",
            "RainbowSix.exe",
            "RainbowSix_BE.exe",
            "tslgame.exe", // PUBG
            "EscapeFromTarkov.exe",

            // Riot Vanguard
            "vgc.exe",
            "vgtray.exe",
            "VALORANT-Win64-Shipping.exe",
            "League of Legends.exe",

            // FACEIT
            "faceitclient.exe",
            "csgo.exe", // Often protected by FACEIT or VAC
            "cs2.exe",

            // Others
            "Destiny2.exe",
            "Overwatch.exe",
            "cod.exe", // Call of Duty (Ricochet)
        };

        public static bool IsSafeToInject(Process process)
        {
            if (process == null || process.HasExited)
                return false;

            try
            {
                var processName = process.ProcessName + ".exe";
                
                // Direct match
                if (_blacklistedProcesses.Contains(processName))
                    return false;
                
                // TODO: In a more robust system, we would also verify if EAC/BE modules are loaded inside the process memory
                // or if the process was launched by an anti-cheat launcher.

                return true;
            }
            catch
            {
                // If we can't read the process, it's safer to deny injection
                return false;
            }
        }
    }
}
