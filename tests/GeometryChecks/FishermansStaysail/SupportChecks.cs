// Exercises connected luff support, missing ancestors and fitting across mast sections.
using System;
using System.Collections.Generic;
using System.Linq;
using MoreSailwindSails.BoatConfiguration;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Stays.FishermansStay.Configuration;
using MoreSailwindSails.Tests.GeometryChecks.FishermansStay;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class SupportChecks
{
    internal static void Run()
    {
        var boat = new BoatDefinition(
            boatName: "support test",
            mastParents: new Dictionary<int, int>
            {
                { 0, -1 },
                { 1, 0 },
                { 2, 1 },
                { 3, -1 },
            }
        );
        var bottoms = new Dictionary<int, Vector3?>
        {
            [0] = Vector3.zero,
            [1] = new Vector3(0, 6, 0),
            [3] = new Vector3(0, -100, 0),
        };
        Vector3 Bottom(int section = 2, BoatDefinition definition = null) =>
            FishermansStaysailInstallationGeometry.SupportBottom(
                boat: definition ?? boat,
                section: section,
                sectionBottom: new Vector3(0, 10, 0),
                axis: Vector3.up,
                parentBottom: id => bottoms.GetValueOrDefault(id)
            );
        Near(Bottom().y, 0, "Multiple ancestors must reach the base, ignoring unrelated masts.");
        Near(Bottom(section: 99).y, 10, "Unknown sections must retain their own bottom.");
        Near(Bottom(section: 0).y, 10, "Base sections must not extend to descendants.");
        bottoms[0] = null;
        Near(Bottom().y, 6, "A missing base must retain the last supported section.");
        bottoms[0] = Vector3.zero;
        bottoms[1] = null;
        Near(Bottom().y, 10, "An inactive or unusable parent must not be skipped.");
        bottoms[1] = new Vector3(float.NaN, 0, 0);
        Near(Bottom().y, 10, "Invalid geometry must stop support traversal.");
        bottoms[1] = new Vector3(0, 12, 0);
        bottoms[0] = null;
        Near(Bottom().y, 10, "Overlapping sections must never shorten available support.");

        var measurements = StaySparMeasurement.Load();
        int extended = 0;
        foreach (var definition in FishermansStayCatalog.All)
        foreach (var stay in definition.Stays.SelectMany(group => group.Variants))
        {
            var masts = measurements[definition.BoatName];
            var fore = masts[stay.Fore];
            var axis = (fore.Top - fore.Bottom).normalized;
            var bottom = FishermansStaysailInstallationGeometry.SupportBottom(
                boat: definition.Boat,
                section: stay.Fore,
                sectionBottom: fore.Bottom,
                axis: axis,
                parentBottom: id => masts[id].Bottom
            );
            var head =
                fore.Point(stay.ForePoint)
                - axis * FishermansStaysailInstallationGeometry.HeadClearance;
            float oldRoom = Vector3.Dot(head - fore.Bottom, axis);
            float room = Vector3.Dot(head - bottom, axis);
            var offset = new Vector3(30, -20, 10);
            Vector3 Rotate(Vector3 p) => new Vector3(p.y, -p.z, -p.x);
            var posed = FishermansStaysailInstallationGeometry.SupportBottom(
                boat: definition.Boat,
                section: stay.Fore,
                sectionBottom: Rotate(fore.Bottom) + offset,
                axis: Rotate(axis),
                parentBottom: id => Rotate(masts[id].Bottom) + offset
            );
            Near(
                Vector3.Dot(Rotate(head) + offset - posed, Rotate(axis)),
                room,
                "Support length must survive boat translation, rotation and mast rake."
            );
            if (room <= oldRoom + 0.1f)
                continue;
            extended++;
            foreach (
                var cut in new Func<float, float, FishermansStaysailMeshData>[]
                {
                    BehaviorCases.CreateA,
                    BehaviorCases.CreateB,
                    BehaviorCases.CreateC,
                }
            )
            {
                // Height and width can be independently scaled by the shipyard.
                var mesh = cut(1, 20);
                float luff = oldRoom + (room - oldRoom) * 0.5f;
                float heightScale = luff / -mesh.Corners[2].x;
                float scaledLuff = -mesh.Corners[2].x * heightScale;
                if (
                    FishermansStaysailInstallationGeometry.FitError(
                        width: 1,
                        luff: scaledLuff,
                        drop: 0,
                        room: room,
                        span: 5
                    ) != null
                    || FishermansStaysailInstallationGeometry.FitError(
                        width: 1,
                        luff: scaledLuff,
                        drop: 0,
                        room: oldRoom,
                        span: 5
                    ) == null
                    || FishermansStaysailInstallationGeometry.FitError(
                        width: 1,
                        luff: room + 0.1f,
                        drop: 0,
                        room: room,
                        span: 5
                    ) == null
                )
                    throw new Exception(
                        "Cross-section luffs must fit only within connected support."
                    );
            }
        }
        if (extended == 0)
            throw new Exception("No authored topmast supports were exercised.");
        Console.WriteLine(
            $"PASS: connected staysail support across {extended} authored topmast stays, all cuts and missing-parent limits."
        );
    }

    private static void Near(float actual, float expected, string message)
    {
        if (float.IsNaN(actual) || Math.Abs(actual - expected) > 0.0001f)
            throw new Exception(message);
    }
}
