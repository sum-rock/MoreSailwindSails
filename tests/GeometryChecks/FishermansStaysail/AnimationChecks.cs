// Verifies the authored control surface at samples, between samples and across tacks.
using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using MoreSailwindSails.Sails.FishermansStaysail.MkA;
using MoreSailwindSails.Sails.FishermansStaysail.MkB;
using MoreSailwindSails.Sails.FishermansStaysail.MkC;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class AnimationChecks
{
    internal static void Run()
    {
        Check(FishermansStaysailMkABakedMesh.CreateAnimation());
        CheckFitting(
            FishermansStaysailMkABakedMesh.CreateAnimation(),
            FishermansStaysailMkABakedMesh.Create
        );
        Check(FishermansStaysailMkBBakedMesh.CreateAnimation());
        CheckFitting(
            FishermansStaysailMkBBakedMesh.CreateAnimation(),
            FishermansStaysailMkBBakedMesh.Create
        );
        Check(FishermansStaysailMkCBakedMesh.CreateAnimation());
        CheckFitting(
            FishermansStaysailMkCBakedMesh.CreateAnimation(),
            FishermansStaysailMkCBakedMesh.Create
        );
    }

    private static void CheckFitting(
        FishermansStaysailAnimationData data,
        Func<FishermansStaysailMeshData> create
    )
    {
        // An undeformed authored surface must retarget exactly like the fitting mesh,
        // including Mk.A's falling foot and Mk.C's longer luff and rising foot.
        var flat = create().Vertices;
        data.Sheet = new[] { flat, flat, flat };
        data.ReefIn = data.ReefOut = new[] { flat, flat };
        data.ReefFractions = new[] { 0f, 1f };
        foreach (float slope in new[] { 0f, 20f, 75f })
        {
            var fitted = FishermansStaysailMeshFitting.Fit(
                data: create(),
                width: 13.8f,
                headSlope: slope
            );
            var animation = new FishermansStaysailAnimation(
                data: data,
                width: 13.8f,
                headSlope: slope
            );
            animation.Pose(deployment: 1, ease: 0, side: 1);
            for (int i = 0; i < flat.Length; i++)
                if ((animation.Vertices[i] - fitted.Vertices[i]).magnitude > 0.0001f)
                    throw new Exception(
                        "Authored slope retargeting disagrees with the fitted cut."
                    );
        }
    }

    private static void Check(FishermansStaysailAnimationData data)
    {
        var animation = new FishermansStaysailAnimation(data: data, width: 1, headSlope: 20);
        for (int sample = 0; sample < data.ReefFractions.Length; sample++)
        for (int side = -1; side <= 1; side += 2)
        for (int ease = 0; ease <= 1; ease++)
        {
            animation.Pose(deployment: 1 - data.ReefFractions[sample], ease: ease, side: side);
            var expected = (ease == 0 ? data.ReefIn : data.ReefOut)[sample];
            for (int i = 0; i < expected.Length; i++)
            {
                var point = expected[i];
                point.y *= side;
                if ((point - animation.Vertices[i]).magnitude > 1e-5f)
                    throw new Exception("Authored reef sample changed.");
            }
        }
        for (int sheet = 0; sheet < 3; sheet++)
        {
            animation.Pose(deployment: 1, ease: sheet * 0.5f, side: 1);
            for (int i = 0; i < animation.Vertices.Length; i++)
                if ((animation.Vertices[i] - data.Sheet[sheet][i]).magnitude > 1e-5f)
                    throw new Exception("Authored sheet sample changed.");
        }
        // Between reef keys, the endpoint surfaces blend linearly; above 25%
        // reef there is no deployed midpoint correction.
        const int interval = 3;
        float reefMiddle = (data.ReefFractions[interval] + data.ReefFractions[interval + 1]) * 0.5f;
        animation.Pose(deployment: 1 - reefMiddle, ease: 0.3f, side: 1);
        for (int i = 0; i < animation.Vertices.Length; i++)
        {
            var inside = (data.ReefIn[interval][i] + data.ReefIn[interval + 1][i]) * 0.5f;
            var outside = (data.ReefOut[interval][i] + data.ReefOut[interval + 1][i]) * 0.5f;
            if ((animation.Vertices[i] - (inside * 0.7f + outside * 0.3f)).magnitude > 1e-5f)
                throw new Exception("Combined sheet/reef interpolation changed.");
        }
        if (
            data.CornerIndices.Length != 4
            || data.CornerIndices[3] != 824
            || data.ReefFractions[5] != 200f / 220f
            || data.ReefFractions[6] != 1
        )
            throw new Exception("Authored attachments or reef timing changed.");
        foreach (float width in new[] { 0.25f, 13.8f, 40f })
        foreach (float slope in new[] { 0f, 20f, 75f })
        {
            animation = new FishermansStaysailAnimation(data: data, width: width, headSlope: slope);
            for (int reef = 0; reef <= 20; reef++)
            for (int sheet = 0; sheet <= 10; sheet++)
            {
                animation.Pose(deployment: 1 - reef / 20f, ease: sheet / 10f, side: 0.4f);
                foreach (var point in animation.Vertices)
                    if (!float.IsFinite(point.sqrMagnitude))
                        throw new Exception("Non-finite fitted animation.");
            }
        }
        if (
            FishermansStaysailAnimation.SheetEase(angle: -10, minimum: -20, maximum: 40) != 0.5f
            || FishermansStaysailAnimation.SheetEase(angle: 10, minimum: -20, maximum: 40) != 0.25f
        )
            throw new Exception("Asymmetric sheet travel mapping changed.");
        Console.WriteLine(
            "PASS: Staysail authored sheet/reef samples, tack mirroring and fitted animation."
        );
    }
}
