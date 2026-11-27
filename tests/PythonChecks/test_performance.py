"""Use synthetic captures to verify comparisons without installations or services."""

import csv
import json
import math

from click.testing import CliRunner
import pytest

from tools.cli import main
from tools.performance.cli import ENVIRONMENT, load_capture, summarize


def capture(directory, name, seconds=(0.02,) * 50, **overrides):
    directory.mkdir(exist_ok=True)
    metadata = {key: "fixture" for key in ENVIRONMENT}
    metadata.update(
        schemaVersion=1,
        captureId=name,
        complete=True,
        reason="",
        scenario="anchored",
        variant="native",
        mssVersion="absent",
        plugins=["benchmark@0.1.0"],
        frames=len(seconds),
        elapsedSeconds=math.fsum(seconds),
        gc0=0,
        gc1=0,
        gc2=0,
        durationSeconds=1,
        warmupSeconds=0,
        timeScale=1,
        fixedDeltaTime=0.02,
        width=1920,
        height=1080,
        quality=1,
        fullscreen=True,
        shipyard=False,
        targetFrameRate=-1,
        vSyncCount=0,
        notes="",
        measurement="late-update-monotonic-frame-intervals",
    )
    metadata.update(overrides)
    path = directory / f"{name}.json"
    path.write_text(json.dumps(metadata))
    with path.with_suffix(".csv").open("w", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(["frame", "seconds", "gc0", "gc1", "gc2"])
        writer.writerows((index, value, 0, 0, 0) for index, value in enumerate(seconds))
    return path


def compare(left, right):
    return CliRunner().invoke(
        main,
        ["performance", "compare", "--baseline", str(left), "--candidate", str(right)],
    )


def test_math():
    result = summarize([0.01, 0.03, 0.06])
    assert result["mean_ms"] == pytest.approx(100 / 3)
    assert result["fps"] == pytest.approx(30)
    assert result["median_ms"] == 30
    assert result["p95_ms"] == 60 and result["p99_ms"] == 60
    assert result["over16.67_count"] == 2
    assert result["over50_count"] == 1
    assert summarize([1 / 60, 1 / 30, 0.05, 0.1])["over100_count"] == 0


def test_comparison_allows_mss_and_sail_difference(tmp_path):
    left, right = tmp_path / "left", tmp_path / "right"
    capture(left, "a")
    capture(
        right,
        "b",
        seconds=(0.04,) * 25,
        variant="spritsails",
        mssVersion="0.4.1",
        plugins=["benchmark@0.1.0", "com.august.moresailwindsails@0.4.1"],
    )
    result = compare(left, right)
    assert result.exit_code == 0, result.output
    assert "delta=+20.000 (+100.00%)" in result.output
    assert "Fewer than five" in result.output
    assert "No pass/fail" in result.output


def test_repeat_summary_does_not_pool_frames(tmp_path):
    left, right = tmp_path / "left", tmp_path / "right"
    for index in range(5):
        capture(left, f"a{index}", seconds=(0.02,) * (50 + index))
        capture(right, f"b{index}", seconds=(0.04,) * (25 + index))
    result = compare(left, right)
    assert result.exit_code == 0, result.output
    assert "20.000 [20.000, 20.000] -> 40.000 [40.000, 40.000]" in result.output
    assert "Fewer than five" not in result.output


@pytest.mark.parametrize(
    "overrides, message",
    [
        ({"complete": False}, "incomplete"),
        ({"schemaVersion": 2}, "schema"),
        ({"frames": 60}, "frame count"),
        ({"elapsedSeconds": 3}, "elapsed time"),
        ({"durationSeconds": 10}, "shorter"),
        ({"gc0": 1}, "GC totals"),
        ({"captureId": "different"}, "filename"),
    ],
)
def test_invalid_capture(tmp_path, overrides, message):
    path = capture(tmp_path, "a", **overrides)
    with pytest.raises(ValueError, match=message):
        load_capture(path)


@pytest.mark.parametrize(
    "key, value",
    [
        ("scenario", "elsewhere"),
        ("gpu", "different"),
        ("plugins", ["benchmark@0.1.0", "other@1"]),
        ("durationSeconds", 0.5),
    ],
)
def test_mismatched_environment(tmp_path, key, value):
    left, right = tmp_path / "left", tmp_path / "right"
    capture(left, "a")
    capture(right, "b", **{key: value})
    result = compare(left, right)
    assert result.exit_code == 1 and "Incompatible" in result.output


def test_cap_and_duplicate_warning(tmp_path):
    left, right = tmp_path / "left", tmp_path / "right"
    capture(left, "a", vSyncCount=1)
    capture(right, "b", vSyncCount=1)
    assert "frame cap/VSync" in compare(left, right).output
    capture(right, "a", vSyncCount=1)
    assert "Duplicate captures" in compare(left, right).output


def test_mixed_variants_and_partial_export(tmp_path):
    left, right = tmp_path / "left", tmp_path / "right"
    capture(left, "a")
    path = capture(left, "c", variant="other")
    capture(right, "b")
    assert "mixed variants" in compare(left, right).output
    path.unlink()
    assert "CSV without metadata" in compare(left, right).output


@pytest.mark.parametrize("value", [float("nan"), float("inf"), 0, -1])
def test_bad_frame_samples(tmp_path, value):
    path = capture(tmp_path, "a")
    path.with_suffix(".csv").write_text(f"frame,seconds,gc0,gc1,gc2\n0,{value},0,0,0\n")
    with pytest.raises(ValueError, match="frame sample"):
        load_capture(path)


def test_empty_and_missing_csv(tmp_path):
    left, right = tmp_path / "left", tmp_path / "right"
    path = capture(left, "a")
    right.mkdir()
    assert "no captures" in compare(left, right).output
    capture(right, "b")
    path.with_suffix(".csv").unlink()
    assert compare(left, right).exit_code == 1


@pytest.mark.parametrize(
    "extra",
    [
        {},
        {"droppedEvents": 0},
        {
            "events": [
                dict(seconds=0.1, phase="measured", kind="LoadingScenes", value=1)
            ],
            "droppedEvents": 3,
        },
        {"events": None, "droppedEvents": "unused"},
    ],
)
def test_retired_events_ignored_and_long_frames_retained(tmp_path, extra):
    left, right = tmp_path / "left", tmp_path / "right"
    capture(left, "a", seconds=(0.1, 2.0), timeScale=0, **extra)
    capture(right, "b", seconds=(0.1, 2.0), timeScale=0)
    _, summary = load_capture(left / "a.json")
    assert summary["max_ms"] == 2000 and summary["fps"] == pytest.approx(2 / 2.1)
    result = compare(left, right)
    assert result.exit_code == 0, result.output
    assert "event" not in result.output.lower()


def test_recorder_versions_cannot_be_mixed(tmp_path):
    left, right = tmp_path / "left", tmp_path / "right"
    capture(left, "a", recorderVersion="0.1.1")
    capture(right, "b", recorderVersion="0.1.2")
    result = compare(left, right)
    assert result.exit_code == 1 and "recorderVersion" in result.output
