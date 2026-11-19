"""Render the extracted Flying Sail mesh as C# outside Blender."""

from pathlib import Path
from tools.assets.rendering import render


def bake(*, data, cut="A"):
    def number(value):
        return ("0" if value == 0 else format(value, ".9g")) + "f"

    def vector(point, kind):
        return f"new {kind}({', '.join(number(n) for n in point)})"

    return render(
        template=Path(__file__).with_name("flying_sail.cs.jinja"),
        arrays=(
            ("Vertices", "Vector3", [vector(p, "Vector3") for p in data["Vertices"]]),
            ("UV", "Vector2", [vector(p, "Vector2") for p in data["UV"]]),
            ("Triangles", "int", data["Triangles"]),
        ),
    )
