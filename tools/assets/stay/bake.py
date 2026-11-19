"""Render extracted stay meshes as C# outside Blender."""

from pathlib import Path
from tools.assets.rendering import render


def number(n):
    return ("0" if abs(n) < 1e-9 else format(n, ".9g")) + "f"


def bake(*, data, cut="A"):
    dimensions = {name: mesh["dimensions"] for name, mesh in data.items()}

    def formatted(mesh):
        return [
            (
                field,
                kind,
                [
                    str(v)
                    if kind == "int"
                    else f"new {kind}({', '.join(number(n) for n in v)})"
                    for v in mesh[field]
                ],
            )
            for field, kind in (
                ("Vertices", "Vector3"),
                ("Normals", "Vector3"),
                ("UV", "Vector2"),
                ("Triangles", "int"),
            )
        ]

    return render(
        template=Path(__file__).with_name("stay.cs.jinja"),
        dimensions={
            k: number(v) for k, v in (dimensions["Rope"] | dimensions["Collar"]).items()
        },
        meshes=[
            ("CreateRope", formatted(data["Rope"])),
            ("CreateCollar", formatted(data["Collar"])),
        ],
    )
