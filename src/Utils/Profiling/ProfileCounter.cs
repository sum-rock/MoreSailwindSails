namespace MoreSailwindSails.Utils.Profiling
{
    // Named diagnostic events, accumulated separately for each family.
    internal enum ProfileCounter
    {
        MountUploads,
        SurfaceHits,
        SurfaceMisses,
        Initial,
        Sheet,
        Reef,
        Tack,
        Fitting,
        Support,
        Reactivate,
        MountRebuild,
        MountReuse,
        SpritRebuild,
        SpritReuse,
        SnotterRebuild,
        SnotterReuse,
        Count,
    }
}
