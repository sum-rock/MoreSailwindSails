using BepInEx.Configuration;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Owns spritsail category identity and live development tuning independently of donors.
    internal static class SpritsailCategory
    {
        internal const SailCategory Value = (SailCategory)SpritsailRules.CategoryId;
        internal const string Name = "Spritsails";
        private static ConfigEntry<float> forceMultiplier;
        private static ConfigEntry<float> obstructedMultiplier;

        internal static void Configure(ConfigFile config)
        {
            obstructedMultiplier = config.Bind(
                section: "Spritsails",
                key: "ObstructedTackForceMultiplier",
                defaultValue: SpritsailObstructionGeometry.DefaultForceMultiplier,
                configDescription: new ConfigDescription(
                    description: "Propulsion multiplier on the sprit-obstructed tack. 0.90 gives a 10% reduction; 1 disables the force penalty. Visual shaping remains enabled.",
                    acceptableValues: new AcceptableValueRange<float>(minValue: 0, maxValue: 1)
                )
            );
            forceMultiplier = config.Bind(
                section: "Spritsails",
                key: "AppliedForceMultiplier",
                defaultValue: SpritsailRules.DefaultForceMultiplier,
                description: "Development tuning for spritsail propulsion only. Junk uses 0.75; gaff uses 0.85. Use a finite, nonnegative value. Invalid values use 0.75."
            );
        }

        internal static bool IsSpritsail(Sail sail) =>
            sail
            && sail.category == Value
            && SpritsailCatalog.IsRegistered(prefabIndex: sail.prefabIndex);

        internal static float ScalePropulsion(float power, Sail sail) =>
            IsSpritsail(sail: sail)
                ? power
                    * SpritsailRules.ValidForceMultiplier(
                        value: forceMultiplier?.Value ?? SpritsailRules.DefaultForceMultiplier
                    )
                    * ObstructionMultiplier(sail: sail)
                : power;

        private static float ObstructionMultiplier(Sail sail)
        {
            var obstruction = sail.GetComponent<SpritsailObstruction>();
            return obstruction && obstruction.isActiveAndEnabled
                ? SpritsailObstructionGeometry.Force(
                    obstruction: obstruction.Amount,
                    multiplier: obstructedMultiplier?.Value
                        ?? SpritsailObstructionGeometry.DefaultForceMultiplier
                )
                : 1;
        }

        internal static SailCategory OverlapCategory(Sail sail) =>
            IsSpritsail(sail: sail)
                ? (SailCategory)SpritsailRules.OverlapCategory(category: (int)sail.category)
                : sail.category;

        internal static void Initialize(Sail sail, float upwindEfficiency)
        {
            sail.category = Value;
            sail.squareSail = false;
            sail.junkType = false;
            sail.upwindEfficiency = upwindEfficiency;
        }
    }
}
