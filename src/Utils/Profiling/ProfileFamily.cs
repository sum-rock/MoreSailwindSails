namespace MoreSailwindSails.Utils.Profiling
{
    // Attributes measurements without dependencies on sail implementation types.
    internal enum ProfileFamily
    {
        Unspecified,
        LooseFootedSpritsail,
        BoomedSpritsail,
        FlyingSail,
        Staysail,
        Count,
    }
}
