"""Run with installed Blender --background --factory-startup --python build.py -- NAME."""
import sys
from pathlib import Path

import bpy

SOURCE=Path(__file__).resolve().parent
sys.path.insert(0,str(SOURCE))
from geometry import Geometry,palette
from shops import build_shop
from delivery import deliver


def main():
    if not bpy.app.background or bpy.data.filepath:
        raise RuntimeError('Use a NEW Blender --background --factory-startup process. Existing scenes are preserved.')
    args=sys.argv[sys.argv.index('--')+1:]
    name=args[0]
    package=SOURCE.parent
    game=package.parents[3]
    refs=game/'Assets'/'Towns'/'Neris'
    exports=refs/'ModelsV1'
    for folder in [package/'Blend',package/'Previews',exports]:
        folder.mkdir(parents=True,exist_ok=True)
    # This script runs in a NEW factory-startup background process only.
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj,do_unlink=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    materials=palette()
    geometry=Geometry('Neris '+name,materials)
    if name in ['Weapon Store','Armor Store','Item Store']:
        dimensions=build_shop(geometry,name)
        reference=refs/('Neris - '+name+'.png')
    else:
        from civic import build_tower,build_city_hall
        dimensions=build_tower(geometry) if name=='Communication Tower' else build_city_hall(geometry)
        reference=refs/('Neris - Communication Tower.png' if name=='Communication Tower' else 'Neris City Hall.png')
    slug='Neris-'+name.replace(' ','-')
    views=args[1:] or ['Beauty','Front','Back','Left','Right']
    deliver(geometry,dimensions,slug,reference,package,exports,views)


if __name__=='__main__':
    main()
