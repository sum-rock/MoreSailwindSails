using System;
using System.Collections.Generic;

namespace MoreSailwindSails.Utils.Profiling
{
    // Parses user-facing selections once, outside measured paths.
    internal static class ProfileSelection
    {
        internal const int All = (1 << (int)ProfileTarget.Count) - 1;
        internal const string DefaultTargets =
            "Rig,Frame,Shape,SheetFlex,Aerodynamics,Ropes,SailMount,Sprit,Snotter,VisualCache";

        internal static int Parse(string text, Action<string> warn)
        {
            int mask = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in (text ?? "").Split(','))
            {
                var name = item.Trim();
                if (name.Length == 0 || !seen.Add(name))
                    continue;
                if (name.Equals("All", StringComparison.OrdinalIgnoreCase))
                    mask |= All;
                else if (
                    TryName(name: name, value: out ProfileTarget target)
                    && target != ProfileTarget.Count
                )
                    mask |= 1 << (int)target;
                else
                    warn("Unknown ProfileTargets entry: " + name);
            }
            return mask;
        }

        internal static ProfileBypass ParseBypass(string text, Action<string> warn)
        {
            if (TryName(name: (text ?? "").Trim(), value: out ProfileBypass bypass))
                return bypass;
            warn("Unknown ProfileBypass; using None: " + text);
            return ProfileBypass.None;
        }

        private static bool TryName<T>(string name, out T value)
            where T : struct
        {
            foreach (var candidate in Enum.GetNames(typeof(T)))
                if (candidate.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return Enum.TryParse(value: candidate, result: out value);
            value = default;
            return false;
        }
    }
}
