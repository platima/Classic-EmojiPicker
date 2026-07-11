using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace EmojiPicker
{
    /// <summary>
    /// Shrinks the resident footprint while the picker idles in the tray. WPF's
    /// warm visual tree keeps a large working set (~150 MB) alive, but almost all
    /// of it is reclaimable: once the window hides, compact the GC heap and ask
    /// Windows to empty the working set. Pages fault back in on the next open,
    /// which costs a few milliseconds once instead of holding the memory 24/7.
    /// </summary>
    internal static class MemoryTrimmer
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr min, IntPtr max);

        public static void Trim()
        {
            try
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();

                // min = max = -1 asks Windows to page out everything not pinned;
                // the OS brings pages back on demand
                SetProcessWorkingSetSize(GetCurrentProcess(), new IntPtr(-1), new IntPtr(-1));
                Logger.Log($"MemoryTrimmer: trimmed, working set now {Environment.WorkingSet / (1024 * 1024)} MB");
            }
            catch (Exception)
            {
                // Trimming is opportunistic; never let it break the picker
            }
        }
    }
}
