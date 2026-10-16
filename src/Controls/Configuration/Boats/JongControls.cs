// Owns Jong's control profile; no fallback positions are authored.
namespace MoreSailwindSails.Controls.Configuration.Boats
{
    internal static class JongControls
    {
        internal static readonly BoatControlDefinition Definition = new BoatControlDefinition(
            boat: BoatConfiguration.Boats.Jong.Definition
        );
    }
}
