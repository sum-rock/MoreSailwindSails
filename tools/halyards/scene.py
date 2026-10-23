"""Read Unity object identities, hierarchy transforms and meshes without exporting assets."""

from pathlib import Path

import numpy as np
import UnityPy
from UnityPy.classes import PPtr
from UnityPy.helpers.MeshHelper import MeshHandler
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

from tools.halyards.geometry import quaternion, transform_matrix, transform_points
from tools.halyards.typetrees import component_node


def identity(obj):
    return (obj.assets_file.name, obj.path_id)


class Scene:
    """Own parsed objects and temporary import-parent overrides for one analysis."""

    def __init__(self, *, game_dir: Path, include_se: bool):
        self.game_dir = game_dir
        data_dir = game_dir / "Sailwind_Data"
        for required in (
            data_dir / "level24",
            data_dir / "Managed/Assembly-CSharp.dll",
        ):
            if not required.is_file():
                raise ValueError(f"Missing installed Sailwind input: {required}")
        self.env = UnityPy.load(str(data_dir / "level24"))
        self.native_objects = list(self.env.objects)
        self.se_objects = []
        bundle = (
            game_dir / "BepInEx/plugins/ShipyardExpansion/shipyard_expansion.assets"
        )
        self.has_se = include_se and bundle.is_file()
        if self.has_se:
            for filename in ("ShipyardExpansion.dll", "SE_Bridge.dll"):
                if not (bundle.parent / filename).is_file():
                    raise ValueError(
                        f"Incomplete SE installation: missing {bundle.parent / filename}"
                    )
            loaded = self.env.load_file(str(bundle))
            self.se_objects = [
                obj
                for file in loaded.files.values()
                if hasattr(file, "objects")
                for obj in file.objects.values()
            ]
        self.env.path = str(data_dir)
        version = str(self.native_objects[0].assets_file.unity_version)
        generator = TypeTreeGenerator(version)
        generator.load_local_dll_folder(str(data_dir / "Managed"))
        if self.has_se:
            generator.load_local_dll_folder(str(bundle.parent))
        self.env.typetree_generator = generator
        self.data_cache = {}
        self.node_cache = {}
        self.script_cache = {}
        self.component_cache = {}
        self.parent_overrides = {}
        self.local_overrides = {}
        self.matrix_cache = {}
        self.rotation_cache = {}
        self.mesh_cache = {}
        self.mesh_overrides = {}

    def data(self, obj):
        key = identity(obj)
        if key not in self.data_cache:
            node = None
            if obj.type.name == "MonoBehaviour" and not obj.serialized_type.node:
                header = obj.parse_monobehaviour_head()
                script_key = (identity(header.m_Script.deref()), str(obj.version))
                if script_key not in self.node_cache:
                    self.node_cache[script_key] = component_node(obj)
                node = self.node_cache[script_key]
            self.data_cache[key] = obj.parse_as_dict(node=node)
        return self.data_cache[key]

    def ptr(self, owner, value):
        if not value or not value["m_PathID"]:
            return None
        return PPtr(
            m_FileID=value["m_FileID"],
            m_PathID=value["m_PathID"],
            assetsfile=owner.assets_file,
        ).deref()

    def script(self, obj):
        if obj.type.name != "MonoBehaviour":
            return obj.type.name
        # Read the Unity base header independently of generated game-field metadata.
        # This also avoids decoding unrelated game components just to identify them.
        header = obj.parse_monobehaviour_head()
        if not header.m_Script:
            return ""
        script = header.m_Script.deref()
        key = identity(script)
        if key not in self.script_cache:
            self.script_cache[key] = script.parse_as_object().m_ClassName
        return self.script_cache[key]

    def game_object(self, obj):
        return (
            obj
            if obj.type.name == "GameObject"
            else self.ptr(obj, self.data(obj)["m_GameObject"])
        )

    def name(self, obj):
        return self.data(self.game_object(obj))["m_Name"]

    def components(self, obj):
        go = self.game_object(obj)
        key = identity(go)
        if key not in self.component_cache:
            self.component_cache[key] = [
                self.ptr(go, c["component"]) for c in self.data(go)["m_Component"]
            ]
        return self.component_cache[key]

    def component(self, obj, kind):
        return next((c for c in self.components(obj) if self.script(c) == kind), None)

    def transform(self, obj):
        if obj.type.name in ("Transform", "RectTransform"):
            return obj
        return next(
            c
            for c in self.components(obj)
            if c.type.name in ("Transform", "RectTransform")
        )

    def parent(self, obj):
        t = self.transform(obj)
        return self.parent_overrides.get(
            identity(t), self.ptr(t, self.data(t)["m_Father"])
        )

    def reparent(self, child, parent):
        self.parent_overrides[identity(self.transform(child))] = self.transform(parent)
        self.matrix_cache.clear()
        self.rotation_cache.clear()

    def set_local(self, obj, **fields):
        transform = self.transform(obj)
        self.local_overrides[identity(transform)] = fields
        self.matrix_cache.clear()
        self.rotation_cache.clear()

    def local_data(self, transform):
        return self.data(transform) | self.local_overrides.get(identity(transform), {})

    def matrix(self, obj):
        t = self.transform(obj)
        key = identity(t)
        if key not in self.matrix_cache:
            local = transform_matrix(self.local_data(t))
            parent = self.parent(t)
            self.matrix_cache[key] = (
                local if parent is None else self.matrix(parent) @ local
            )
        return self.matrix_cache[key]

    def rotation(self, obj):
        t = self.transform(obj)
        key = identity(t)
        if key not in self.rotation_cache:
            local = quaternion(self.local_data(t)["m_LocalRotation"])
            parent = self.parent(t)
            self.rotation_cache[key] = (
                local if parent is None else self.rotation(parent) @ local
            )
        return self.rotation_cache[key]

    def children(self, obj):
        t = self.transform(obj)
        return [self.ptr(t, p) for p in self.data(t)["m_Children"]]

    def descendants(self, obj):
        yield self.transform(obj)
        for child in self.children(obj):
            yield from self.descendants(child)

    def find(self, obj, path):
        current = self.transform(obj)
        for name in path.split("/"):
            matches = [
                child for child in self.children(current) if self.name(child) == name
            ]
            if len(matches) != 1:
                raise ValueError(
                    f"Expected one {path!r} under {self.name(obj)}; found {len(matches)} at {name!r}"
                )
            current = matches[0]
        return current

    def below(self, obj, ancestor):
        current = self.transform(obj)
        target = identity(self.transform(ancestor))
        while current is not None:
            if identity(current) == target:
                return True
            current = self.parent(current)
        return False

    def mesh(self, mesh):
        key = identity(mesh)
        if key not in self.mesh_cache:
            handler = MeshHandler(mesh.parse_as_object())
            handler.process()
            if handler.m_VertexCount == 0 and not handler.m_IndexBuffer:
                self.mesh_cache[key] = np.empty((0, 3)), np.empty((0, 3), dtype=int)
                return self.mesh_cache[key]
            vertices = np.asarray(handler.m_Vertices, dtype=float)
            triangles = np.asarray(
                [
                    triangle
                    for submesh in handler.get_triangles()
                    for triangle in submesh
                ],
                dtype=int,
            )
            if (
                vertices.ndim != 2
                or vertices.shape[1] != 3
                or triangles.ndim != 2
                or triangles.shape[1] != 3
            ):
                raise ValueError(f"Unsupported or empty triangle mesh: {key}")
            if not np.isfinite(vertices).all():
                raise ValueError(f"Non-finite mesh: {key}")
            self.mesh_cache[key] = vertices, triangles
        return self.mesh_cache[key]

    def render_mesh(self, obj, frame):
        mesh_filter = self.component(obj, "MeshFilter")
        renderer = self.component(obj, "MeshRenderer")
        if mesh_filter is None or renderer is None:
            return None
        mesh = self.mesh_overrides.get(identity(mesh_filter))
        if mesh is None:
            mesh = self.ptr(mesh_filter, self.data(mesh_filter)["m_Mesh"])
        if mesh is None:
            return None
        vertices, indices = self.mesh(mesh)
        if not len(vertices):
            return None
        vertices = transform_points(frame @ self.matrix(obj), vertices)
        return vertices, vertices[indices]
