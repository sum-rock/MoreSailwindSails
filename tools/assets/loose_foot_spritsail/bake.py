"""Render extracted spritsail assemblies as C# outside Blender."""

from pathlib import Path
from tools.assets.loose_foot_spritsail.authoring import get_cut
from tools.assets.rendering import render

PARTS = ("Sail", "Sprit", "SnotterAssembly", "RotationPlate", "Struck")
ATTACHMENTS = (
    "ReefLineAttachment",
    "SheetLineAttachment",
    "StruckReefLineAttachment",
    "StruckSheetLineAttachment",
)
CORNERS = ("Tack", "Throat", "Peak", "Clew", "SheetVertex")


def number(value):
    return ("0" if value == 0 else format(value, ".9g")) + "f"


def vector(values, kind="Vector3"):
    return f"new {kind}({', '.join(number(n) for n in values)})"


def bake(*, data, cut="A"):
    definition = get_cut(cut)
    names = (*PARTS, *(("Spar",) if definition.spar else ()))
    poses = []
    for pose in data["ReefPoses"]:
        points, rotation = pose[:2]
        spar = pose[2] if definition.spar else None
        poses.append(
            {
                "points": [vector(p) for p in points],
                "rotation": vector(rotation, "Quaternion"),
                "spar": ", ".join(vector(column, "Vector4") for column in spar)
                if spar is not None
                else None,
            }
        )
    return render(
        template=Path(__file__).with_name("spritsail.cs.jinja"),
        cut=cut,
        definition=definition,
        parts={name: data[name] for name in names},
        poses=poses,
        radius=number(data["CollarInnerRadius"]),
        attachments={name: vector(data[name]) for name in ATTACHMENTS},
        corners={name: data[name] for name in CORNERS},
        topsail=data,
        vector=vector,
        number=number,
    )
