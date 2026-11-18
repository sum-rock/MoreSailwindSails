"""Shared bake regression checks; mutate temporary mesh copies without saving assets."""

import bpy


def check(*, bake, source_path, object_name):
    source = bake()
    assert source == bake(), "Bake is not deterministic"
    assert source == source_path.read_text(encoding="utf-8"), "Stale checked-in bake"
    obj = bpy.data.objects[object_name]
    original = obj.data

    def reject(label, edit):
        obj.data = original.copy()
        try:
            edit(obj)
            try:
                bake()
            except ValueError:
                pass
            else:
                raise AssertionError(f"Accepted incompatible edit: {label}")
        finally:
            modified = obj.data
            obj.data = original
            obj.location = (0, 0, 0)
            obj.modifiers.clear()
            bpy.context.view_layer.update()
            bpy.data.meshes.remove(modified)

    def move_object(obj):
        obj.location.x = 1
        bpy.context.view_layer.update()

    def add_vertex(obj):
        obj.data.vertices.add(count=1)

    def flip_face(obj):
        face = obj.data.polygons[0]
        face.vertices = tuple(reversed(face.vertices))

    # Authored shape and continuous UV edits must reach the exported data.
    obj.data = original.copy()
    try:
        obj.data.vertices[10].co = (0.1, 0.2, -0.3)
        reshaped = bake()
        assert reshaped != source, "Authored shape edit was ignored"
        for loop in obj.data.loops:
            if loop.vertex_index == 10:
                obj.data.uv_layers[0].data[loop.index].uv = (0.2, 0.3)
        assert bake() != reshaped, "Authored UV edit was ignored"
    finally:
        modified = obj.data
        obj.data = original
        bpy.data.meshes.remove(modified)

    reject("zero span", lambda o: setattr(o.data.vertices[24].co, "x", 0))
    reject("negative span", lambda o: setattr(o.data.vertices[24].co, "x", -1))
    reject(
        "non-finite UV",
        lambda o: setattr(o.data.uv_layers[0].data[0], "uv", (float("nan"), 0)),
    )
    reject("object transform", move_object)
    reject("vertex count", add_vertex)
    reject(
        "non-finite vertex",
        lambda o: setattr(o.data.vertices[10].co, "z", float("nan")),
    )
    reject("UV seam", lambda o: setattr(o.data.uv_layers[0].data[1], "uv", (0, 0)))
    reject("missing UV layer", lambda o: o.data.uv_layers.remove(o.data.uv_layers[0]))
    reject("winding", flip_face)
    reject("modifier", lambda o: o.modifiers.new(name="Solidify", type="SOLIDIFY"))
    assert bake() == source, "Checks changed the source asset"

    print(
        f"PASS: {object_name} deterministic JSON bake and incompatible edit rejection."
    )
