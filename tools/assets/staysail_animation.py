"""Evaluate Mk.B cloth sequentially and export normalized animation samples."""

import math
import bpy
from sail_authoring import require

SHEET_FRAMES = (60, 90, 120)
REEF_FRAMES = (200, 240, 280, 320, 360, 400, 420)
EASED_FRAMES = tuple(frame + 300 for frame in REEF_FRAMES)


def samples():
    obj = bpy.data.objects["Fishermans Staysail Mk.B"]
    require(
        all(
            m.type in ("HOOK", "CLOTH", "COLLISION") and m.show_viewport
            for m in obj.modifiers
        ),
        "expected enabled Hook/Cloth/Collision modifiers",
    )
    topology = [tuple(p.vertices) for p in obj.data.polygons]
    span = obj.data.vertices[24].co.x
    frames = set(SHEET_FRAMES + REEF_FRAMES + EASED_FRAMES)
    result = {}
    # Reset to before the simulation start; force evaluation on every frame.
    bpy.context.scene.frame_set(0)
    for frame in range(1, max(frames) + 1):
        bpy.context.scene.frame_set(frame)
        evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        try:
            require(
                len(mesh.vertices) == 825
                and [tuple(p.vertices) for p in mesh.polygons] == topology,
                f"topology changed at frame {frame}",
            )
            if frame in frames:
                points = [
                    (v.co.z / span, -v.co.y / span, v.co.x / span - 1)
                    for v in mesh.vertices
                ]
                require(
                    all(math.isfinite(n) for p in points for n in p),
                    f"non-finite frame {frame}",
                )
                result[frame] = points
        finally:
            evaluated.to_mesh_clear()
    return result


def bake_animation():
    poses = samples()
    # Simulations have different settling histories. Use the deployed sheet
    # samples at zero reef so the control surface has a single continuous seam.
    for reef, sheet in ((200, 60), (500, 120)):
        corners = (0, 24, 800, 824)
        require(
            max(
                abs(a - b)
                for i in corners
                for a, b in zip(poses[reef][i], poses[sheet][i])
            )
            < 1e-4,
            "deployed reef and sheet attachments disagree",
        )
        poses[reef] = poses[sheet]

    def number(n):
        return ("0" if abs(n) < 1e-10 else format(n, ".9g")) + "f"

    lines = [
        "        internal static FishermansStaysailAnimationData CreateAnimation() =>",
        "            new FishermansStaysailAnimationData",
        "            {",
        "                CornerIndices = new int[] { 0, 24, 800, 824 },",
        "                ReefFractions = new float[] { "
        + ", ".join(number((f - 200) / 220) for f in REEF_FRAMES)
        + " },",
    ]
    for name, frames in (
        ("Sheet", SHEET_FRAMES),
        ("ReefIn", REEF_FRAMES),
        ("ReefOut", EASED_FRAMES),
    ):
        lines += [f"                {name} = new Vector3[][]", "                {"]
        for frame in frames:
            lines += [
                f"                    // Blender frame {frame}.",
                "                    new Vector3[]",
                "                    {",
            ]
            lines += [
                "                        new Vector3("
                + ", ".join(number(n) for n in p)
                + "),"
                for p in poses[frame]
            ]
            lines += ["                    },"]
        lines += ["                },"]
    lines += ["            };", ""]
    return "\n".join(lines)
