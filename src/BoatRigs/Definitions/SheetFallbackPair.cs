namespace MoreSailwindSails.BoatRigs
{
    internal sealed class SheetFallbackPair
    {
        internal readonly SheetFallbackSeat Port,
            Starboard;

        internal SheetFallbackPair(SheetFallbackSeat port, SheetFallbackSeat starboard)
        {
            Port = port;
            Starboard = starboard;
        }

        internal string InvalidSide =>
            Port == null || !Port.Valid ? "port"
            : Starboard == null || !Starboard.Valid ? "starboard"
            : null;
    }
}
