"""Validate benchmark artifacts and compare repeated whole-game frame captures."""

import csv
import json
import math
from pathlib import Path
import statistics

import click

from tools.performance.install import install

MSS_GUID = "com.august.moresailwindsails"
ENVIRONMENT = (
    "measurement",
    "recorderVersion",
    "gameVersion",
    "unityVersion",
    "cpu",
    "gpu",
    "graphicsApi",
    "operatingSystem",
    "width",
    "height",
    "quality",
    "fullscreen",
    "vSyncCount",
    "targetFrameRate",
    "scene",
    "shipyard",
    "timeScale",
    "fixedDeltaTime",
    "warmupSeconds",
    "durationSeconds",
    "scenario",
)


def percentile(values: list[float], fraction: float) -> float:
    """Nearest-rank percentile of an ascending, nonempty list."""
    return values[max(0, math.ceil(len(values) * fraction) - 1)]


def summarize(seconds: list[float]) -> dict[str, float]:
    """Compute whole-frame statistics; FPS is not the mean of reciprocals."""
    milliseconds = sorted(value * 1000 for value in seconds)
    elapsed = math.fsum(seconds)
    result = {
        "mean_ms": elapsed * 1000 / len(seconds),
        "fps": len(seconds) / elapsed,
        "median_ms": percentile(milliseconds, 0.5),
        "p95_ms": percentile(milliseconds, 0.95),
        "p99_ms": percentile(milliseconds, 0.99),
        "max_ms": milliseconds[-1],
    }
    for label, threshold in (
        ("16.67", 1000 / 60),
        ("33.33", 1000 / 30),
        ("50", 50),
        ("100", 100),
    ):
        count = sum(value > threshold for value in milliseconds)
        result[f"over{label}_count"] = count
        result[f"over{label}_percent"] = count * 100 / len(seconds)
    return result


def load_capture(path: Path) -> tuple[dict, dict[str, float]]:
    """Reject partial, malformed or inconsistent artifacts instead of silently skipping them."""
    metadata = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(metadata, dict) or metadata.get("schemaVersion") != 1:
        raise ValueError(f"{path}: unsupported metadata schema")
    if metadata.get("complete") is not True:
        raise ValueError(
            f"{path}: incomplete capture ({metadata.get('reason', 'unknown')})"
        )
    for key in (
        *ENVIRONMENT,
        "variant",
        "mssVersion",
        "captureId",
        "plugins",
        "frames",
        "elapsedSeconds",
        "gc0",
        "gc1",
        "gc2",
    ):
        if key not in metadata or metadata[key] is None:
            raise ValueError(f"{path}: missing {key}")
    for key in ("scenario", "variant", "captureId", "mssVersion"):
        if not isinstance(metadata[key], str) or not metadata[key].strip():
            raise ValueError(f"{path}: invalid {key}")
    if metadata["captureId"] != path.stem:
        raise ValueError(f"{path}: capture ID does not match filename")
    if not isinstance(metadata["plugins"], list) or any(
        not isinstance(p, str) or "@" not in p for p in metadata["plugins"]
    ):
        raise ValueError(f"{path}: invalid plugin inventory")
    if metadata["measurement"] != "late-update-monotonic-frame-intervals":
        raise ValueError(f"{path}: unsupported measurement")
    for key in ("durationSeconds", "elapsedSeconds", "fixedDeltaTime"):
        value = metadata[key]
        if (
            not isinstance(value, (int, float))
            or not math.isfinite(value)
            or value <= 0
        ):
            raise ValueError(f"{path}: invalid {key}")
    for key in (
        "frames",
        "gc0",
        "gc1",
        "gc2",
        "width",
        "height",
        "quality",
        "vSyncCount",
        "targetFrameRate",
    ):
        if type(metadata[key]) is not int:
            raise ValueError(f"{path}: invalid integer {key}")
    for key in ("fullscreen", "shipyard"):
        if type(metadata[key]) is not bool:
            raise ValueError(f"{path}: invalid boolean {key}")
    time_scale = metadata["timeScale"]
    if (
        type(time_scale) not in (int, float)
        or not math.isfinite(time_scale)
        or time_scale < 0
    ):
        raise ValueError(f"{path}: invalid timeScale")
    warmup = metadata["warmupSeconds"]
    if type(warmup) not in (int, float) or not math.isfinite(warmup) or warmup < 0:
        raise ValueError(f"{path}: invalid warmupSeconds")
    with path.with_suffix(".csv").open(newline="", encoding="utf-8") as stream:
        reader = csv.DictReader(stream)
        if reader.fieldnames != ["frame", "seconds", "gc0", "gc1", "gc2"]:
            raise ValueError(f"{path}: invalid CSV header")
        seconds = []
        gc = [0, 0, 0]
        for index, row in enumerate(reader):
            if None in row or any(value is None for value in row.values()):
                raise ValueError(f"{path}: malformed CSV row")
            value = float(row["seconds"])
            if int(row["frame"]) != index or not math.isfinite(value) or value <= 0:
                raise ValueError(f"{path}: invalid frame sample")
            seconds.append(value)
            for generation in range(3):
                delta = int(row[f"gc{generation}"])
                if delta < 0:
                    raise ValueError(f"{path}: invalid GC counter")
                gc[generation] += delta
    if not seconds or len(seconds) != metadata["frames"]:
        raise ValueError(f"{path}: frame count mismatch or empty capture")
    elapsed = math.fsum(seconds)
    if not math.isclose(
        elapsed, metadata["elapsedSeconds"], rel_tol=1e-9, abs_tol=1e-9
    ):
        raise ValueError(f"{path}: elapsed time mismatch")
    if elapsed < metadata["durationSeconds"] - 1e-9:
        raise ValueError(f"{path}: shorter than requested duration")
    if gc != [metadata[f"gc{i}"] for i in range(3)]:
        raise ValueError(f"{path}: GC totals mismatch")
    return metadata, summarize(seconds)


def environment(metadata: dict) -> dict:
    result = {key: metadata[key] for key in ENVIRONMENT}
    result["pluginsExceptMss"] = sorted(
        p for p in metadata["plugins"] if p.split("@", 1)[0] != MSS_GUID
    )
    return result


def load_group(directory: Path) -> list[tuple[dict, dict[str, float]]]:
    paths = sorted(directory.glob("*.json"))
    if not paths:
        raise ValueError(f"{directory}: no captures")
    orphaned = {p.stem for p in directory.glob("*.csv")} - {p.stem for p in paths}
    if orphaned:
        raise ValueError(
            f"{directory}: CSV without metadata: {', '.join(sorted(orphaned))}"
        )
    runs = [load_capture(path) for path in paths]
    reference = runs[0][0]
    for metadata, _ in runs[1:]:
        require_matching(reference, metadata)
        if any(
            metadata[key] != reference[key]
            for key in ("variant", "mssVersion", "plugins")
        ):
            raise ValueError(
                f"{directory}: mixed variants or plugin versions within one group"
            )
    return runs


def require_matching(first: dict, second: dict) -> None:
    left, right = environment(first), environment(second)
    mismatched = [key for key in left if left[key] != right[key]]
    if mismatched:
        raise ValueError("Incompatible captures: " + ", ".join(mismatched))


@click.group()
def performance() -> None:
    """Install the optional benchmark recorder or compare its captures offline."""


performance.add_command(install)


@performance.command()
@click.option(
    "--baseline",
    required=True,
    type=click.Path(exists=True, file_okay=False, path_type=Path),
)
@click.option(
    "--candidate",
    required=True,
    type=click.Path(exists=True, file_okay=False, path_type=Path),
)
def compare(baseline: Path, candidate: Path) -> None:
    """Compare directories containing repeated runs of one scenario and variant.

    Use at least five runs per condition, alternating baseline/candidate order.
    Match boat, camera, weather, graphics, sail count and approximate area manually.
    Keep MSS profiling OFF. Compare native without MSS to native with MSS, then
    native with MSS to custom sails. Stage deployed, reefed, furled, off-camera
    and shipyard scenarios separately. No automatic performance budget is applied.
    """
    try:
        groups = [load_group(baseline), load_group(candidate)]
        require_matching(groups[0][0][0], groups[1][0][0])
        ids = [metadata["captureId"] for group in groups for metadata, _ in group]
        if len(ids) != len(set(ids)):
            raise ValueError("Duplicate captures across comparison groups")
    except (OSError, ValueError, TypeError, KeyError, csv.Error) as error:
        raise click.ClickException(str(error)) from error
    click.echo(
        "Whole-game frame intervals; positive frame-time deltas mean slower. FPS deltas use the opposite direction."
    )
    click.echo(
        "Thresholds 16.67/33.33 ms use exact 1/60 and 1/30 second budgets. Percentiles use nearest rank."
    )
    for label, group in zip(("baseline", "candidate"), groups):
        click.echo(
            f"\n{label}: {group[0][0]['variant']} ({len(group)} runs), scenario={group[0][0]['scenario']}, MSS={group[0][0]['mssVersion']}"
        )
        for metadata, summary in group:
            click.echo(
                f"  {metadata['captureId']} frames={metadata['frames']} elapsed={metadata['elapsedSeconds']:.3f}s gc0/1/2={metadata['gc0']}/{metadata['gc1']}/{metadata['gc2']}"
            )
            click.echo(
                "    "
                + " ".join(f"{key}={value:.3f}" for key, value in summary.items())
            )
            if metadata.get("notes"):
                click.echo(f"    notes: {metadata['notes']}")
    click.echo("\nAcross runs: median [min, max]; runs are not pooled.")
    for key in groups[0][0][1]:
        values = [[summary[key] for _, summary in group] for group in groups]
        left, right = (statistics.median(group) for group in values)
        delta = right - left
        percent = f"{delta / left * 100:+.2f}%" if left else "n/a (zero baseline)"
        click.echo(
            f"{key}: {left:.3f} [{min(values[0]):.3f}, {max(values[0]):.3f}] -> {right:.3f} [{min(values[1]):.3f}, {max(values[1]):.3f}]; delta={delta:+.3f} ({percent})"
        )
    metadata = groups[0][0][0]
    if metadata["vSyncCount"] > 0 or metadata["targetFrameRate"] > 0:
        click.echo(
            "\nCAUTION: configured frame cap/VSync can conceal additional work. External driver caps are not detectable here."
        )
    if any(len(group) < 5 for group in groups):
        click.echo(
            "\nFewer than five repeats in at least one group; collect more runs before drawing conclusions."
        )
    click.echo(
        "\nNo pass/fail budget applied. Differences within repeat variation are unresolved; this does not demonstrate zero overhead or identify CPU/GPU cost."
    )
