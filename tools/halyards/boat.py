"""Assemble registered native and SE mast options in the boat coordinate frame."""

import numpy as np

from tools.halyards.options import Options
from tools.halyards.scene import identity

ROLES = ("reefWinch", "leftAngleWinch", "rightAngleWinch", "midAngleWinch")
SE_NAMES = {"LargeDhow": "sanbig"}
IMPORT_PARENTS = {
    "Brig": "medi medium new",
    "Junk": "junk medium (actual)",
    "Jong": "junk large (3)",
    "Sanbuq": "dhow medium new",
    "LargeDhow": "dhow large",
    "Kakam": "junk small",
    "Dhow": "dhow",
}


class Boat:
    """Normalize one boat's parts and conservative native control inventory."""

    def __init__(self, *, scene, profile):
        self.scene, self.profile = scene, profile
        roots = [
            obj
            for obj in scene.native_objects
            if obj.type.name == "MonoBehaviour"
            and scene.script(obj) == "BoatRefs"
            and scene.name(obj) == profile.boat_name
        ]
        if len(roots) != 1:
            raise ValueError(
                f"Expected one installed boat {profile.boat_name}; found {len(roots)}"
            )
        self.refs = roots[0]
        self.root = scene.transform(self.refs)
        self.model = scene.ptr(self.refs, scene.data(self.refs)["boatModel"])
        self.frame = np.linalg.inv(scene.matrix(self.root))
        self.rotation_frame = scene.rotation(self.root).T
        self.nodes = list(scene.descendants(self.model))
        self.parts = scene.component(self.refs, "BoatCustomParts")
        self.groups = [
            [scene.ptr(self.parts, p) for p in group["partOptions"]]
            for group in scene.data(self.parts)["availableParts"]
        ]
        self.native_groups = [list(group) for group in self.groups]
        self.se = None
        self.se_nodes = []
        self.excluded = set()
        self.extra_boxes = []
        self.extra_structure = []
        self.options = Options()
        if scene.has_se:
            name = "SE_parts_" + SE_NAMES.get(profile.name, profile.name.lower())
            matches = [
                obj
                for obj in scene.se_objects
                if obj.type.name == "MonoBehaviour"
                and scene.script(obj) == "SE_BoatCustomData"
                and scene.name(obj) == name
            ]
            if len(matches) != 1:
                raise ValueError(f"Expected one SE boat definition {name}")
            self.se = matches[0]
            parent = (
                scene.find(self.root, IMPORT_PARENTS[profile.name])
                if profile.name in IMPORT_PARENTS
                else self.model
            )
            scene.reparent(self.se, parent)
            self.se_nodes = list(scene.descendants(self.se))
            walk = scene.ptr(self.se, scene.data(self.se)["walkCol"])
            self.excluded.update(identity(t) for t in scene.descendants(walk))
            data = scene.data(self.se)
            for part_ref in data["parts"]:
                part = scene.ptr(self.se, part_ref)
                self.groups.append(
                    [scene.ptr(part, p) for p in scene.data(part)["partOptions"]]
                )
            for group in data["partsNew"]:
                self.groups.append(
                    [scene.ptr(self.se, p) for p in group["partOptions"]]
                )
            for reference in data["options"]:
                extra = scene.ptr(self.se, reference)
                index = scene.data(extra)["parentPartIndex"]
                if index >= 0:
                    self.groups[index].append(scene.component(extra, "BoatPartOption"))
        for group_index, group in enumerate(self.groups):
            for option in group:
                if option is not None:
                    self.options.groups[identity(option)] = group_index
        self.nodes += [
            node for node in self.se_nodes if identity(node) not in self.excluded
        ]
        self.option_objects = {}
        for node in self.nodes:
            option = scene.component(node, "BoatPartOption")
            if option is not None:
                key = identity(option)
                self.option_objects[key] = option
                data = scene.data(option)
                self.options.requires[key] = {
                    identity(scene.ptr(option, p))
                    for p in data["requires"]
                    if p["m_PathID"]
                }
                self.options.disabled[key] = {
                    identity(scene.ptr(option, p))
                    for p in data["requiresDisabled"]
                    if p["m_PathID"]
                }
                extra = scene.component(node, "SE_PartOptionData")
                if extra is not None:
                    edata = scene.data(extra)
                    for field, mapping in [
                        ("requiresVanilla", self.options.requires),
                        ("requiresDisabledVanilla", self.options.disabled),
                    ]:
                        for suffix in ("1", "2"):
                            reference = edata[field + suffix]
                            if len(reference) == 2:
                                group, index = reference
                                # Some SE patches append an empty native option before imports.
                                reference_group = (
                                    self.native_groups[group]
                                    if group < len(self.native_groups)
                                    else self.groups[group]
                                )
                                if index >= len(reference_group):
                                    target = ("empty-native-option", group, index)
                                    self.options.groups[target] = group
                                else:
                                    target = identity(reference_group[index])
                                mapping[key].add(target)
        self.masts = {}
        registered_se = (
            {identity(scene.ptr(self.se, p)) for p in scene.data(self.se)["masts"]}
            if self.se is not None
            else set()
        )
        for node in self.nodes:
            mast = scene.component(node, "Mast")
            if mast is None:
                continue
            if (
                mast.assets_file.name == "level24"
                and not scene.data(mast)["shipRigidbody"]["m_PathID"]
            ):
                continue
            if (
                self.se is not None
                and scene.below(mast, self.se)
                and identity(mast) not in registered_se
            ):
                continue
            option = scene.component(node, "BoatPartOption")
            if option is not None and identity(option) not in self.options.groups:
                continue
            index = scene.data(mast)["orderIndex"]
            if option is None and index not in profile.parents:
                continue
            if index in self.masts:
                raise ValueError(
                    f"Ambiguous registered mast {index}: {scene.name(mast)}"
                )
            self.masts[index] = mast
        self.controls = {}
        self.usages = {}
        for mast in self.masts.values():
            for role in ROLES:
                for reference in scene.data(mast)[role]:
                    control = scene.ptr(mast, reference)
                    if control is not None:
                        key = identity(control)
                        self.controls[key] = control
                        self.usages.setdefault(key, set()).add(self.option(mast))
        if self.se is not None:
            from tools.halyards.expansion import adapt

            adapt(boat=self)

    def option(self, obj):
        scene = self.scene
        node = scene.transform(obj)
        while node is not None and identity(node) != identity(self.root):
            option = scene.component(node, "BoatPartOption")
            if option is not None and identity(option) in self.options.groups:
                return identity(option)
            node = scene.parent(node)
        return None

    def compatible(self, obj, mast):
        return self.options.compatible(self.option(obj), self.option(mast))

    def control_compatible(self, control, mast):
        usages = self.usages.get(identity(control), {None})
        return self.compatible(control, mast) and any(
            self.options.compatible(self.option(mast), usage) for usage in usages
        )
