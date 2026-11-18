"""Render deterministic C# templates and enforce read-only bake checks."""

from pathlib import Path
from jinja2 import Environment, FileSystemLoader, StrictUndefined


def render(*, template: Path, **data) -> str:
    environment = Environment(
        loader=FileSystemLoader(str(template.parent)),
        undefined=StrictUndefined,
        autoescape=False,
        trim_blocks=True,
        lstrip_blocks=True,
        keep_trailing_newline=True,
    )
    return environment.get_template(template.name).render(**data)


def write(*, source: str, output: Path, check: bool) -> None:
    if check:
        if not output.is_file() or output.read_text(encoding="utf-8") != source:
            raise ValueError(f"Stale baked asset: regenerate {output}")
        print(f"PASS: baked source matches extracted data: {output}")
    else:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(source, encoding="utf-8")
        print(f"Baked asset -> {output}")


def bake(*, asset, cut, data):
    if asset in ("flying_sail", "staysail"):
        from tools.assets.grid_bake import bake as render_grid

        return render_grid(data=data, asset=asset, cut=cut)
    from importlib import import_module

    return import_module(f"tools.assets.{asset}.bake").bake(data=data, cut=cut)
