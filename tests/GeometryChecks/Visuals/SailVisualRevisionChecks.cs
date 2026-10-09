using System;
using MoreSailwindSails.Visuals;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.Visuals;

// Exercises deliberate state transitions and independent successful/hidden/failed cache consumers.
internal static class SailVisualRevisionChecks
{
    internal static void Run()
    {
        var revision = new SailVisualRevision();
        var mount = new SailVisualCache();
        var sprit = new SailVisualCache();
        var snotter = new SailVisualCache();
        var shape = new[] { Vector3.right, Vector3.up, Vector3.forward, Vector3.one };
        var mast = new object();
        var mesh = new object();
        var left = new object();
        var right = new object();
        var sheets = new Vector3(2, 0, 3);
        float unroll = 1,
            height = 4;
        int tack = 1;
        var scale = Vector3.one;
        var mastScale = Vector3.one;
        bool active = true;
        SailVisualState State() =>
            new SailVisualState(
                sheets: sheets,
                unroll: unroll,
                tack: tack,
                scale: scale,
                mastScale: mastScale,
                height: height,
                mast: mast,
                mesh: mesh,
                left: left,
                mid: null,
                right: right,
                active: active
            );
        long Update() => revision.Update(state: State(), shape: shape);
        void CommitAll(long value)
        {
            mount.Commit(revision: value);
            sprit.Commit(revision: value);
            snotter.Commit(revision: value);
        }
        void Changed(int reason)
        {
            long previous = revision.Revision;
            long current = Update();
            Require(
                value: current == previous + 1 && revision.Reasons == reason,
                message: "Only the corresponding deliberate change must advance the shared revision."
            );
            Require(
                value: mount.Needs(revision: current)
                    && sprit.Needs(revision: current)
                    && snotter.Needs(revision: current),
                message: "All consumers must independently observe the new revision."
            );
            CommitAll(value: current);
        }
        long initial = Update();
        Require(
            value: initial > 0 && (revision.Reasons & SailVisualRevision.Initial) != 0,
            message: "Initial creation must produce a fitting revision."
        );
        CommitAll(value: initial);
        // Repeated draws during environmental motion supply exactly the same logical state.
        for (int i = 0; i < 1000; i++)
            Require(
                value: Update() == initial
                    && revision.Reasons == 0
                    && !mount.Needs(revision: initial)
                    && !sprit.Needs(revision: initial)
                    && !snotter.Needs(revision: initial),
                message: "Unchanged logical state must not rebuild under repeated environmental updates."
            );
        for (int i = 0; i < 5; i++)
        {
            sheets.x += 0.00001f;
            Changed(reason: SailVisualRevision.Sheet);
            unroll -= 0.00001f;
            Changed(reason: SailVisualRevision.Reef);
        }
        for (int i = 0; i < 5; i++)
        {
            tack = SailVisualRevision.TackSide(angle: i % 2 == 0 ? -4.9f : 4.9f, previous: tack);
            long current = revision.Revision;
            Require(
                value: Update() == current,
                message: "Native same-side sway inside the hysteresis band must not invalidate."
            );
        }
        tack = SailVisualRevision.TackSide(angle: -6, previous: tack);
        Changed(reason: SailVisualRevision.Tack);
        tack = SailVisualRevision.TackSide(angle: 6, previous: tack);
        Changed(reason: SailVisualRevision.Tack);
        scale = new Vector3(2, 1, 1);
        Changed(reason: SailVisualRevision.Fitting);
        mastScale = new Vector3(1, 2, 1);
        Changed(reason: SailVisualRevision.Fitting);
        height += 0.01f;
        Changed(reason: SailVisualRevision.Fitting);
        shape[1] += Vector3.forward * 0.01f;
        Changed(reason: SailVisualRevision.Fitting);
        mast = new object();
        Changed(reason: SailVisualRevision.Support);
        mesh = new object();
        Changed(reason: SailVisualRevision.Support);
        left = new object();
        Changed(reason: SailVisualRevision.Sheet);
        active = false;
        long hidden = revision.Revision;
        Require(
            value: Update() == hidden,
            message: "Hiding alone must not require a geometry rebuild."
        );
        active = true;
        Changed(reason: SailVisualRevision.Reactivate);

        sheets.z += 1;
        long pending = Update();
        sprit.Commit(revision: pending);
        // Mount is bypassed, snotter fails: neither consumer acknowledges the new state.
        for (int i = 0; i < 3; i++)
        {
            Update();
            Require(
                value: mount.Needs(revision: pending)
                    && snotter.Needs(revision: pending)
                    && !sprit.Needs(revision: pending),
                message: "Hidden/bypassed/failed consumers must remain dirty independently of successful parts."
            );
        }
        mount.Commit(revision: pending);
        snotter.Commit(revision: pending);
        Require(
            value: !mount.Needs(revision: pending) && !snotter.Needs(revision: pending),
            message: "Successful catch-up must reuse the current revision afterward."
        );
        snotter.Invalidate();
        Require(
            value: snotter.Needs(revision: pending) && !mount.Needs(revision: pending),
            message: "A consumer's reactivation cannot invalidate unrelated parts."
        );
        revision.Reset();
        Require(
            value: Update() > pending && (revision.Reasons & SailVisualRevision.Initial) != 0,
            message: "Rig reactivation must force a new revision even with unchanged control values."
        );
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            Update();
        Require(
            value: GC.GetAllocatedBytesForCurrentThread() == before,
            message: "Warmed state sampling must not allocate managed objects."
        );
        Console.WriteLine(
            "PASS: deliberate visual revisions, exact continuous controls, tack hysteresis, fitting/support/reactivation, independent pending consumers and allocation-free reuse; environmental inputs are excluded."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
