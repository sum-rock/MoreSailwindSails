"""Render extracted sail grids and animation as C# outside Blender."""

from pathlib import Path
from tools.assets.rendering import render
from tools.assets.staysail.authoring import SHEET_FRAMES, REEF_FRAMES, EASED_FRAMES


def bake(*, data, asset, cut="A"):
    animated = asset == "staysail" and cut == "B"

    def number(value, *, animated=False):
        zero = abs(value) < 1e-10 if animated else value == 0
        return ("0" if zero else format(value, ".9g")) + "f"

    def vector(point, *, kind="Vector3", animated=False):
        return f"new {kind}({', '.join(number(n, animated=animated) for n in point)})"

    return render(
        template=Path(__file__).with_name("grid.cs.jinja"),
        asset=asset,
        cut=cut,
        animated=animated,
        class_name=f"FishermansStaysailMk{cut}BakedMesh"
        if asset == "staysail"
        else "FishermansFlyingSailBakedMesh",
        namespace=f"MoreSailwindSails.Sails.FishermansStaysail.Mk{cut}"
        if asset == "staysail"
        else "MoreSailwindSails.Sails.FishermansFlyingSail",
        data_type="FishermansStaysailMeshData"
        if asset == "staysail"
        else "FishermansFlyingSailMeshData",
        arrays=(
            ("Vertices", "Vector3", [vector(p) for p in data["Vertices"]]),
            ("UV", "Vector2", [vector(p, kind="Vector2") for p in data["UV"]]),
            ("Triangles", "int", data["Triangles"]),
        ),
        reef_fractions=", ".join(
            number((frame - 200) / 220, animated=True) for frame in REEF_FRAMES
        ),
        channels=(
            ("Sheet", SHEET_FRAMES),
            ("ReefIn", REEF_FRAMES),
            ("ReefOut", EASED_FRAMES),
        ),
        poses={
            frame: [vector(p, animated=True) for p in points]
            for frame, points in data["Poses"]
        },
    )
