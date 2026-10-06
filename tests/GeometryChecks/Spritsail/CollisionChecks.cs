using System;
using MoreSailwindSails.Sails.Spritsail.BoomedSpritsail;
using MoreSailwindSails.Sails.Spritsail.LooseFootedSpritsail;
using UnityEngine;
using MkA = MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.MkA.BoomedSpritsailMkA;
using MkB = MoreSailwindSails.Sails.Spritsail.BoomedSpritsail.MkB.BoomedSpritsailMkB;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Checks the native-style thin deployed panels across both companion cuts and shipyard scales.
internal static class CollisionChecks
{
    internal static void Run()
    {
        foreach (var definition in new[] { MkA.Definition, MkB.Definition })
        // Mk.B's tapered leech loses its last shallow strips at small sizes;
        // Mk.A retains all 24. Pin that coverage independently of CollisionStrip's result.
        foreach (
            var (baseWidth, taperedColumns) in new[]
            {
                (0.25f, 21),
                (1f, 23),
                (4.6f, 23),
                (12f, 24),
            }
        )
        foreach (bool boomed in new[] { false, true })
        foreach (
            var scale in new[] { Vector3.one, Vector3.one * 0.75f, new Vector3(0.6f, 1.3f, 1.5f) }
        )
        {
            var corners = definition.Corners(width: baseWidth);
            float width = corners[3].z - corners[2].z;
            for (int column = 0; column < 24; column++)
            {
                Vector3 center,
                    size;
                bool active = boomed
                    ? BoomedSpritsailMastInstallationGeometry.CollisionStrip(
                        width: width,
                        column: column,
                        center: out center,
                        size: out size,
                        corners: corners
                    )
                    : LooseFootedSpritsailMastInstallationGeometry.CollisionStrip(
                        width: width,
                        column: column,
                        center: out center,
                        size: out size,
                        corners: corners
                    );
                int expectedColumns = definition == MkA.Definition ? 24 : taperedColumns;
                Require(
                    value: active == (column < expectedColumns),
                    message: $"{definition.DisplayName}, boomed={boomed}, width={baseWidth}, column={column}: deployed coverage changed, including the mast-adjacent strip."
                );
                if (!active)
                    continue;
                var scaledCenter = Vector3.Scale(center, scale);
                var halfSize = Vector3.Scale(size, scale) * 0.5f;
                Require(
                    value: Math.Abs(scaledCenter.y) < 1e-6f
                        && Math.Abs(halfSize.y - 0.05f * scale.y) < 1e-6f,
                    message: "Panel thickness must follow the native 10 cm box, independent of sail width or flex."
                );
                Require(
                    value: scaledCenter.z - halfSize.z >= corners[2].z * scale.z - 1e-5f
                        && scaledCenter.z + halfSize.z <= corners[3].z * scale.z + 1e-5f,
                    message: "Deployed collision must not reserve extra reach beyond the tack or clew."
                );
                foreach (float z in new[] { center.z - size.z / 2, center.z + size.z / 2 })
                {
                    float u = (z - corners[2].z) / width;
                    float peakU = (corners[1].z - corners[2].z) / width;
                    float foot = corners[2].x + (corners[3].x - corners[2].x) * u;
                    float head =
                        u <= peakU
                            ? corners[1].x * u / peakU
                            : corners[1].x
                                + (corners[3].x - corners[1].x) * (u - peakU) / (1 - peakU);
                    Require(
                        value: scaledCenter.x - halfSize.x >= foot * scale.x - 1e-5f
                            && scaledCenter.x + halfSize.x <= head * scale.x + 1e-5f,
                        message: "Thin collision strips must remain within the deployed cut after scaling."
                    );
                }
            }
        }
        Console.WriteLine(
            "PASS: both Spritsail variants and marks retain thin deployed collision panels across sizes/scales without flex padding."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }
}
