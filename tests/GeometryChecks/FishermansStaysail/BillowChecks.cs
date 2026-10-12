using System;
using MoreSailwindSails.Sails.FishermansStaysail;
using UnityEngine;

namespace MoreSailwindSails.Tests.GeometryChecks.FishermansStaysail;

internal static class BillowChecks
{
    internal static void Run()
    {
        BehaviorCases.ForEach(
            "weighted skin and triangle lengths mirror across tacks and reefs",
            (mark, width, slope) =>
            {
                var data = mark.Create(width, slope);
                for (int angle = -40; angle <= 40; angle += 5)
                    foreach (float unroll in BehaviorCases.Reefs)
                        BehaviorCases.Check(
                            $"sheet {angle}, unroll {unroll}",
                            () =>
                            {
                                var port = Pose(data, mark.HeadAngle, width, angle, 1, unroll);
                                var starboard = Pose(
                                    data,
                                    mark.HeadAngle,
                                    width,
                                    -angle,
                                    -1,
                                    unroll
                                );
                                for (int i = 0; i < port.Length; i++)
                                    BehaviorCases.Near(
                                        (Mirror(port[i]) - starboard[i]).magnitude,
                                        0,
                                        width * 2e-5f,
                                        "opposite-tack weighted skin"
                                    );
                                // Check actual skin edges, including triangle diagonals, rather
                                // than just corresponding bone positions or corner spans.
                                for (int i = 0; i < data.Triangles.Length; i += 3)
                                for (int edge = 0; edge < 3; edge++)
                                {
                                    int a = data.Triangles[i + edge],
                                        b = data.Triangles[i + (edge + 1) % 3];
                                    BehaviorCases.Near(
                                        (port[a] - port[b]).magnitude,
                                        (starboard[a] - starboard[b]).magnitude,
                                        width * 2e-5f,
                                        "tack-invariant cloth edge lengths"
                                    );
                                }
                            }
                        );
            }
        );

        const float sampleWidth = 6.9f;
        for (int row = 0; row <= FishermansStaysailGeometry.Rows * 7 / 8; row++)
        for (
            int col = FishermansStaysailGeometry.Columns / 4;
            col <= FishermansStaysailGeometry.Columns * 3 / 4;
            col++
        )
        {
            float u = (float)col / FishermansStaysailGeometry.Columns;
            float v = (float)row / FishermansStaysailGeometry.Rows;
            float camber = FishermansStaysailGeometry.RestCamber(sampleWidth, u, v);
            float travel = FishermansStaysailBillow.ClothTravel(sampleWidth, u, v);
            if (travel >= camber)
                throw new Exception(
                    $"Family interior can cross its neutral plane: u={u}, v={v}, camber={camber:F3}m, travel={travel:F3}m."
                );
            if (travel <= 0)
                throw new Exception("The sail belly must retain some free cloth motion.");
        }
        float center = FishermansStaysailGeometry.RestCamber(sampleWidth, 0.5f, 0);
        if (
            FishermansStaysailGeometry.RestCamber(sampleWidth, 0.25f, 0) < center * 0.65f
            || FishermansStaysailGeometry.RestCamber(sampleWidth, 0.5f, 0.5f) < center * 0.6f
        )
            throw new Exception("Family billow must have rounded shoulders and a fuller middle.");
        CheckTransitions();
        Console.WriteLine(
            "PASS (executed): family rounded billow, bounded interior cloth motion and smooth repeated camber reversals."
        );
    }

    private static Vector3 Mirror(Vector3 p) => new Vector3(p.x, -p.y, p.z);

    private static Vector3[] Pose(
        FishermansStaysailMeshData data,
        float headAngle,
        float width,
        float angle,
        int side,
        float unroll
    )
    {
        var c = data.Corners;
        float deployment = FishermansStaysailBillow.Deployment(unroll);
        var head = FishermansStaysailFixedHead.Position(
            c[1],
            c[0],
            Vector3.right,
            -side,
            headAngle,
            unroll
        );
        var tack = FishermansStaysailReefingGeometry.Pose(c[2], c[0], c[1], unroll);
        var clew = FishermansStaysailReefingGeometry.Pose(c[3], c[0], c[1], unroll);
        clew = FishermansStaysailFrameGeometry.RotateAroundMast(
            clew,
            c[0],
            Vector3.right,
            FishermansStaysailFixedHead.LowerAngle(angle, -side * headAngle, unroll)
        );
        var leech = new Vector3[FishermansStaysailGeometry.Rows + 1];
        if (
            !FishermansStaysailEdgeFit.Fit(
                head,
                clew,
                tack,
                Vector3.up,
                -Vector3.right,
                width,
                side,
                unroll,
                (c[1] - c[3]).magnitude * Math.Max(0.015f, unroll),
                (clew - tack).magnitude,
                leech
            )
        )
            throw new Exception("Mirrored-pose fixture must fit.");
        var normal = FishermansStaysailBillow.CamberNormal(c[0], tack, head, leech[0]);
        var bones = new Vector3[data.BonePositions.Length];
        for (int row = 0; row <= FishermansStaysailGeometry.Rows; row++)
        for (int col = 0; col <= FishermansStaysailGeometry.ShapeColumns; col++)
        {
            float v = (float)row / FishermansStaysailGeometry.Rows;
            bones[FishermansStaysailGeometry.ShapeBone(row, col)] =
                FishermansStaysailBillow.ShapePoint(
                    Vector3.Lerp(c[0], tack, v),
                    leech[FishermansStaysailGeometry.Rows - row],
                    normal,
                    width,
                    (float)col / FishermansStaysailGeometry.ShapeColumns,
                    v,
                    side * deployment
                );
        }
        var posed = new Vector3[data.Vertices.Length];
        for (int i = 0; i < posed.Length; i++)
        {
            var w = data.Weights[i];
            posed[i] =
                data.Vertices[i]
                + (bones[w.boneIndex0] - data.BonePositions[w.boneIndex0]) * w.weight0
                + (bones[w.boneIndex1] - data.BonePositions[w.boneIndex1]) * w.weight1;
        }
        return posed;
    }

    private static void CheckTransitions()
    {
        foreach (int fps in new[] { 15, 60, 144 })
        {
            float camber = 1;
            int side = 1;
            foreach (int desired in new[] { -1, 1, -1, 1 })
            {
                for (int frame = 0; frame < fps * 3; frame++)
                {
                    side = FishermansStaysailBillow.CamberSide(side, desired * 8);
                    float next = FishermansStaysailBillow.SmoothLoad(camber, side, 1f / fps);
                    if (Math.Abs(next - camber) > 6.01f / fps || Math.Abs(next) > 1)
                        throw new Exception("Billow jumps or overshoots on a tack.");
                    camber = next;
                }
                BehaviorCases.Near(camber, desired, 0.001f, "new tack settles");
            }
        }
    }
}
