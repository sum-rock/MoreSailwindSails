using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace MoreSailwindSails.BoatRigs
{
    internal sealed class MastSupportDefinition
    {
        // Native rig ID supplying sheet winch assets; it need not be installed.
        internal readonly int SheetControlSource;

        // Sections are ordered from upper to lower. The last aft section
        // identifies the physical support when optional upper sections are absent.
        internal readonly int[] ForeSections,
            AftSections;

        internal MastSupportDefinition(
            int sheetControlSource,
            int[] foreSections,
            int[] aftSections
        )
        {
            if (
                sheetControlSource < 0
                || foreSections.Length == 0
                || aftSections.Length == 0
                || foreSections.Concat(aftSections).Any(id => id < 0)
                || foreSections.Intersect(aftSections).Any()
            )
                throw new ArgumentException("Invalid mast support or control source.");
            SheetControlSource = sheetControlSource;
            ForeSections = foreSections;
            AftSections = aftSections;
        }
    }

    internal sealed class FishermansStayVariantDefinition
    {
        internal readonly int MountIndex,
            Donor,
            Fore,
            Aft,
            GuideIndex;
        internal readonly string Label;
        internal readonly Vector3 ForePoint,
            AftPoint;
        internal readonly bool ExtendedGuide;
        internal readonly int[] Required,
            Forbidden;

        internal FishermansStayVariantDefinition(
            int mountIndex,
            int donor,
            string label,
            int fore,
            Vector3 forePoint,
            int aft,
            Vector3 aftPoint,
            bool extendedGuide,
            int guideIndex,
            int[] required,
            int[] forbidden
        )
        {
            if (
                mountIndex < 128
                || mountIndex >= 256
                || donor < 0
                || donor >= 128
                || fore == aft
                || guideIndex < 0
                || string.IsNullOrEmpty(label)
                || !Finite(forePoint)
                || !Finite(aftPoint)
                || !required.Contains(fore)
                || !required.Contains(aft)
                || required.Concat(forbidden).Any(i => i < 0 || i >= 128)
                || required.Distinct().Count() != required.Length
                || forbidden.Distinct().Count() != forbidden.Length
                || required.Intersect(forbidden).Any()
            )
                throw new ArgumentException("Invalid Fisherman's Stay reference or mount ID.");
            MountIndex = mountIndex;
            Donor = donor;
            Label = label;
            Fore = fore;
            Aft = aft;
            ForePoint = forePoint;
            AftPoint = aftPoint;
            ExtendedGuide = extendedGuide;
            GuideIndex = guideIndex;
            Required = required;
            Forbidden = forbidden;
        }

        private static bool Finite(Vector3 point) =>
            !float.IsNaN(point.x)
            && !float.IsInfinity(point.x)
            && !float.IsNaN(point.y)
            && !float.IsInfinity(point.y)
            && !float.IsNaN(point.z)
            && !float.IsInfinity(point.z);
    }

    internal sealed class FishermansStayGroupDefinition
    {
        internal readonly string Label;
        internal readonly FishermansStayVariantDefinition[] Variants;

        internal FishermansStayGroupDefinition(
            string label,
            FishermansStayVariantDefinition[] variants
        )
        {
            if (string.IsNullOrEmpty(label) || variants.Length == 0)
                throw new ArgumentException("Empty Fisherman's Stay group.");
            Label = label;
            Variants = variants;
        }
    }

    internal enum WinchRole
    {
        Reef,
        Left,
        Right,
        Mid,
    }

    internal sealed class BoatRigDefinition
    {
        internal readonly string BoatName;
        internal readonly MastSupportDefinition[] Supports;
        internal readonly FishermansStayGroupDefinition[] Stays;
        internal readonly IReadOnlyDictionary<int, int> MastParents;
        internal readonly IReadOnlyDictionary<string, float> WinchClearances;
        internal readonly SheetWinchCategory[] SheetCategories;

        internal BoatRigDefinition(
            string boatName,
            MastSupportDefinition[] supports,
            FishermansStayGroupDefinition[] stays,
            IReadOnlyDictionary<int, int> mastParents,
            SheetWinchCategory[] sheetCategories = null,
            IReadOnlyDictionary<string, float> winchClearances = null
        )
        {
            if (
                supports.Length == 0
                || supports.Select(s => s.SheetControlSource).Distinct().Count() != supports.Length
            )
                throw new ArgumentException("Empty mast supports or duplicate control source.");
            var mounts = stays.SelectMany(g => g.Variants).Select(v => v.MountIndex).ToArray();
            if (mounts.Distinct().Count() != mounts.Length)
                throw new ArgumentException("Duplicate Fisherman's Stay mount ID.");
            if (
                mastParents.Any(p =>
                    p.Key < 0 || p.Value < -1 || (p.Value >= 0 && !mastParents.ContainsKey(p.Value))
                )
            )
                throw new ArgumentException("Invalid authored mast ancestry.");
            BoatName = boatName;
            Supports = supports;
            Stays = stays;
            MastParents = new ReadOnlyDictionary<int, int>(
                mastParents.ToDictionary(p => p.Key, p => p.Value)
            );
            WinchClearances = winchClearances ?? new Dictionary<string, float>();
            SheetCategories = sheetCategories ?? Array.Empty<SheetWinchCategory>();
            if (
                SheetCategories
                    .SelectMany(c => c.PhysicalMasts)
                    .GroupBy(id => id)
                    .Any(g => g.Count() != 1)
                || SheetCategories.GroupBy(c => c.Name).Any(g => g.Count() != 1)
            )
                throw new ArgumentException("Duplicate physical winch category.");
            foreach (int section in MastParents.Keys)
                Sections(section); // Reject cycles before any runtime lookup can hang.
        }

        internal IEnumerable<IGrouping<int, MastSupportDefinition>> MastPairs(int foreIndex) =>
            Supports
                .Where(s => s.ForeSections.Contains(foreIndex))
                .GroupBy(s => s.AftSections.Last());

        internal int Base(int section) => Sections(section).Last();

        internal int[] Sections(int section)
        {
            var sections = new List<int>();
            var visited = new HashSet<int>();
            while (section >= 0)
            {
                if (!visited.Add(section))
                    throw new ArgumentException("Cyclic authored mast ancestry.");
                sections.Add(section);
                if (!MastParents.TryGetValue(section, out section))
                    throw new ArgumentException("No authored staysail mast reference.");
            }
            return sections.ToArray();
        }

        internal SheetWinchCategory SheetCategory(int mast)
        {
            int physical = MastParents.ContainsKey(mast) ? Base(mast) : mast;
            return SheetCategories.FirstOrDefault(c => c.PhysicalMasts.Contains(physical));
        }
    }

    internal static class BoatRigCatalog
    {
        internal static BoatRigDefinition[] All =>
            new[]
            {
                Brig.Definition,
                Junk.Definition,
                Jong.Definition,
                Sanbuq.Definition,
                Cog.Definition,
                Leopard.Definition,
                Shroud.Definition,
                LargeDhow.Definition,
            };

        internal static BoatRigDefinition Find(string boatName)
        {
            if (boatName == null)
                return null;
            while (boatName.EndsWith("(Clone)", StringComparison.Ordinal))
                boatName = boatName.Substring(0, boatName.Length - 7).TrimEnd();
            return All.FirstOrDefault(d => d.BoatName == boatName);
        }
    }
}
