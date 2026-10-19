"""Opt into read-only integration checks against a local Sailwind installation."""

from pathlib import Path
import pytest


def pytest_addoption(parser):
    parser.addoption(
        "--installed-game",
        type=Path,
        default=None,
        help="Run installed-asset checks against this Sailwind directory.",
    )


@pytest.fixture(scope="session")
def installed_scene(request):
    game = request.config.getoption("--installed-game")
    if game is None:
        pytest.skip("Pass --installed-game PATH to inspect local proprietary assets")
    from tools.halyards.scene import Scene

    return Scene(game_dir=game, include_se=True)
