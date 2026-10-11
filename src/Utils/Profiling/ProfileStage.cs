namespace MoreSailwindSails.Utils.Profiling
{
    // Optional subdivisions of a measured operation; Total includes child stages.
    internal enum ProfileStage
    {
        Total,
        Setup,
        Surface,
        Fit,
        Upload,
        Submit,
        Count,
    }
}
