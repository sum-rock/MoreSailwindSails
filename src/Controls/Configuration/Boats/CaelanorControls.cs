// Owns Caelanor's control profile; no fallback positions are authored.
namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class CaelanorControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Caelanor.Definition
        );
    }
}
