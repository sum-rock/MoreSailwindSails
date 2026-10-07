using MoreSailwindSails.Visuals;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansFlyingSail
{
    // Owns the visible sheets and their shared extensions to the fore mast.
    // Native sheet controllers still calculate tension and operate the winches.
    internal sealed class FishermansFlyingSailSupportLine : MonoBehaviour
    {
        public RoutedRope[] UpperSheets;
        public RoutedRope[] LowerSheets;
        public RoutedRope[] SharedSpans;
        public RopeEffect[] NativeSheets;
        public FishermansFlyingSailLuffTies LuffTies;
        public FishermansFlyingSailKnots Knots;
        private readonly Vector3[] points = new Vector3[33];

        internal static FishermansFlyingSailSupportLine Create(
            Transform parent,
            RopeEffect left,
            RopeEffect right,
            Transform[] bones
        )
        {
            var root = new GameObject("FishermansFlyingSail sheets");
            root.transform.SetParent(parent, false);
            var route = root.AddComponent<FishermansFlyingSailSupportLine>();
            route.NativeSheets = new[] { left, right };
            route.UpperSheets = new RoutedRope[2];
            route.LowerSheets = new RoutedRope[2];
            route.SharedSpans = new RoutedRope[3];
            route.LuffTies = FishermansFlyingSailLuffTies.Create(root.transform, bones, left);
            for (int i = 0; i < 2; i++)
            {
                var source = route.NativeSheets[i];
                source.gameObject.AddComponent<FishermansFlyingSailNativeSheetVisual>();
                route.UpperSheets[i] = CreateRenderer(
                    root.transform,
                    "Upper sheet " + i,
                    source,
                    33
                );
                route.LowerSheets[i] = CreateRenderer(
                    root.transform,
                    "Lower sheet " + i,
                    source,
                    33
                );
            }
            for (int i = 0; i < 3; i++)
                route.SharedSpans[i] = CreateRenderer(root.transform, "Shared span " + i, left, 33);
            return route;
        }

        internal static RoutedRope CreateRenderer(
            Transform parent,
            string name,
            RopeEffect source,
            int count
        )
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rope = root.AddComponent<LineRenderer>();
            var original = source.GetComponent<LineRenderer>();
            rope.sharedMaterials = original.sharedMaterials;
            rope.startColor = original.startColor;
            rope.endColor = original.endColor;
            rope.startWidth = source.ropeWidth;
            rope.endWidth = source.ropeWidth;
            rope.textureMode = LineTextureMode.Tile;
            rope.useWorldSpace = true;
            rope.numCornerVertices = 2;
            rope.numCapVertices = 2;
            rope.shadowCastingMode = original.shadowCastingMode;
            rope.receiveShadows = original.receiveShadows;
            rope.positionCount = count;
            return RoutedRope.Attach(line: rope);
        }

        internal void Draw(
            Transform[] bones,
            Vector3 aftGuide,
            Vector3 foreGuide,
            Vector3 aftDirection,
            bool struck
        )
        {
            var controls = Vector3.zero;
            float sharedSlack = 1;
            int active = 0;
            for (int side = 0; side < NativeSheets.Length; side++)
            {
                var source = NativeSheets[side];
                if (!source || !source.gameObject.activeInHierarchy)
                    continue;
                controls += source.transform.position;
                sharedSlack = Mathf.Min(sharedSlack, Slack(source));
                active++;
            }
            if (active == 0)
            {
                Hide();
                return;
            }
            controls /= active;
            var head = bones[1].position;
            var clew = bones[3].position;
            var top = bones[0].position;
            var tack = bones[2].position;
            var headDirection = head - top;
            var footDirection = clew - tack;
            float headLength = (head - top).magnitude;
            float footLength = (clew - tack).magnitude;
            if (struck)
            {
                LuffTies.Hide();
                if (Knots)
                    Knots.Hide();
                foreach (var span in SharedSpans)
                    span.SetVisible(visible: false);
            }
            else
            {
                LuffTies.Draw(aftDirection);
                if (Knots)
                    Knots.Draw(bones, aftGuide, controls, aftDirection);
                DrawSpan(
                    SharedSpans[0],
                    top,
                    head,
                    aftDirection,
                    headDirection,
                    FishermansFlyingSailFrameGeometry.TieLength,
                    headLength,
                    sharedSlack
                );
                DrawDirectSpan(SharedSpans[1], head, aftGuide, sharedSlack);
                DrawSpan(
                    SharedSpans[2],
                    tack,
                    clew,
                    aftDirection,
                    footDirection,
                    FishermansFlyingSailFrameGeometry.TieLength,
                    footLength,
                    sharedSlack
                );
            }
            for (int side = 0; side < NativeSheets.Length; side++)
            {
                var source = NativeSheets[side];
                if (!source || !source.gameObject.activeInHierarchy)
                {
                    UpperSheets[side].SetVisible(visible: false);
                    LowerSheets[side].SetVisible(visible: false);
                    continue;
                }
                var control = source.transform.position;
                DrawDirectSpan(UpperSheets[side], aftGuide, control, Slack(source));
                DrawDirectSpan(
                    LowerSheets[side],
                    struck ? foreGuide : clew,
                    control,
                    Slack(source)
                );
            }
        }

        private static float Slack(RopeEffect source) =>
            FishermansFlyingSailFrameGeometry.SheetSlack(
                source.currentRopeLength,
                source.totalRopeLength
            );

        private void DrawDirectSpan(RoutedRope rope, Vector3 start, Vector3 end, float slack)
        {
            for (int i = 0; i < points.Length; i++)
                points[i] = FishermansFlyingSailRopeGeometry.DirectPoint(
                    start,
                    end,
                    slack,
                    (float)i / (points.Length - 1)
                );
            rope.SetPositions(points);
            rope.SetVisible(visible: true);
        }

        private void DrawSpan(
            RoutedRope rope,
            Vector3 start,
            Vector3 end,
            Vector3 startDirection,
            Vector3 endDirection,
            float beforeLength,
            float afterLength,
            float slack
        )
        {
            float length = (end - start).magnitude;
            var startHandle = FishermansFlyingSailRopeGeometry.Handle(
                startDirection,
                beforeLength,
                length
            );
            var endHandle = FishermansFlyingSailRopeGeometry.Handle(
                endDirection,
                afterLength,
                length
            );
            for (int i = 0; i < points.Length; i++)
                points[i] = FishermansFlyingSailRopeGeometry.Point(
                    start,
                    end,
                    startHandle,
                    endHandle,
                    slack,
                    (float)i / (points.Length - 1)
                );
            rope.SetPositions(points);
            rope.SetVisible(visible: true);
        }

        internal void Hide()
        {
            foreach (var sheet in UpperSheets)
                sheet.SetVisible(visible: false);
            foreach (var sheet in LowerSheets)
                sheet.SetVisible(visible: false);
            foreach (var span in SharedSpans)
                span.SetVisible(visible: false);
            LuffTies.Hide();
            if (Knots)
                Knots.Hide();
        }

        private void OnDisable()
        {
            if (UpperSheets != null && LowerSheets != null && SharedSpans != null && LuffTies)
                Hide();
        }
    }
}
