"""Assemble a fresh, self-contained Neris town; never replace an open user scene."""
import sys
from pathlib import Path

import bpy

PACKAGE = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PACKAGE / 'Source'))
sys.path.insert(0, str(PACKAGE.parent / 'NerisBuildingsV1' / 'Source'))

from geometry import palette, material
from homes import create_homes
from town_layout import assemble_town
from presentation import finish


def main():
    if not bpy.app.background or bpy.data.filepath:
        raise RuntimeError('Run in a fresh background Blender process with --factory-startup.')
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if not collection.objects:
            bpy.data.collections.remove(collection)
    mats = palette()
    for key, color, metal, rough in [
        ('paving', (.38, .43, .42), 0, .85),
        ('pavinglight', (.55, .57, .50), 0, .85),
        ('grass', (.16, .28, .115), 0, 1),
        ('soil', (.105, .135, .082), 0, 1),
        ('water', (.035, .32, .38), .35, .16),
        ('flower', (.65, .30, .39), 0, .8),
        ('treeleaf', (.19, .39, .21), 0, .9),
        ('treebright', (.36, .49, .20), 0, .9),
        ('foundation', (.20, .25, .25), 0, .95),
    ]:
        mats[key] = material('Town ' + key.title(), color, metal, rough)
    homes = create_homes(mats)
    placements = assemble_town(PACKAGE, mats, homes)
    finish(PACKAGE, placements, '--draft' in sys.argv)


if __name__ == '__main__':
    main()
