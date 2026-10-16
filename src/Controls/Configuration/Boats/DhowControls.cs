// Owns Dhow's control profile; no fallback positions are authored.
namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class DhowControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Dhow.Definition
        );
    }
}
