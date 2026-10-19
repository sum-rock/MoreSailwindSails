"""Verify float32-compatible C# output and rejection of malformed numeric input."""

import numpy as np
import pytest

from tools.halyards.generate import definition, number
from tools.halyards.search import Seat


def test_definition_contains_all_existing_constructor_arguments():
    seat = Seat(
        contact=np.array([1.23456789, -2.0, 3.0]),
        normal=np.array([0.0, 0.0, -1.0]),
        source_normal=np.array([-1.0, 0.0, 0.0]),
        offset=0.13091201,
        template_index=2,
    )
    source = definition(mast_id=5, seat=seat, source="numeric fixture")
    for field in (
        "mast: 5",
        "contact: new Vector3",
        "normal: new Vector3",
        "sourceNormal: new Vector3",
        "offset:",
        "templateIndex: 2",
    ):
        assert field in source
    assert np.float32(number(seat.contact[0])[:-1]) == np.float32(seat.contact[0])
    assert "new HalyardFallbackDefinition(" in source


@pytest.mark.parametrize("value", [float("nan"), float("inf"), -float("inf")])
def test_nonfinite_values_cannot_be_emitted(value):
    with pytest.raises(ValueError, match="non-finite"):
        number(value)
