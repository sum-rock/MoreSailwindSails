"""Verify template retries and per-height collision inputs during fallback generation."""

from types import SimpleNamespace
from unittest.mock import Mock

import numpy as np
import pytest

from tools.halyards import generate
from tools.halyards.geometry import Sphere
from tools.halyards.search import Seat


@pytest.fixture
def evaluation(monkeypatch):
    templates = [
        SimpleNamespace(path_id=index + 1, assets_file=SimpleNamespace(name="fixture"))
        for index in range(3)
    ]
    references = [{"m_PathID": template.path_id} for template in templates]
    mast = object()

    def matrix(template):
        result = np.eye(4)
        result[1, 3] = (template.path_id - 1) * 10
        return result

    scene = SimpleNamespace(
        data=lambda obj: {"reefWinch": references},
        ptr=lambda obj, reference: templates[reference["m_PathID"] - 1],
        matrix=matrix,
        transform=lambda obj: obj,
    )
    boat = SimpleNamespace(
        scene=scene,
        masts={5: mast},
        frame=np.eye(4),
        profile=SimpleNamespace(name="Brig"),
        controls={index: template for index, template in enumerate(templates)},
        extra_spheres=[],
        control_compatible=lambda control, mast: True,
        compatible=lambda node, mast: True,
    )
    triangles = np.array([[[0, 0, 0], [1, 0, 0], [0, 1, 0]]], dtype=float)
    monkeypatch.setattr(
        generate,
        "mast_surface",
        lambda **kwargs: (np.zeros(3), np.array([0, 1, 0]), triangles),
    )
    monkeypatch.setattr(generate, "identity", lambda obj: ("fixture", id(obj)))

    def geometry(*, boat, control):
        index = control.path_id - 1
        angle = index * np.pi / 2
        rotation = np.array(
            [
                [np.cos(angle), 0, np.sin(angle)],
                [0, 1, 0],
                [-np.sin(angle), 0, np.cos(angle)],
            ]
        )
        return (
            np.array([[0, 0, -0.1], [0.2, 0.2, 0.2]]) * (index + 1),
            rotation,
            0.1 * (index + 1),
        )

    monkeypatch.setattr(generate, "template_geometry", geometry)
    spheres = {
        index: Sphere(center=np.array([0, index * 10, 0]), radius=0.1)
        for index in range(3)
    }
    structures = [
        (template, triangles + [0, index * 10, 0])
        for index, template in enumerate(templates)
    ]
    kwargs = dict(
        boat=boat,
        mast_id=5,
        spheres=spheres,
        structures=structures,
        clearance=0.02,
        height_range=0.5,
    )
    return SimpleNamespace(
        kwargs=kwargs, references=references, templates=templates, geometry=geometry
    )


def seat(index):
    return Seat(
        contact=np.zeros(3),
        normal=np.array([0, 0, 1]),
        source_normal=np.array([0, 0, 1]),
        offset=0.1,
        template_index=index,
    )


def test_retry_uses_next_templates_geometry_and_restores_nearby_obstacles(
    monkeypatch, evaluation
):
    expected = seat(index=1)
    search = Mock(side_effect=[None, expected])
    monkeypatch.setattr(generate, "search", search)
    result, source = generate.evaluate(**evaluation.kwargs)
    assert result is expected
    assert f"fixture:{id(evaluation.templates[1])}" in source
    assert "templateIndex: 1" in generate.definition(
        mast_id=5, seat=result, source=source
    )
    assert search.call_count == 2
    for index, call in enumerate(search.call_args_list):
        args = call.kwargs
        points, rotation, offset = evaluation.geometry(
            boat=evaluation.kwargs["boat"], control=evaluation.templates[index]
        )
        assert args["template_index"] == index
        np.testing.assert_allclose(args["template_origin"], [0, index * 10, 0])
        np.testing.assert_allclose(args["template_points"], points)
        np.testing.assert_allclose(args["template_rotation"], rotation)
        assert args["offset"] == offset
        assert len(args["obstacles"]) == 1
        assert args["obstacles"][0] is evaluation.kwargs["spheres"][index]
        np.testing.assert_allclose(
            args["structure"], evaluation.kwargs["structures"][index][1]
        )
        assert args["height_range"] == 0.5
        assert args["clearance"] == 0.02


def test_first_success_stops_search(monkeypatch, evaluation):
    search = Mock(return_value=seat(index=0))
    monkeypatch.setattr(generate, "search", search)
    generate.evaluate(**evaluation.kwargs)
    search.assert_called_once()


def test_null_and_unusable_templates_preserve_array_index(monkeypatch, evaluation):
    evaluation.references[0] = {"m_PathID": 0}

    def geometry(**kwargs):
        if kwargs["control"] is evaluation.templates[1]:
            raise ValueError("unusable")
        return evaluation.geometry(**kwargs)

    monkeypatch.setattr(generate, "template_geometry", geometry)
    search = Mock(return_value=seat(index=2))
    monkeypatch.setattr(generate, "search", search)
    result, _ = generate.evaluate(**evaluation.kwargs)
    search.assert_called_once()
    assert search.call_args.kwargs["template_index"] == result.template_index == 2


@pytest.mark.parametrize("failure", ["missing", "unusable", "blocked"])
def test_exhausted_templates_report_distinct_failures(monkeypatch, evaluation, failure):
    search = Mock(return_value=None)
    monkeypatch.setattr(generate, "search", search)
    if failure == "missing":
        evaluation.references.clear()
        message = "No native reef template"
    elif failure == "unusable":
        monkeypatch.setattr(
            generate, "template_geometry", Mock(side_effect=ValueError("unusable"))
        )
        message = "No reef template has usable rendered geometry"
    else:
        message = "No clear mast-surface position.*any usable reef template"
    with pytest.raises(ValueError, match=message):
        generate.evaluate(**evaluation.kwargs)
    assert search.call_count == (3 if failure == "blocked" else 0)
