using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ZenitharClient.Src
{
    public static class AppExitWatcher
    {
        public static bool active = false;

        public static async Task<bool> WaitForESOExit()
        {
            if (active)
            {
                return false; // Already waiting for ESO to exit, return false to indicate we didn't start a new wait
            }

            active = true;

            var p = Process.GetProcessesByName("eso64").FirstOrDefault();
            if (p == null)
            {
                // Process not found, assume ESO is not running and exit immediately
                active = false;
                return true;
            }

            // Wait for the process to exit, then return
            await Task.Run(() => p.WaitForExit());
            active = false;
            return true;
        }
    }
}
