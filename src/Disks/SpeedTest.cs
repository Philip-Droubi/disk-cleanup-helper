// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// A short read/write speed test.
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.Disks
{
    public class SpeedResult
    {
        public double WriteMBs, ReadMBs, RandomMBs, RandomIops;
        public long TestBytes;
    }

    // Writes a temporary file, reads it back, then reads small random blocks. Bypasses Windows' file cache.
    public static class SpeedTest
    {
        const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, CREATE_ALWAYS = 2, OPEN_EXISTING = 3;
        const uint FILE_FLAG_NO_BUFFERING = 0x20000000, FILE_FLAG_WRITE_THROUGH = 0x80000000, FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000, FILE_FLAG_RANDOM_ACCESS = 0x10000000;
        const uint MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000, MEM_RELEASE = 0x8000, PAGE_READWRITE = 0x04;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteFile(SafeFileHandle h, IntPtr buffer, uint count, out uint written, IntPtr overlapped);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadFile(SafeFileHandle h, IntPtr buffer, uint count, out uint read, IntPtr overlapped);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetFilePointerEx(SafeFileHandle h, long distance, out long newPosition, uint method);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAlloc(IntPtr address, UIntPtr size, uint type, uint protect);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualFree(IntPtr address, UIntPtr size, uint type);

        public static SpeedResult Run(string folder, long totalBytes, CancelFlag cancel, Action<string> progress)
        {
            const int block = 4 * 1048576;
            int blocks = (int)(totalBytes / block);
            string file = Path.Combine(folder, "DiskCleanupHelper-speedtest-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp");
            IntPtr buf = VirtualAlloc(IntPtr.Zero, (UIntPtr)block, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (buf == IntPtr.Zero) throw new InvalidOperationException(L.T("Not enough memory for the speed test."));
            SpeedResult result = new SpeedResult();
            result.TestBytes = (long)blocks * block;
            try
            {
                // random data, so drives that compress data cannot cheat
                byte[] rnd = new byte[block];
                new Random().NextBytes(rnd);
                Marshal.Copy(rnd, 0, buf, block);
                rnd = null;

                progress(L.F("Writing a {0} MB test file...", result.TestBytes / 1048576));
                Stopwatch sw = Stopwatch.StartNew();
                using (SafeFileHandle h = CreateFile(file, GENERIC_WRITE, 0, IntPtr.Zero, CREATE_ALWAYS, FILE_FLAG_NO_BUFFERING | FILE_FLAG_WRITE_THROUGH, IntPtr.Zero))
                {
                    if (h.IsInvalid) throw new IOException(L.F("Could not create the test file in {0}.", folder));
                    for (int i = 0; i < blocks; i++)
                    {
                        if (cancel.IsSet) throw new OperationCanceledException();
                        uint n;
                        if (!WriteFile(h, buf, (uint)block, out n, IntPtr.Zero) || n != block) throw new IOException(L.T("Writing the test file failed. Is the drive full?"));
                    }
                }
                result.WriteMBs = result.TestBytes / 1048576.0 / Math.Max(0.001, sw.Elapsed.TotalSeconds);

                progress(L.T("Reading it back..."));
                sw = Stopwatch.StartNew();
                using (SafeFileHandle h = CreateFile(file, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_NO_BUFFERING | FILE_FLAG_SEQUENTIAL_SCAN, IntPtr.Zero))
                {
                    if (h.IsInvalid) throw new IOException(L.T("Could not open the test file."));
                    for (int i = 0; i < blocks; i++)
                    {
                        if (cancel.IsSet) throw new OperationCanceledException();
                        uint n;
                        if (!ReadFile(h, buf, (uint)block, out n, IntPtr.Zero) || n == 0) break;
                    }
                }
                result.ReadMBs = result.TestBytes / 1048576.0 / Math.Max(0.001, sw.Elapsed.TotalSeconds);

                progress(L.T("Reading small random blocks..."));
                Random r = new Random();
                long slots = result.TestBytes / 4096;
                int ops = 0;
                sw = Stopwatch.StartNew();
                using (SafeFileHandle h = CreateFile(file, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_NO_BUFFERING | FILE_FLAG_RANDOM_ACCESS, IntPtr.Zero))
                {
                    if (h.IsInvalid) throw new IOException(L.T("Could not open the test file."));
                    while (sw.Elapsed.TotalSeconds < 4)
                    {
                        if (cancel.IsSet) throw new OperationCanceledException();
                        long pos = (long)(r.NextDouble() * (slots - 1)) * 4096;
                        long np;
                        uint n;
                        if (!SetFilePointerEx(h, pos, out np, 0)) break;
                        if (!ReadFile(h, buf, 4096, out n, IntPtr.Zero)) break;
                        ops++;
                    }
                }
                double secs = Math.Max(0.001, sw.Elapsed.TotalSeconds);
                result.RandomIops = ops / secs;
                result.RandomMBs = ops * 4096.0 / 1048576.0 / secs;
                return result;
            }
            finally
            {
                VirtualFree(buf, UIntPtr.Zero, MEM_RELEASE);
                try { if (File.Exists(file)) File.Delete(file); } catch { }
            }
        }
    }
}
