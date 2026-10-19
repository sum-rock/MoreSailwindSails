"""Read the existing C# boat identity and physical mast ancestry contract."""

from dataclasses import dataclass
import re

from tools.settings import BOAT_PROFILES

PROFILES = {
    "brig": "Brig",
    "junk": "Junk",
    "jong": "Jong",
    "sanbuq": "Sanbuq",
    "baghala": "LargeDhow",
    "large_dhow": "LargeDhow",
    "cog": "Cog",
    "kakam": "Kakam",
    "dhow": "Dhow",
}


@dataclass(frozen=True)
class Profile:
    """The boat identity and validated mast ancestry already authored by the plugin."""

    name: str
    boat_name: str
    parents: dict[int, int]


def read_profile(boat: str) -> Profile:
    name = PROFILES[boat.lower()]
    path = BOAT_PROFILES / f"{name}.cs"
    source = re.sub(r"//[^\n]*", "", path.read_text())
    names = re.findall(r'boatName:\s*"([^"]+)"', source)
    dictionaries = re.findall(
        r"new\s+Dictionary<int,\s*int>\s*\{((?:\s*\{\s*\d+\s*,\s*-?\d+\s*\}\s*,?)+)\s*\}",
        source,
    )
    if len(dictionaries) != 1:
        raise ValueError(f"Unsupported mast dictionary in {path}")
    entries = re.findall(r"\{\s*(\d+)\s*,\s*(-?\d+)\s*\}", dictionaries[0])
    if len(names) != 1 or not entries:
        raise ValueError(f"Unsupported boat profile declaration in {path}")
    parents = {int(child): int(parent) for child, parent in entries}
    if len(parents) != len(entries):
        raise ValueError(f"Duplicate mast in {path}")
    for child in parents:
        seen = set()
        while child != -1:
            if child in seen or child not in parents:
                raise ValueError(f"Invalid mast ancestry in {path}")
            seen.add(child)
            child = parents[child]
    return Profile(name=name, boat_name=names[0], parents=parents)
