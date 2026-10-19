// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Size formatting. Values go through L.Ltr so "12.50 GB" keeps its order inside right-to-left text.
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.Common
{
    public static class Fmt
    {
        const double MB = 1048576.0, GB = 1073741824.0, TB = 1099511627776.0;

        /// <summary>"12.50 GB", "340 MB", "&lt; 1 MB", "0", or "-" when unknown.</summary>
        public static string Size(double? bytes)
        {
            if (!bytes.HasValue) return "-";
            double v = bytes.Value;
            if (v >= GB) return L.Ltr((v / GB).ToString("N2") + " GB");
            if (v >= MB) return L.Ltr((v / MB).ToString("N0") + " MB");
            if (v > 0) return L.Ltr("< 1 MB");
            return "0";
        }

        /// <summary>Like Size, but switches to TB for very large amounts (data written over a disk's life).</summary>
        public static string LargeSize(double bytes)
        {
            if (bytes >= TB) return L.Ltr((bytes / TB).ToString("N1") + " TB");
            return Size(bytes);
        }
    }
}
