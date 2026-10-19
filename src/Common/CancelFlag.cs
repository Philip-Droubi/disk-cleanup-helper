// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Lets the user stop a background task.
using System;

namespace DiskCleanupHelper.Common
{
    public class CancelFlag { public volatile bool IsSet; }
}
