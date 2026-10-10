using UnityEngine;

namespace MoreSailwindSails.Sails.Spritsail
{
    // Fits the authored edging to an existing luff and its rope loops to the physical mast.
    internal static class SpritsailMountGeometry
    {
        internal const float SeamOverlap = 0.002f;

        internal static Vector3 Eyelet(Vector3[] points, float sourceX) =>
            new SpritsailMountFitData.LuffSample(sourceX: sourceX, count: points.Length).Evaluate(
                luff: points
            )
            + Vector3.forward * (SeamOverlap - 0.05f);

        internal static void Fit(
            Vector3[] luff,
            Vector3 mastOrigin,
            Vector3 mastAxis,
            float[] radii,
            Vector3[] vertices,
            Vector3[] normals,
            Vector3[] eyelets
        )
        {
            var data = SpritsailMountFitData.ForLuffCount(count: luff.Length);
            for (int eye = 0; eye < 7; eye++)
                eyelets[eye] =
                    data.Eyelets[eye].Evaluate(luff: luff)
                    + Vector3.forward * (SeamOverlap - 0.05f);
            for (int j = 0; j < data.Indices[0].Length; j++)
            {
                int i = data.Indices[0][j];
                int sample = j * 5;
                vertices[i] = data.Strip[sample].Evaluate(points: luff, eyelets: eyelets);
                var tangent =
                    data.Strip[sample + 1].Evaluate(points: luff, eyelets: eyelets)
                    - data.Strip[sample + 2].Evaluate(points: luff, eyelets: eyelets);
                var transverse =
                    data.Strip[sample + 3].Evaluate(points: luff, eyelets: eyelets)
                    - data.Strip[sample + 4].Evaluate(points: luff, eyelets: eyelets);
                normals[i] = (Vector3.Cross(lhs: transverse, rhs: tangent) / 0.000004f).normalized;
                if (SpritsailMountAsset.Normals[i].y < 0)
                    normals[i] = -normals[i];
            }
            for (int eye = 0; eye < 7; eye++)
            {
                var eyelet = eyelets[eye];
                foreach (int i in data.Indices[eye + 1])
                {
                    var source = SpritsailMountAsset.Positions[i];
                    var normal = SpritsailMountAsset.Normals[i];
                    vertices[i] =
                        eyelet
                        + new Vector3(
                            source.x - SpritsailMountAsset.EyeletX[eye],
                            -source.y,
                            -(source.z + 0.05f)
                        );
                    normals[i] = new Vector3(normal.x, -normal.y, -normal.z);
                }
                var center =
                    mastOrigin + mastAxis * Vector3.Dot(lhs: eyelet - mastOrigin, rhs: mastAxis);
                var outward = (eyelet - center).normalized;
                var side = Vector3.Cross(lhs: mastAxis, rhs: outward).normalized;
                float ropeRadius = SpritsailMountAsset.RopeRadii[eye];
                var frame = new RopeFrame(
                    eyelet: eyelet,
                    center: center,
                    axis: mastAxis,
                    outward: outward,
                    side: side,
                    fitted: radii[eye] + ropeRadius * 2
                );
                for (int j = 0; j < data.Indices[eye + 8].Length; j++)
                {
                    int i = data.Indices[eye + 8][j];
                    ref readonly var source = ref data.Ropes[eye][j];
                    var point = frame.Point(sample: in source.Point);
                    var tangent = (
                        frame.Point(sample: in source.After) - frame.Point(sample: in source.Before)
                    ).normalized;
                    var radial = Vector3.Cross(lhs: tangent, rhs: mastAxis).normalized;
                    normals[i] = (
                        mastAxis * source.Normal.x
                        + radial * source.Normal.y
                        + tangent * source.Normal.z
                    ).normalized;
                    vertices[i] = point + mastAxis * source.Axial + normals[i] * ropeRadius;
                }
            }
        }

        // The two approach curves share these controls for every vertex of one rope.
        private readonly struct RopeFrame
        {
            private readonly Vector3 eyelet;
            private readonly Vector3 center;
            private readonly Vector3 axis;
            private readonly Vector3 outward;
            private readonly Vector3 side;
            private readonly Vector3 positiveB;
            private readonly Vector3 positiveC;
            private readonly Vector3 positiveD;
            private readonly Vector3 negativeB;
            private readonly Vector3 negativeC;
            private readonly Vector3 negativeD;
            private readonly float fitted;

            internal RopeFrame(
                Vector3 eyelet,
                Vector3 center,
                Vector3 axis,
                Vector3 outward,
                Vector3 side,
                float fitted
            )
            {
                this.eyelet = eyelet;
                this.center = center;
                this.axis = axis;
                this.outward = outward;
                this.side = side;
                this.fitted = fitted;
                positiveB = eyelet - Vector3.up * 0.02f;
                negativeB = eyelet - Vector3.up * -0.02f;
                positiveD = center + side * fitted;
                negativeD = center + side * -fitted;
                positiveC = positiveD + outward * (fitted * 0.55f);
                negativeC = negativeD + outward * (fitted * 0.55f);
            }

            internal Vector3 Point(in SpritsailMountFitData.RopeSample sample)
            {
                if (sample.Collar)
                    return center + (outward * sample.Cos + side * sample.Sin) * fitted;
                var b = sample.Sign > 0 ? positiveB : negativeB;
                var c = sample.Sign > 0 ? positiveC : negativeC;
                var d = sample.Sign > 0 ? positiveD : negativeD;
                var point = eyelet * sample.A + b * sample.B + c * sample.C + d * sample.D;
                var axialCenter = center + axis * Vector3.Dot(lhs: point - center, rhs: axis);
                var radial = point - axialCenter;
                if (radial.sqrMagnitude < fitted * fitted)
                    point =
                        axialCenter
                        + (radial.sqrMagnitude > 0.000001f ? radial.normalized : outward) * fitted;
                return point;
            }
        }
    }
}
