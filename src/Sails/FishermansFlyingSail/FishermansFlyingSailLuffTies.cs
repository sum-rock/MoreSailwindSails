using MoreSailwindSails.Visuals;
using UnityEngine;

namespace MoreSailwindSails.Sails.FishermansFlyingSail
{
    internal sealed class FishermansFlyingSailLuffTies : MonoBehaviour
    {
        public RoutedRope[] Ropes;
        public Transform[] MastEnds;
        public Transform[] CornerEnds;

        internal static FishermansFlyingSailLuffTies Create(
            Transform parent,
            Transform[] bones,
            RopeEffect source
        )
        {
            var root = new GameObject("FishermansFlyingSail fixed luff ties");
            root.transform.SetParent(parent, false);
            var ties = root.AddComponent<FishermansFlyingSailLuffTies>();
            ties.Ropes = new RoutedRope[2];
            ties.MastEnds = new Transform[2];
            ties.CornerEnds = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                ties.Ropes[i] = FishermansFlyingSailSupportLine.CreateRenderer(
                    root.transform,
                    "FishermansFlyingSail luff tie " + i,
                    source,
                    2
                );
                ties.MastEnds[i] = new GameObject("Mast tie attachment " + i).transform;
                ties.MastEnds[i].SetParent(root.transform, false);
                ties.CornerEnds[i] = new GameObject("Luff tie attachment " + i).transform;
                ties.CornerEnds[i].SetParent(bones[i * 2], false);
            }
            return ties;
        }

        internal void Draw(Vector3 aftDirection)
        {
            for (int i = 0; i < Ropes.Length; i++)
            {
                var corner = CornerEnds[i].position;
                MastEnds[i].position = FishermansFlyingSailFrameGeometry.TieAnchor(
                    corner,
                    aftDirection
                );
                Ropes[i].SetPosition(0, MastEnds[i].position);
                Ropes[i].SetPosition(1, corner);
                Ropes[i].SetVisible(visible: true);
            }
        }

        internal void Hide()
        {
            foreach (var rope in Ropes)
                rope.SetVisible(visible: false);
        }

        private void OnDisable()
        {
            if (Ropes != null)
                Hide();
        }
    }
}
