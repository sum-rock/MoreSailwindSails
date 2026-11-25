"""Compare each cut's baked poses with sequentially evaluated Blender cloth."""

import hashlib
from pathlib import Path
import runpy
import sys

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
tool = runpy.run_path(str(ROOT / "tools/assets/loose_foot_spritsail/collect.py"))
from tools.assets.loose_foot_spritsail.authoring import get_cut

sys.path.insert(0, str(Path(__file__).resolve().parent))
from json_bake_checks import bake as bake_json

cut = sys.argv[sys.argv.index("--") + 1]
definition = get_cut(cut)
asset = Path(bpy.data.filepath)
before = hashlib.sha256(asset.read_bytes()).digest()


def reopen():
    # A fresh file load prevents exporter cache state from validating itself.
    bpy.ops.wm.open_mainfile(filepath=str(asset))


reopen()
source = bake_json(asset="loose_foot_spritsail", cut=cut)
reopen()
assert source == bake_json(asset="loose_foot_spritsail", cut=cut), (
    "Non-deterministic bake"
)
assert source == definition.source_path.read_text(), "Stale bake"
reopen()
data = tool["collect"](
    reef_frames=definition.reef_frames,
    generate_cloth_uv=definition.generate_cloth_uv,
    topsail=definition.topsail,
    spar=definition.spar,
    topsail_hide_frame=definition.topsail_hide_frame,
)
assert len(data["ReefPoses"]) == len(definition.reef_frames)
from spritsail_windcenter_checks import check as check_windcenters

check_windcenters(tool=tool, data=data, cut=cut)
reopen()
sail = bpy.data.objects["Sail"]
top = bpy.data.objects.get("Topsail") if definition.topsail else None
bpy.context.scene.frame_set(0)
inverse = sail.matrix_world.inverted()
if top:
    assert data["TopsailVertexStart"] == len(sail.data.vertices)
    assert data["ReefPoses"][4] == data["ReefPoses"][5]
for frame in range(definition.reef_frames[-1] + 1):
    bpy.context.scene.frame_set(frame)
    if top:
        if definition.topsail_hide_frame is not None:
            assert top.hide_render == (frame >= definition.topsail_hide_frame)
        if top.hide_viewport:
            top.hide_viewport = False
    depsgraph = bpy.context.evaluated_depsgraph_get()
    if definition.spar and frame in definition.reef_frames:
        pose = data["ReefPoses"][definition.reef_frames.index(frame)]
        expected_spar = (
            tool["CONVERSION"]
            @ bpy.data.objects["Spar"].evaluated_get(depsgraph).matrix_world
            @ tool["CONVERSION"]
        )
        assert (
            max(
                abs(pose[2][row][col] - expected_spar[row][col])
                for row in range(4)
                for col in range(4)
            )
            < 1e-6
        )
    offset = 0
    for obj in (sail, top) if top else (sail,):
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        try:
            if frame in definition.reef_frames:
                points, rotation = data["ReefPoses"][
                    definition.reef_frames.index(frame)
                ][:2]
                assert len(points) == len(sail.data.vertices) + (
                    len(top.data.vertices) if top else 0
                )
                assert abs(sum(n * n for n in rotation) - 1) < 1e-5
                for point, vertex in zip(points[offset:], mesh.vertices):
                    expected = (
                        tool["CONVERSION"]
                        @ inverse
                        @ evaluated.matrix_world
                        @ vertex.co
                    )
                    assert (Vector(point) - expected).length < 1e-6, (
                        obj.name,
                        frame,
                        vertex.index,
                    )
                if obj == top and cut == "C" and frame == 120:
                    # Mk.C regression: the old two-pass export stretched an edge from <2 m to >9 m.
                    for edge in mesh.edges:
                        a, b = edge.vertices
                        rest_length = (
                            obj.data.vertices[a].co - obj.data.vertices[b].co
                        ).length
                        assert (
                            Vector(points[offset + a]) - Vector(points[offset + b])
                        ).length < 2 * rest_length
            offset += len(mesh.vertices)
        finally:
            evaluated.to_mesh_clear()
assert data["Struck"]["Vertices"]
assert hashlib.sha256(asset.read_bytes()).digest() == before
print(
    f"PASS: Mk.{cut} deterministic bake, exact animation samples and source preservation"
)
