using System;
using System.Diagnostics;

namespace nhitomi
{
    public  class MeasureContext : IDisposable
    {
        public readonly Stopwatch Watch = Stopwatch.StartNew();

        // ReSharper disable MemberCanBePrivate.Global
        public TimeSpan Elapsed
        {
            get
            {
                Watch.Stop();
                return Watch.Elapsed;
            }
        }

        public double Hours => Elapsed.TotalHours;
        public double Minutes => Elapsed.TotalMinutes;
        public double Seconds => Elapsed.TotalSeconds;
        public double Milliseconds => Elapsed.TotalMilliseconds;

        // ReSharper restore MemberCanBePrivate.Global

        public override string ToString()
        {
            var elapsed = Elapsed;

            if (elapsed.TotalMilliseconds <= 1000)
                return $"{elapsed.TotalMilliseconds:F}ms";

            if (elapsed.TotalSeconds <= 60)
                return $"{elapsed.TotalSeconds:F}s";

            if (elapsed.TotalMinutes <= 60)
                return $"{elapsed.TotalMinutes:F}m";

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (elapsed.TotalHours <= 24)
                return $"{elapsed.TotalHours:F}h";

            return elapsed.ToString();
        }

        public void Dispose() => Watch.Stop();
    }
}