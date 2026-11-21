"""Check numeric precision, matrix column order and non-finite rejection in the handoff."""

import json
from pathlib import Path
import sys
import tempfile
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from tools.assets.extract import write

with tempfile.TemporaryDirectory() as directory:
    output = Path(directory) / "asset.json"
    matrix = Matrix(((1, 2, 3, 4), (5, 6, 7, 8), (9, 10, 11, 12), (13, 14, 15, 16)))
    data = {
        "matrix": matrix,
        "vector": Vector((1, 2, 3)),
        "precision": 0.12345678912345678,
        "poses": [[1, 2], [1, 2]],
    }
    write(data=data, output=output)
    decoded = json.loads(output.read_text())
    assert decoded["matrix"] == [
        [1, 5, 9, 13],
        [2, 6, 10, 14],
        [3, 7, 11, 15],
        [4, 8, 12, 16],
    ]
    assert (
        decoded["precision"] == data["precision"] and decoded["poses"] == data["poses"]
    )
    assert decoded["vector"] == [1, 2, 3]
    before = output.read_bytes()
    for invalid in (float("nan"), float("inf"), -float("inf")):
        try:
            write(data={"invalid": invalid}, output=output)
        except ValueError:
            pass
        else:
            raise AssertionError("Accepted non-finite JSON")
        assert output.read_bytes() == before
print("PASS: JSON precision, matrix columns, repeated poses, and non-finite rejection")
