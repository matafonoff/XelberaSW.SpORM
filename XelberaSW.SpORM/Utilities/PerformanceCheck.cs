using System;
using System.Diagnostics;

namespace XelberaSW.SpORM.Utilities
{
    public class PerformanceCheck : IDisposable
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly string _prefix;

        public PerformanceCheck(string prefix = null)
        {
            _prefix = prefix;

            Trace.WriteLine($" << {prefix}");

            _stopwatch.Start();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _stopwatch.Stop();

            Trace.WriteLine($" >> {_prefix} {_stopwatch.Elapsed}");
        }
    }
}
