using System;
using System.Linq;
using MoreSailwindSails.BoatRigs;
using MoreSailwindSails.Controls;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Controls;

// Exercises native-seat exhaustion and the measured mainmast and mizzen fallback seats.
internal static class GlorianaHalyardChecks
{
    internal static void Run()
    {
        CheckGroup(
            mast: 2,
            sources: new[] { 2 },
            contact: new Vector3(-0.005373f, 1.887422f, 0.791344f),
            normal: new Vector3(-0.098015f, 0.003737f, 0.995178f),
            sourceNormal: new Vector3(0f, 0.98480785f, -0.17364828f),
            templateIndex: 1
        );
        CheckGroup(
            mast: 3,
            sources: new[] { 3, 4, 6 },
            contact: new Vector3(0.801387f, 4.769459f, -9.351812f),
            normal: Vector3.right,
            sourceNormal: new Vector3(-0.17364817f, 0.96984630f, 0.17101012f),
            templateIndex: 0
        );
    }

    private static void CheckGroup(
        int mast,
        int[] sources,
        Vector3 contact,
        Vector3 normal,
        Vector3 sourceNormal,
        int templateIndex
    )
    {
        var group = Gloriana.Definition.HalyardGroups.Single(g => g.Mast == mast);
        var definition = group.Fallback;
        Check(
            group.Sources.SequenceEqual(sources)
                && definition.Valid
                && definition.TemplateIndex == templateIndex,
            "Gloriana fallback changed native sources or template."
        );
        Check(
            (definition.Contact - contact).magnitude < 0.000001f
                && (definition.Normal - normal).magnitude < 0.000001f
                && (definition.SourceNormal - sourceNormal).magnitude < 0.000001f,
            "Gloriana fallback left its captured contact or measured normal."
        );
        // The installed coil's rear bound, at native 0.7 scale, meets the captured surface.
        Check(
            Math.Abs(definition.Offset - 0.13091201f * 0.7f) < 0.000001f
                && Math.Abs(
                    Vector3.Dot(
                        definition.Position - definition.Contact,
                        definition.Normal.normalized
                    ) - definition.Offset
                ) < 0.000001f,
            "Coil mounting depth changed."
        );
        Check(
            BoatRigCatalog.All.Sum(b => b.HalyardGroups.Count(g => g.Fallback != null)) == 2,
            "Manual halyard fallback leaked to another mast or boat."
        );
        foreach (
            var invalid in new[]
            {
                new HalyardFallbackSeat(
                    contact: definition.Contact,
                    normal: Vector3.zero,
                    sourceNormal: definition.SourceNormal,
                    offset: definition.Offset,
                    templateIndex: 1
                ),
                new HalyardFallbackSeat(
                    contact: definition.Contact,
                    normal: definition.Normal,
                    sourceNormal: definition.SourceNormal,
                    offset: float.NaN,
                    templateIndex: 1
                ),
                new HalyardFallbackSeat(
                    contact: definition.Contact,
                    normal: definition.Normal,
                    sourceNormal: definition.SourceNormal,
                    offset: definition.Offset,
                    templateIndex: -1
                ),
            }
        )
            Check(!invalid.Valid, "Invalid halyard fallback accepted.");

        WinchCandidate Seat(string id, bool fallback, Vector3 position) =>
            new WinchCandidate
            {
                Id = id,
                Fallback = fallback,
                Supported = true,
                Vacant = true,
                Seats = new[]
                {
                    new WinchSeat(
                        identity: id,
                        aliases: null,
                        position: position,
                        rotation: Quaternion.identity
                    ),
                },
            };
        var native = Enumerable
            .Range(0, 3)
            .Select(i => Seat(id: "native/" + i, fallback: false, position: new Vector3(i, 2, 0)))
            .ToArray();
        var manual = Seat(
            id: "halyard-fallback/" + mast,
            fallback: true,
            position: definition.Position
        );
        var ledger = new WinchReservations();
        var owner = new object();
        WinchResolution Resolve(WinchCandidate current = null) =>
            WinchPlacementPolicy.Resolve(
                ledger: ledger,
                owner: owner,
                current: current,
                native: native,
                fallback: manual
            );
        Check(Resolve().Candidate == native[0], "Fallback bypassed free native seats.");
        native[0].Vacant = false;
        Check(
            Resolve(current: native[0]).Candidate == native[1],
            "Fallback bypassed next native seat."
        );
        foreach (var seat in native)
            seat.Vacant = false;
        Check(
            Resolve(current: native[1]).Candidate == manual && ledger.Count == 1,
            "Three occupied native seats did not yield to one fallback."
        );
        Check(
            WinchPlacementPolicy
                .Resolve(
                    ledger: ledger,
                    owner: new object(),
                    current: null,
                    native: native,
                    fallback: manual
                )
                .FallbackBlocked == 1,
            "Two sails claimed one fallback."
        );
        native[0].Vacant = true;
        Check(
            Resolve(current: manual).Candidate == manual,
            "Valid fallback moved when a native seat freed."
        );
        foreach (var seat in native)
            seat.Supported = false;
        manual.Supported = false;
        Check(
            Resolve(current: manual).Candidate == null && ledger.Count == 0,
            "Missing requested mast retained its halyard reservation."
        );
        manual.Supported = true;
        Check(Resolve().Candidate == manual, "Fallback did not recover after support returned.");
        ledger.Release(owner: owner);
        foreach (var seat in native)
            seat.Supported = true;
        Check(
            Resolve().Candidate == native[0],
            "New allocation did not prefer a free native seat."
        );
        Console.WriteLine(
            $"PASS: Gloriana mast {mast} reef fallback, measured coil depth, native priority, exclusive reservation, stable retention and support-loss recovery."
        );
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception(message);
    }
}
