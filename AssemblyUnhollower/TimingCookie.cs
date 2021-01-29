using System;
using System.Diagnostics;
using UnhollowerBaseLib;

namespace AssemblyUnhollower
{
    internal readonly struct TimingCookie : IDisposable
    {
        private readonly Stopwatch myStopwatch;
        public TimingCookie(string message)
        {
            LogSupport.Info(message + "... ");
            myStopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            LogSupport.Info($"Done in {myStopwatch.Elapsed}");
        }
    }
}