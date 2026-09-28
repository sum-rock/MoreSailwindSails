namespace MoreSailwindSails.BoatRigs
{
    internal sealed class NativeSheetSource
    {
        internal readonly int Mast;

        // Null means corresponding indices, verified by the installed inventory.
        // Exceptions can explicitly author left/right indices without synthesizing seats.
        internal readonly int[][] Pairs;

        internal NativeSheetSource(int mast, int[][] pairs = null)
        {
            Mast = mast;
            Pairs = pairs;
        }
    }
}
