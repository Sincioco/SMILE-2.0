"""Blender projection of the same physical layer profile used by native Studio."""
import json
from pathlib import Path

LAYERS = json.loads(Path(__file__).with_name('TownSurfaceLayers.json').read_text())


def elevation(layer):
    return (LAYERS[layer] - LAYERS['BLENDER_ORIGIN_Y']) / LAYERS['UNITS_PER_METER']


def distance(layer):
    return LAYERS[layer] / LAYERS['UNITS_PER_METER']
