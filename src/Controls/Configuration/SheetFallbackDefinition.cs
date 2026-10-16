// Names an authored sheet fallback pair independently of native source rigs.
namespace MoreSailwindSails.Controls.Configuration
{
    internal sealed class SheetFallbackDefinition
    {
        internal readonly string Name;
        internal readonly SheetFallbackPair Fallback;
        internal string FallbackName => Name + "Fallback";

        internal SheetFallbackDefinition(string name, SheetFallbackPair fallback)
        {
            Name = name;
            Fallback = fallback;
        }
    }
}
