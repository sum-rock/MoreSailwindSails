namespace MoreSailwindSails.Utils
{
    // Priority order for a diagnostic location; hidden spare clones never participate.
    internal enum WinchMountStatus
    {
        Hidden = -1,
        Unfitted = 0,
        Unused = 1,
        Occupied = 2,
    }
}
