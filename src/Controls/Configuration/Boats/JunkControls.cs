// Owns Junk's control profile; no fallback positions are authored.
namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class JunkControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Junk.Definition
        );
    }
}
