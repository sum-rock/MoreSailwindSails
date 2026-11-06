"""Compare each cut's baked poses with sequentially evaluated Blender cloth."""

import hashlib
from pathlib import Path
import runpy
import sys

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
tool = runpy.run_path(str(ROOT / "tools/assets/bake_loose_foot_spritsail.py"))
from loose_foot_spritsail_authoring import get_cut

cut = sys.argv[sys.argv.index("--") + 1]
definition = get_cut(cut)
asset = Path(bpy.data.filepath)
before = hashlib.sha256(asset.read_bytes()).digest()
source = tool["bake"](cut)
assert source == tool["bake"](cut), "Non-deterministic bake"
assert source == definition.source_path.read_text(), "Stale bake"
data = tool["collect"](reef_frames=definition.reef_frames)
assert len(data["ReefPoses"]) == 5
sail = bpy.data.objects["Sail"]
inverse = sail.matrix_world.inverted()
for frame in range(241):
    bpy.context.scene.frame_set(frame)
    evaluated = sail.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    try:
        if frame in definition.reef_frames:
            points, rotation = data["ReefPoses"][definition.reef_frames.index(frame)]
            assert len(points) == len(mesh.vertices)
            assert abs(sum(n * n for n in rotation) - 1) < 1e-5
            for point, vertex in zip(points, mesh.vertices):
                expected = (
                    tool["CONVERSION"] @ inverse @ evaluated.matrix_world @ vertex.co
                )
                assert (Vector(point) - expected).length < 1e-6
    finally:
        evaluated.to_mesh_clear()
assert data["Struck"]["Vertices"]
assert hashlib.sha256(asset.read_bytes()).digest() == before
print(
    f"PASS: Mk.{cut} deterministic bake, exact animation samples and source preservation"
)
