// Owns Brig's control profile; no fallback positions are authored.
namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class BrigControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Brig.Definition
        );
    }
}
