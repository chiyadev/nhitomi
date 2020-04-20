using System;
using System.Diagnostics;

namespace nhitomi
{
    /// <summary>
    /// Contains system memory information.
    /// </summary>
    public class MemoryInfo
    {
        /// <summary>
        /// Process virtual memory.
        /// </summary>
        public readonly long Virtual;

        /// <summary>
        /// Process working set memory.
        /// </summary>
        public readonly long WorkingSet;

        /// <summary>
        /// Managed memory.
        /// </summary>
        public readonly long Managed;

        public MemoryInfo()
        {
            using (var process = Process.GetCurrentProcess())
            {
                Virtual    = process.VirtualMemorySize64;
                WorkingSet = process.WorkingSet64;
            }

            Managed = GC.GetTotalMemory(false);
        }
    }
}